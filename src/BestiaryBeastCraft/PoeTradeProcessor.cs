using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;

namespace BestiaryBeastCraft
{
    public class PoeTradeProcessor
    {
        List<string> DefaultPostData = new List<string>();

        public PoeTradeProcessor()
        {
            DefaultPostData.Add("league=Bestiary");
            DefaultPostData.Add("type=Beast");
            DefaultPostData.Add("base=");
            DefaultPostData.Add("name=");
            DefaultPostData.Add("dmg_min=");
            DefaultPostData.Add("dmg_max=");
            DefaultPostData.Add("aps_min=");
            DefaultPostData.Add("aps_max=");
            DefaultPostData.Add("crit_min=");
            DefaultPostData.Add("crit_max=");
            DefaultPostData.Add("dps_min=");
            DefaultPostData.Add("dps_max=");
            DefaultPostData.Add("edps_min=");
            DefaultPostData.Add("edps_max=");
            DefaultPostData.Add("pdps_min=");
            DefaultPostData.Add("pdps_max=");
            DefaultPostData.Add("armour_min=");
            DefaultPostData.Add("armour_max=");
            DefaultPostData.Add("evasion_min=");
            DefaultPostData.Add("evasion_max=");
            DefaultPostData.Add("shield_min=");
            DefaultPostData.Add("shield_max=");
            DefaultPostData.Add("block_min=");
            DefaultPostData.Add("block_max=");
            DefaultPostData.Add("sockets_min=");
            DefaultPostData.Add("sockets_max=");
            DefaultPostData.Add("link_min=");
            DefaultPostData.Add("link_max=");
            DefaultPostData.Add("sockets_r=");
            DefaultPostData.Add("sockets_g=");
            DefaultPostData.Add("sockets_b=");
            DefaultPostData.Add("sockets_w=");
            DefaultPostData.Add("linked_r=");
            DefaultPostData.Add("linked_g=");
            DefaultPostData.Add("linked_b=");
            DefaultPostData.Add("linked_w=");
            DefaultPostData.Add("rlevel_min=");
            DefaultPostData.Add("rlevel_max=");
            DefaultPostData.Add("rstr_min=");
            DefaultPostData.Add("rstr_max=");
            DefaultPostData.Add("rdex_min=");
            DefaultPostData.Add("rdex_max=");
            DefaultPostData.Add("rint_min=");
            DefaultPostData.Add("rint_max=");

            DefaultPostData.Add("InsertModsHere");
            //DefaultPostData.Add("mod_name=");
            //DefaultPostData.Add("mod_min=");
            //DefaultPostData.Add("mod_max=");
            //DefaultPostData.Add("mod_weight=");

            DefaultPostData.Add("group_type=And");
            DefaultPostData.Add("group_min=");
            DefaultPostData.Add("group_max=");
            DefaultPostData.Add("group_count=1");
            DefaultPostData.Add("q_min=");
            DefaultPostData.Add("q_max=");
            DefaultPostData.Add("level_min=");
            DefaultPostData.Add("level_max=");
            DefaultPostData.Add("ilvl_min=");
            DefaultPostData.Add("ilvl_max=");
            DefaultPostData.Add("rarity=");
            DefaultPostData.Add("seller=");
            DefaultPostData.Add("thread=");
            DefaultPostData.Add("identified=");
            DefaultPostData.Add("corrupted=0");
            DefaultPostData.Add("progress_min=");
            DefaultPostData.Add("progress_max=");
            DefaultPostData.Add("sockets_a_min=");
            DefaultPostData.Add("sockets_a_max=");
            DefaultPostData.Add("shaper=");
            DefaultPostData.Add("elder=");
            DefaultPostData.Add("map_series=");
            DefaultPostData.Add("crafted=");
            DefaultPostData.Add("enchanted=");
            DefaultPostData.Add("online=x");
            DefaultPostData.Add("altart=");
            DefaultPostData.Add("capquality=x");
            DefaultPostData.Add("buyout_min=");
            DefaultPostData.Add("buyout_max=");
            DefaultPostData.Add("buyout_currency=");
            DefaultPostData.Add("has_buyout=1");
            DefaultPostData.Add("exact_currency=");
        }

        string url = "http://poe.trade/search";
        public async Task CheckBestiaryPrice(MonsterDisplayCfg monsterFcg, bool byMods)
        {
            var postData = new List<string>(DefaultPostData);


            var insIndex = postData.IndexOf("InsertModsHere");
            postData.RemoveAt(insIndex);

            if (byMods)
            {
                monsterFcg.Price = $"({string.Join(", ", monsterFcg.Mods)}) ";

                var cachedPrice = GetCachedPrice(monsterFcg.Price + monsterFcg.Level);
                if (!string.IsNullOrEmpty(cachedPrice))
                {
                    var pr = cachedPrice.Split('|');
                    monsterFcg.Price += pr[0];
                    monsterFcg.URL = pr[1];
                    return;
                }

                foreach (var mod in monsterFcg.Mods)
                {
                    postData.Insert(insIndex, "mod_weight=");
                    postData.Insert(insIndex, "mod_max=");
                    postData.Insert(insIndex, "mod_min=");
                    postData.Insert(insIndex, $"mod_name={mod}");
                }
                InsertChangeData(postData, "group_count", monsterFcg.Mods.Count.ToString());
            }
            else
            {
                monsterFcg.Price = $"({monsterFcg.DisplayName}) ";

                var cachedPrice = GetCachedPrice(monsterFcg.Price + monsterFcg.Level);
                if (!string.IsNullOrEmpty(cachedPrice))
                {
                    var pr = cachedPrice.Split('|');
                    monsterFcg.Price += pr[0];
                    monsterFcg.URL = pr[1];
                    return;
                }

                InsertChangeData(postData, "name", monsterFcg.DisplayName);
            }

          
            InsertChangeData(postData, "ilvl_min", monsterFcg.Level.ToString());
            //InsertChangeData(postData, "ilvl_max", monsterFcg.Level.ToString());



            using (var client = new MyWebClient())
            {
                try
                {
                    client.Encoding = Encoding.UTF8;
                    var qstr = string.Join("&", postData.ToArray()); 
                    byte[] bytes = Encoding.UTF8.GetBytes(qstr);

                    bytes = await client.UploadDataTaskAsync(new Uri("http://poe.trade/search"), "POST", bytes);

                    monsterFcg.URL = client.ResponseUri.AbsoluteUri;

                    //PoeHUD.DebugPlug.DebugPlugin.LogMsg("URL: " + client.ResponseUri, 10);

                    using (var ms = new MemoryStream(bytes))
                    {
                        using (var gsr = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            using (var sr = new StreamReader(gsr))
                            {
                                string response = sr.ReadToEnd();

                                var calculatedPrice = CalcPriceParse(response) + "c";
                                if (!CachedPrices.ContainsKey(monsterFcg.Price + monsterFcg.Level))
                                    CachedPrices.Add(monsterFcg.Price + monsterFcg.Level, calculatedPrice + "|" + monsterFcg.URL);
                                monsterFcg.Price += calculatedPrice;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PoeHUD.DebugPlug.DebugPlugin.LogMsg("Error while calculating beast price: " + ex.Message, 10);
                }
            }
        }

        public static Dictionary<string, string> CachedPrices = new Dictionary<string, string>();
        private string GetCachedPrice(string input)
        {
            if (CachedPrices.TryGetValue(input, out var price))
            {
                return price;
            }
            return null;
        }


        private string CalcPriceParse(string response)
        {
            var prices = new List<float>();
            var igns = new List<string>();

            var regex = new Regex("data-buyout=\"(\\d+) chaos\"");
            var mathes = regex.Matches(response);

            foreach (Match match in mathes)
            {
                foreach (Group group in match.Groups)
                {
                    var priceStr = group.Value.Replace("data-buyout=\"", string.Empty);
                    priceStr = priceStr.Replace(" chaos\"", string.Empty);
                    
                    float price;
                    if(float.TryParse(priceStr, out price))
                    {
                        prices.Add(price);
                    }
                }
            }

            if (prices.Count == 0)
                return "PriceNotFound";



            //prices = prices.OrderByDescending(x => prices.Count(y => y == x)).ThenBy(z => z).Distinct().ToList();


            return string.Join(",", prices.Distinct().Take(5).Select(x => x.ToString()));
        }

        /*
        private List<string> GetRegexValues(string regexFilter, string input)
        {
            var result = new List<string>();
            var regex = new Regex(regexFilter);
            var mathes = regex.Matches(input);

            foreach (Match match in mathes)
            {
                foreach (Group group in match.Groups)
                {
                    result.Add(group.Value);
                }
            }
            return result;
        }
        */
        private int InsertChangeData(List<string> data, string parm, string newData)
        {
            int pos = 0;
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].StartsWith(parm + "="))
                {
                    data[i] = parm + "=" + newData;
                    pos = i;
                }
            }
            return pos;
        }

        class MyWebClient : WebClient
        {
            Uri _responseUri;

            public Uri ResponseUri
            {
                get { return _responseUri; }
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                WebResponse response = base.GetWebResponse(request);
                _responseUri = response.ResponseUri;
                return response;
            }

            protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
            {
                WebResponse response = base.GetWebResponse(request);
                _responseUri = response.ResponseUri;
                return response;
            }
        }
    }
}
