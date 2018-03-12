using System.Windows.Forms;
using SharpDX;
using PoeHUD.Plugins;
using PoeHUD.Hud.Settings;

namespace BestiaryBeastCraft
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            Enable = true;
            
            Width = new RangeNode<int>(700, 0, 1000);
            Height = new RangeNode<int>(20, 0, 100);
            Spacing = new RangeNode<int>(5, 0, 100);
            TextHeight = new RangeNode<int>(15, 5, 50);
            CaptureTime = new RangeNode<int>(3, 0, 10);

            BGColor = Color.Black;
            UniqueHpColor = Color.Brown;
            RareHpColor = Color.Orange;
            UniqueTextColor = Color.White;
            RareTextColor = Color.Black;

            DPS = new RangeNode<int>(150, 0, 2000);
            ShowCatchThreshold = true;
            HasCullingStrike = false;
            UpperThreshold = false;
            Has20PrcCullingStrike = false;
        }

        [Menu("Show Recipes", 0)]
        public ToggleNode ShowRecipes { get; set; } = true;

        [Menu("Short Description", 1, 0)]
        public ToggleNode RecipesShortDescription { get; set; } = false;

        [Menu("Show Genus")]
        public ToggleNode ShowGenus { get; set; } = false;

        [Menu("Show Amount Captured")]
        public ToggleNode ShowAmountCaptured { get; set; } = true;

        [Menu("Show HP")]
        public ToggleNode ShowHP { get; set; } = true;

        [Menu("Pos X")]
        public RangeNode<int> PosX { get; set; } = new RangeNode<int>(500, 0, 2000);

        [Menu("Pos Y")]
        public RangeNode<int> PosY { get; set; } = new RangeNode<int>(1000, 0, 2000);

        [Menu("Width")]
        public RangeNode<int> Width { get; set; }

        [Menu("Height")]
        public RangeNode<int> Height { get; set; }

        [Menu("TextHeight")]
        public RangeNode<int> TextHeight { get; set; }

        [Menu("Spacing")]
        public RangeNode<int> Spacing { get; set; }

        [Menu("BG Color")]
        public ColorNode BGColor { get; set; }

        [Menu("Unique HP Color")]
        public ColorNode UniqueHpColor { get; set; }

        [Menu("Rare HP Color")]
        public ColorNode RareHpColor { get; set; }

        [Menu("Unique Text Color")]
        public ColorNode UniqueTextColor { get; set; }

        [Menu("Rare Text Color")]
        public ColorNode RareTextColor { get; set; }


        [Menu("Show Catch Threshold", 100)]
        public ToggleNode ShowCatchThreshold { get; set; }

        [Menu("Upper (min) Threshold", "To kill (false) or to Capture (true) threshold. Set false if you have enough DPS to kill it fast", 101, 100)]
        public ToggleNode UpperThreshold { get; set; }

        [Menu("DPS (K)", 110, 100)]
        public RangeNode<int> DPS { get; set; }

        [Menu("Has Culling Strike", 120, 100)]
        public ToggleNode HasCullingStrike { get; set; }

        [Menu("Slayer (20% CS)", 121, 120)]
        public ToggleNode Has20PrcCullingStrike { get; set; }

        [Menu("Net Capture Time", "By default 3sec", 130, 100)]
        public RangeNode<int> CaptureTime { get; set; }


        [Menu("Draw Icon", 200)]
        public ToggleNode DrawIcon { get; set; } = true;

        [Menu("Height", 210, 200)]
        public RangeNode<int> IconSize { get; set; } = new RangeNode<int>(70, 10, 200);
    }
}
