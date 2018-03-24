using SharpDX;
using SharpDX.Direct3D9;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Models;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using PoeHUD.Poe.FilesInMemory;

namespace BestiaryBeastCraft
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        public Core() { PluginName = "BestiaryBeastCraft"; }

        private Dictionary<string, CraftMonster> SpecialMonsterMetadata = new Dictionary<string, CraftMonster>();
        private Dictionary<string, CraftMonster>  MonsterMods = new Dictionary<string, CraftMonster>();
        private List<MonsterDisplayCfg> TrackingMonsters = new List<MonsterDisplayCfg>();
        private Dictionary<string, BestiaryCapturableMonster> MonstersUniversal = new Dictionary<string, BestiaryCapturableMonster>();
        private Dictionary<string, int> CaptureGenusAmount = new Dictionary<string, int>();

        public override void Initialise()
        {
            LoadRecipes();
            CalcAmount();
            GameController.Area.OnAreaChange += Area_OnAreaChange;
        }

        public override void OnPluginDestroyForHotReload()
        {
            GameController.Area.OnAreaChange -= Area_OnAreaChange;
        }

        private void LoadRecipes()
        {
            foreach (var recipe in GameController.Files.BestiaryRecipes.EntriesList)
            {
                foreach (var component in recipe.Components)
                {
                    if (component.Mod != null)
                    {
                        if (!MonsterMods.TryGetValue(component.Mod.Key, out CraftMonster recipeCfg))
                        {
                            recipeCfg = new CraftMonster();
                            recipeCfg.Mod = component.Mod;
                            MonsterMods.Add(component.Mod.Key, recipeCfg);
                        }
                        recipeCfg.Recipes.Add(recipe);
                    }

                    if (component.BestiaryCapturableMonster != null)
                    {
                        if (!SpecialMonsterMetadata.TryGetValue(component.BestiaryCapturableMonster.MonsterVariety.VarietyId, out CraftMonster recipeCfg))
                        {
                            recipeCfg = new CraftMonster();
                            SpecialMonsterMetadata.Add(component.BestiaryCapturableMonster.MonsterVariety.VarietyId, recipeCfg);
                        }
                        recipeCfg.Recipes.Add(recipe);
                    }
                }
            }

            foreach (var defMob in GameController.Files.BestiaryCapturableMonsters.EntriesList)
            {
                MonstersUniversal.Add(defMob.MonsterVariety.VarietyId, defMob);
            }
        }


        private bool IsManagerie;
        private void Area_OnAreaChange(PoeHUD.Controllers.AreaController obj)
        {
            string areaId = GameController.Game.IngameState.Data.CurrentWorldArea.Id;
            IsManagerie = 
                areaId == "Menagerie_Hub" || 
                areaId == "Menagerie_WaterCreatures" || 
                areaId == "Menagerie_InsectsArachnids" || 
                areaId == "Menagerie_BirdsReptiles" ||
                areaId == "Menagerie_Mammals";

            TrackingMonsters.Clear();
        }

        private class CraftMonster
        {
            public ModsDat.ModRecord Mod;
            public List<BestiaryRecipe> Recipes = new List<BestiaryRecipe>();
        }

        private void CalcAmount()
        {
            CaptureGenusAmount.Clear();

            foreach (var captMonster in GameController.Files.BestiaryCapturableMonsters.EntriesList)
            {
                if (!CaptureGenusAmount.ContainsKey(captMonster.BestiaryGenus.Name))
                    CaptureGenusAmount.Add(captMonster.BestiaryGenus.Name, 0);
                CaptureGenusAmount[captMonster.BestiaryGenus.Name] += GameController.Game.IngameState.ServerData.GetBeastCapturedAmount(captMonster);
            }
        }

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            if (IsManagerie && !Settings.ShowInManagerie.Value) return;
            if (!entityWrapper.IsHostile) return;
            if (!entityWrapper.HasComponent<Monster>()) return;
            var rareComps = entityWrapper.GetComponent<ObjectMagicProperties>();
            if (rareComps == null) return;
            if (rareComps.Rarity != MonsterRarity.Rare && rareComps.Rarity != MonsterRarity.Unique) return;

            var path = entityWrapper.Path;
            var sybstrIndc = path.IndexOf("@");
            if (sybstrIndc != -1)
                path = path.Substring(0, sybstrIndc);

            MonstersUniversal.TryGetValue(path, out BestiaryCapturableMonster translatedMonster);
            if (translatedMonster == null) return;

            var stats = entityWrapper.GetComponent<Stats>();
            bool captured = stats.StatDictionary.ContainsKey(GameStat.IsHiddenMonster) && stats.StatDictionary[GameStat.IsHiddenMonster] == 1;
            if (captured && Settings.HideCapturedImmediately.Value) return;



            CalcAmount();

            bool foundNotShitty = false;
            var captGenusAmount = CaptureGenusAmount[translatedMonster.BestiaryGenus.Name];
            var newDisplayCfg = new MonsterDisplayCfg()
            {
                CaptMonster = translatedMonster,
                CapturedGenusAmount = captGenusAmount,
                Entity = entityWrapper,
                LifeComp = entityWrapper.GetComponent<Life>(),
                Rarity = rareComps.Rarity,
                DisplayName = translatedMonster.MonsterName,
                IsCaptured = captured
            };
        

            if (SpecialMonsterMetadata.TryGetValue(path, out CraftMonster monsterNameCfg))
            {
                newDisplayCfg.Recipes.AddRange(monsterNameCfg.Recipes);
                foundNotShitty = true;
            }

            var modsFound = new List<string>();
            foreach (var mod in rareComps.Mods)
            {
                if (MonsterMods.TryGetValue(mod, out CraftMonster monsterModCfg))
                {
                    modsFound.Add(monsterModCfg.Mod.UserFriendlyName);
                    newDisplayCfg.Recipes.AddRange(monsterModCfg.Recipes);
                    foundNotShitty = true;
                }
            }

            newDisplayCfg.ModsCount = modsFound.Count;
            if(newDisplayCfg.ModsCount > 0)
                newDisplayCfg.DisplayMods = $"Mods: {string.Join(", ", modsFound)}";
            newDisplayCfg.CaptureThreshould = CalcCatchThreshould(newDisplayCfg);

            if(foundNotShitty || !Settings.HideShitty.Value)
                TrackingMonsters.Add(newDisplayCfg);
        }

        private float CalcCatchThreshould(MonsterDisplayCfg mobCfg)
        {
            var lifeComp = mobCfg.LifeComp;
            float fullHp = lifeComp.MaxES + lifeComp.MaxHP;
            float dmgToProcess = Settings.DPS.Value * 1000 * Settings.CaptureTime.Value;//3 sec

            if(Settings.UpperThreshold.Value)
            {
                if(mobCfg.IsRed)
                    dmgToProcess += fullHp * 0.1f;//On 10% hp
                else
                    dmgToProcess += fullHp * 0.2f;//On 20% hp
            }

            var result = dmgToProcess / fullHp;

            if (Settings.HasCullingStrike.Value && result < 0.1f)
                result = 0.1f;

            if (Settings.Has20PrcCullingStrike.Value && result < 0.2f)
                result = 0.2f;

            if (result < 0)
                result = 0;

            if (result > 1)
                result = 1;

            return result;
        }

        public override void EntityRemoved(EntityWrapper entityWrapper) => TrackingMonsters.RemoveAll(x => x.Entity == entityWrapper);

        private class MonsterDisplayCfg
        {
            public EntityWrapper Entity;
            public Life LifeComp;
            public List<BestiaryRecipe> Recipes = new List<BestiaryRecipe>();
            public BestiaryCapturableMonster CaptMonster;
            public int ModsCount;
            public float CaptureThreshould;
            public string DisplayName;
            public string DisplayMods;
            public int CapturedGenusAmount;
            public MonsterRarity Rarity;
            public bool IsCaptured;

            public bool IsRed => ModsCount > 1;
            public bool IsYellow => ModsCount == 1;
        }

        public override void Render()
        {
            if (IsManagerie && !Settings.ShowInManagerie.Value) return;
            if (!GameController.InGame) return;
            if (GameController.Game.IngameState.IngameUi.AtlasPanel.IsVisible) return;
            if (GameController.Game.IngameState.IngameUi.TreePanel.IsVisible) return;
            if (GameController.Area.CurrentArea.IsHideout) return;
            if (GameController.Area.CurrentArea.IsTown) return;
            if (GameController.Game.IngameState.IngameUi.InventoryPanel.IsVisible) return;
        
            var drawPosY = Settings.PosY.Value - Settings.Height.Value;

            foreach (var monster in TrackingMonsters.ToList())
            {
                var life = monster.LifeComp;
                int totalHp = life.MaxES + life.MaxHP;
                int curHp = life.CurES + life.CurHP;
                float perc = (float)curHp / totalHp;


                var stats = monster.Entity.GetComponent<Stats>();
                bool captured = stats.StatDictionary.ContainsKey(GameStat.IsHiddenMonster) && stats.StatDictionary[GameStat.IsHiddenMonster] == 1;
                bool netUsed = stats.StatDictionary.ContainsKey(GameStat.CannotDie) && stats.StatDictionary[GameStat.CannotDie] == 1;

                if(!monster.IsCaptured && captured)
                {
                    monster.IsCaptured = true;
                    CalcAmount();
                }
                if (captured && Settings.HideCapturedImmediately.Value)
                    TrackingMonsters.Remove(monster);

                if (Settings.DrawIcon.Value && !captured)
                {
                    var iconDrawPos = GameController.Game.IngameState.Camera.WorldToScreen(monster.Entity.Pos, monster.Entity);

                    string icon = "images/bestiary-white.png";
                    if (monster.IsRed)
                        icon = "images/bestiary-red.png";
                    else if (monster.IsYellow)
                        icon = "images/bestiary-yellow.png";
                    else if (monster.Rarity == MonsterRarity.Unique)
                        icon = "images/bestiary-uniq.png";


                    var iconDrawRect = new RectangleF(
                        iconDrawPos.X - Settings.IconSize.Value / 2,
                        iconDrawPos.Y - Settings.IconSize.Value / 2,
                        Settings.IconSize.Value, Settings.IconSize.Value);


                    Graphics.DrawPluginImage(Path.Combine(PluginDirectory, icon), iconDrawRect);

                    if (netUsed)
                        Graphics.DrawPluginImage(Path.Combine(PluginDirectory, "images/net.png"), iconDrawRect);
                }

                Graphics.DrawText(perc.ToString("p0"), Settings.TextHeight.Value,
                  new Vector2(Settings.PosX.Value - 5, drawPosY + Settings.Height.Value / 2), FontDrawFlags.VerticalCenter | FontDrawFlags.Right);

                var displayLabel = monster.DisplayName;

                if(!string.IsNullOrEmpty(monster.DisplayMods))
                    displayLabel += $", {monster.DisplayMods}";

                if (Settings.ShowGenus)
                    displayLabel += $", ({monster.CaptMonster.BestiaryGenus.Name})";

                var drawColor = Color.Gray;
                drawColor.A = 128;

                if(!captured)
                    drawColor = GetColorByRarity(monster.Rarity);

                if (netUsed)
                    drawColor = Color.Lerp(drawColor, Color.Gray, 0.5f);

                var mainRect = new RectangleF(Settings.PosX.Value, drawPosY, Settings.Width.Value, Settings.Height.Value);
                DrawProgressBar(mainRect, drawColor, perc, monster);

                if(netUsed)
                    Graphics.DrawPluginImage(Path.Combine(PluginDirectory, "images/bar_net.png"), mainRect);

                Graphics.DrawText(displayLabel, Settings.TextHeight.Value, 
                    new Vector2(Settings.PosX.Value + 5, drawPosY + Settings.Height.Value / 2), GetTextColorByRarity(monster.Rarity),
                    FontDrawFlags.VerticalCenter | FontDrawFlags.Left);

                string additionalInfo = "";

                if (Settings.ShowHP.Value)
                    additionalInfo += $"{(monster.LifeComp.CurES + monster.LifeComp.CurHP).KiloFormat()} ";

                if (Settings.ShowAmountCaptured)
                    additionalInfo += $"({monster.CapturedGenusAmount}/{monster.CaptMonster.BestiaryGenus.MaxInStorage})";

                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    Graphics.DrawText(additionalInfo, Settings.TextHeight.Value,
                    new Vector2(Settings.PosX.Value + Settings.Width.Value - 5, drawPosY + Settings.Height.Value / 2), GetTextColorByRarity(monster.Rarity),
                    FontDrawFlags.VerticalCenter | FontDrawFlags.Right);
                }

                if (Settings.ShowRecipes)
                {
                    var recipesSorted = (from recipe in monster.Recipes
                        group recipe by recipe.Description
                        into groupedDemoClass
                        select groupedDemoClass).ToDictionary(
                        gdc => gdc.Key, 
                        gdc => string.Join(", ", gdc.Select(x => 
                        x.HintText.Replace("Create a ", string.Empty).Replace("Create ", string.Empty).Replace(" with", string.Empty)
                        ).ToArray()));

                    var recipeStr = string.Join(", ", recipesSorted.Select(x => $"{x.Key}: {x.Value}"));

                    Graphics.DrawText(recipeStr, Settings.TextHeight.Value,
                    new Vector2(Settings.PosX.Value + Settings.Width.Value + 5, drawPosY + Settings.Height.Value / 2), Color.White, 
                    FontDrawFlags.VerticalCenter | FontDrawFlags.Left);
                }

                if(Settings.ShowCatchThreshold)
                {
                    var deltaPos = Settings.PosX.Value + Settings.Width.Value * monster.CaptureThreshould;
                    var pos1 = new Vector2(deltaPos, drawPosY - 3);
                    var pos2 = pos1;
                    pos2.Y += Settings.Height.Value + 6;

                    Graphics.DrawLine(pos1, pos2, 2, Color.Green);
                }

                drawPosY -= Settings.Height.Value;
                drawPosY -= Settings.Spacing.Value;
            }
        }

        private void DrawProgressBar(RectangleF mainRect, Color barColor, float perc, MonsterDisplayCfg monsterCfg)
        {
        
            Graphics.DrawBox(mainRect, Settings.BGColor.Value);
            var frameRect = mainRect;
            mainRect.Width *= perc;
            Graphics.DrawBox(mainRect, barColor);
            
            int borderSize = 1;
            var drawBorderColor = Color.White;

            if (monsterCfg.IsRed)
            {
                if (monsterCfg.Rarity == MonsterRarity.Unique)
                {
                    drawBorderColor = Color.DarkOrange;
                    borderSize = 6;
                }
                else
                {
                    drawBorderColor = Color.Red;
                    borderSize = 4;
                }
            }
            else if (monsterCfg.IsYellow)
            {
                drawBorderColor = Color.Red;
                borderSize = 2;
            }
            else if (monsterCfg.Rarity == MonsterRarity.Unique)
            {
                drawBorderColor = Color.DarkOrange;
                borderSize = 1;
            }

            Graphics.DrawFrame(frameRect, borderSize, drawBorderColor);
        }

        private Color GetColorByRarity(MonsterRarity rarity)
        {
            switch(rarity)
            {
                case MonsterRarity.Unique:
                    return Settings.UniqueHpColor.Value;
                case MonsterRarity.Rare:
                    return Settings.RareHpColor.Value;
                default:
                    return Color.Gray;
            }
        }

        private Color GetTextColorByRarity(MonsterRarity rarity)
        {
            switch (rarity)
            {
                case MonsterRarity.Unique:
                    return Settings.UniqueTextColor.Value;
                case MonsterRarity.Rare:
                    return Settings.RareTextColor.Value;
                default:
                    return Color.Gray;
            }
        }
    }

    public static class HpExtensions
    {
        public static string KiloFormat(this int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + "M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0K");

            if (num >= 1000)
                return (num / 1000).ToString("0.#") + "K";

            return num.ToString("#,0");
        }
    }
}