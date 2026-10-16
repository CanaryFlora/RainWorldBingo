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
using CreatureType = CreatureTemplate.Type;

namespace BingoMode.BingoChallenges
{
    using static ChallengeHooks;

    public class BingoDodgeRandomizer : ChallengeRandomizer
    {
        public Randomizer<int> amount;
        public Randomizer<string> creature;
        public Randomizer<string> region;

        public override Challenge Random()
        {
            BingoDodgeChallenge challenge = new();
            challenge.amount.Value = amount.Random();
            challenge.creature.Value = creature.Random();
            challenge.region.Value = region.Random();
            return challenge;
        }

        public override StringBuilder Serialize(string indent)
        {
            string surindent = indent + INDENT_INCREMENT;
            StringBuilder serializedContent = new();
            serializedContent.AppendLine($"{surindent}amount-{amount.Serialize(surindent)}");
            return base.Serialize(indent).Replace("__Type__", "Dodge").Replace("__Content__", serializedContent.ToString());
        }

        public override void Deserialize(string serialized)
        {
            Dictionary<string, string> dict = ToDict(serialized);
            amount = Randomizer<int>.InitDeserialize(dict["amount"]);
        }
    }

    public class BingoDodgeChallenge : BingoChallenge
    {
        public SettingBox<int> amount;
        public SettingBox<string> creature;
        public SettingBox<string> region;
        public int current;

        public BingoDodgeChallenge()
        {
            amount = new(0, "Amount", 0);
            creature = new("", "Creature", 1);
            region = new("", "Region", 2);
        }

        public override void UpdateDescription()
        {
            Dictionary<string, string> creatureNameToAttack = new()
            {
                ["BigNeedleWorm"] = "attacks",
                ["KingVulture"] = "harpoons",
                ["BigEel"] = "bites",
                ["SpitterSpider"] = "maggots",
            };
            if (ChallengeTools.creatureNames == null)
            {
                ChallengeTools.CreatureName(ref ChallengeTools.creatureNames);
            }
            string creatureName = "Unknown";
            try
            {
                int indexe = new CreatureType(creature.Value).index;
                if (indexe >= 0)
                {
                    creatureName = ChallengeTools.IGT.Translate(ChallengeTools.creatureNames[indexe]);
                }
            }
            catch (Exception ex)
            {
                ExpLog.Log("Error getting creature name for BingoDodgeChallenge | " + ex.Message);
            }
            description = ChallengeTools.IGT.Translate("Dodge [<current>/<amount>] <creature> <attack>")
                .Replace("<current>", ValueConverter.ConvertToString(current))
                .Replace("<amount>", ValueConverter.ConvertToString(amount.Value))
                .Replace("<creature>", creatureName)
                .Replace("<attack>", creatureNameToAttack[creature.Value]);
            base.UpdateDescription();
        }

        public override Phrase ConstructPhrase()
        {
            if (creature.Value == "BigEel" && amount.Value == 1)
            {
                return new Phrase([[new Icon("leviathan_dodge")]]);
            }
            else
            {
                Phrase phrase = new([[Icon.FromEntityName(creature.Value), new Icon("slugtarget")]]);

                int lastLine = 1;
                if (region.Value != "Any Region")
                {
                    phrase.InsertWord(new Verse(region.Value), 1);
                    lastLine = 2;
                }

                phrase.InsertWord(new Counter(current, amount.Value), lastLine);
                return phrase;
            }
        }

        public override string ChallengeName()
        {
            return ChallengeTools.IGT.Translate("Dodging creature attacks");
        }

        public override bool Duplicable(Challenge challenge)
        {
            return challenge is not BingoDodgeChallenge;
        }

        public override Challenge Generate()
        {
            return new BingoDodgeChallenge()
            {
                creature = new(ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.DodgeableCreatures)[UnityEngine.Random.Range(0, ChallengeUtils.GetCorrectListForChallenge(ChallengeListConstants.DodgeableCreatures).Length)], "Creature", 1, listName: ChallengeListConstants.DodgeableCreatures),
                amount = new(
                    (creature.Value == "BigEel" ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(2, 5)),
                    "Amount", 0),
                region = new("Any Region", "Region", 2)
            };
        }

        public void Dodged()
        {
            if (!completed && !TeamsCompleted[SteamTest.team] && !hidden && !revealed)
            {
                current++;
                UpdateDescription();
                if (current >= amount.Value) CompleteChallenge();
                else ChangeValue();
            }
        }

        public override bool CombatRequired()
        {
            return false;
        }

        public override int Points()
        {
            return 20;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                "BingoDodgeChallenge",
                "~",
                amount.ToString(),
                "><",
                current.ToString(),
                "><",
                completed ? "1" : "0",
                "><",
                revealed ? "1" : "0",
            });
        }

        public override void FromString(string args)
        {
            try
            {
                string[] array = Regex.Split(args, "><");
                amount = SettingBoxFromString(array[0]) as SettingBox<int>;
                current = int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                completed = (array[2] == "1");
                revealed = (array[3] == "1");
                UpdateDescription();
            }
            catch (Exception ex)
            {
                ExpLog.Log("ERROR: BingoDodgeChallenge FromString() encountered an error: " + ex.Message);
                throw ex;
            }
        }

        public override bool ValidForThisBingoSlugcat(SlugName slugcat, BingoData.BingoModifier modifier)
        {
            return true;
        }

        public override void AddHooks()
        {
            On.BigNeedleWorm.Swish += BigNeedleWorm_Swish;
        }

        public override void RemoveHooks()
        {
            On.BigNeedleWorm.Swish -= BigNeedleWorm_Swish;
        }

        public override List<object> Settings() => [amount];
    }
}
