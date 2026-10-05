using BingoMode.BingoRandomizer;
using BingoMode.BingoSteamworks;
using Expedition;
using Menu.Remix;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BingoMode.BingoChallenges
{
    using static ChallengeHooks;

    public class BingoTradeRandomizer : ChallengeRandomizer
    {
        public Randomizer<int> amount;
        public Randomizer<string> item;
        public Randomizer<int> itemAmount;
        public Randomizer<string> region;


        public override Challenge Random()
        {
            BingoTradeChallenge challenge = new();
            challenge.amount.Value = amount.Random();
            return challenge;
        }

        public override StringBuilder Serialize(string indent)
        {
            string surindent = indent + INDENT_INCREMENT;
            StringBuilder serializedContent = new();
            serializedContent.AppendLine($"{surindent}amount-{amount.Serialize(surindent)}");
            return base.Serialize(indent).Replace("__Type__", "Trade").Replace("__Content__", serializedContent.ToString());
        }

        public override void Deserialize(string serialized)
        {
            Dictionary<string, string> dict = ToDict(serialized);
            amount = Randomizer<int>.InitDeserialize(dict["amount"]);
        }
    }

    public class BingoTradeChallenge : BingoChallenge
    {
        public SettingBox<int> amount;
        public SettingBox<string> item;
        public SettingBox<int> itemAmount;
        public SettingBox<string> region;
        public int current;
        public List<EntityID> tradedItemIDs;
        public Dictionary<EntityID, EntityID> bannedMerchantForItem;

        public BingoTradeChallenge()
        {
            amount = new(0, "Value", 0);
            region = new("", "Any Region", 1);
            item = new("", "Item", 3, listName: ChallengeListConstants.SpecificMerchantItems);;
            itemAmount = new(0, "Item Amount", 4);
            tradedItemIDs = [];
        }

        public override void UpdateDescription()
        {
            string regionMod = region.Value != "Any Region" ? ChallengeTools.IGT.Translate(Region.GetRegionFullName(region.Value, BingoData.slugcatPlayer)) : "";
            if (item.Value == "Any Items")
            {
                this.description = ChallengeTools.IGT.Translate("Trade [<current>/<amount>] worth of value to Scavenger Merchants <region>")
                    .Replace("<amount>", ValueConverter.ConvertToString(amount.Value))
                    .Replace("<current>", ValueConverter.ConvertToString(current))
                    .Replace("<region>", regionMod != "" ? ChallengeTools.IGT.Translate("in ") + regionMod : "");
            }
            else if (item.Value != "Any Items")
            {
                this.description = ChallengeTools.IGT.Translate("Trade [<current>/<amount>] <item> to Scavenger Merchants <region>")
                    .Replace("<amount>", ValueConverter.ConvertToString(itemAmount.Value))
                    .Replace("<current>", ValueConverter.ConvertToString(current))
                    .Replace("<item>", ChallengeTools.IGT.Translate(ChallengeTools.ItemName(new(item.Value))))
                    .Replace("<region>", regionMod != "" ? ChallengeTools.IGT.Translate("in ") + regionMod : "");
            }
            base.UpdateDescription();
        }

        public override Phrase ConstructPhrase()
        {
            Phrase phrase;
            int shownNumber = item.Value == "Any Items" ? amount.Value : itemAmount.Value;
            if (item.Value != "Any Items") {
                phrase = new Phrase([[
                    item.Value == "KarmaFlower" ? Icon.KARMA_FLOWER : Icon.FromEntityName(item.Value),
                    new Icon(Plugin.PluginInstance.BingoConfig.FillIcons.Value ? "keyShiftB" : "keyShiftA", 1f, Color.white, 90), new Icon("scav_merchant")]]);
            } else
            {
                phrase = new Phrase(
                [[new Icon("scav_merchant")]]
                );
            };
            int lastLine = 1;
            if (region.Value != "Any Region")
            {
                phrase.InsertWord(new Verse(region.Value), 1);
                lastLine = 2;
            }
            phrase.InsertWord(new Counter(current, shownNumber), lastLine);
            return phrase;

            //return new Phrase(
            //    [[new Icon("scav_merchant")],
            //    [new Counter(current, amount.Value)]]);
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoTradeChallenge;
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Trading items to merchants");
        }

        public override Challenge Generate()
        {
            int amou = UnityEngine.Random.Range(5, 21);
            int specAmou = UnityEngine.Random.Range(1, 3);
            bool isSpecific = UnityEngine.Random.value < 0.5f;
            bool anyRegion = UnityEngine.Random.value < 0.5f;

            return new BingoTradeChallenge
            {
                amount = new(amou, "Value", 0),
                region = new(!anyRegion ? "Any Region" : ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.MerchantRegions)[UnityEngine.Random.Range(0, ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.MerchantRegions).Length)], "Region", 1, listName: ChallengeListConstants.MerchantRegions),
                item = new(!isSpecific ? "Any Items" : ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.SpecificMerchantItems)[UnityEngine.Random.Range(0, ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.SpecificMerchantItems).Length)], "Item", 2, listName: ChallengeListConstants.SpecificMerchantItems),
                itemAmount = new(specAmou, "Amount", 3),
                tradedItemIDs = [],
            };
        }

        public void Traded(int pnts, EntityID ID)
        {
            if (completed || TeamsCompleted[SteamTest.team] || revealed || hidden) return;
            
            if (!tradedItemIDs.Contains(ID))
            {
                current += pnts;
                tradedItemIDs.Add(ID);
                UpdateDescription();

                if (current >= amount.Value)
                {
                    CompleteChallenge();
                }
                else ChangeValue();
            }
        }

        public override int Points()
        {
            return amount.Value * 10;
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            current = 0;
        }

        public override bool ValidForThisBingoSlugcat(SlugName slugcat, BingoData.BingoModifier modifier)
        {
            return slugcat != SlugNameMSC.Artificer;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoTradeChallenge",
                "~",
                current.ToString(),
                "><",
                amount.ToString(),
                "><",
                completed ? "1" : "0",
                "><",
                revealed ? "1" : "0"
            });
        }

        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                current = int.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                amount = SettingBoxFromString(array[1]) as SettingBox<int>;
                completed = (array[2] == "1");
                revealed = (array[3] == "1");
                tradedItemIDs = [];
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoTradeChallenge FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public override void AddHooks()
        {
            On.Room.ctor += Room_ctor;
            On.Room.Unloaded += Room_Unloaded;
            On.Player.ReleaseGrasp += Player_ReleaseGrasp;
            On.Scavenger.Grab += Scavenger_GrabTrade;
        }

        public override void RemoveHooks()
        {
            On.Room.ctor -= Room_ctor;
            On.Room.Unloaded -= Room_Unloaded;
            On.Player.ReleaseGrasp -= Player_ReleaseGrasp;
            On.Scavenger.Grab -= Scavenger_GrabTrade;
        }

        public override List<object> Settings() => [amount, item, itemAmount, region];
    }
}