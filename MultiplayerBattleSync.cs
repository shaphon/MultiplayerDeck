using GameDataEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerDeck
{
    public static class MultiplayerBattleSync
    {
        private static bool battleInitialized;
        private static int lastDeckHash;
        private static int lastUsedHash;
        private static bool contributionSent;
        private static int lastDiscardCount = -1;
        private static readonly List<string> lastHandKeys = new List<string>();
        private static readonly Dictionary<string, List<string>> deckContributions = new Dictionary<string, List<string>>();

        public static void Tick()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer || BattleSystem.instance == null)
            {
                battleInitialized = false;
                contributionSent = false;
                lastDeckHash = 0;
                lastUsedHash = 0;
                lastDiscardCount = -1;
                lastHandKeys.Clear();
                deckContributions.Clear();
                return;
            }

            if (!battleInitialized)
            {
                battleInitialized = true;
                EnsureBattlePassive();
                SendDeckContributionOnce();
                SnapshotHand();
            }

            TrackClickWasteExchange();
            SendDeckStateWhenChanged();
        }

        public static void EnsureBattlePassive()
        {
            bool hasBossSync = false;
            foreach (PassiveBase passive in BattleSystem.instance.BattleExtended)
            {
                if (passive is MultiplayerDeck_Plugin.BossBattleNet)
                {
                    hasBossSync = true;
                }
            }

            if (!hasBossSync)
            {
                BattleSystem.instance.BattleExtended.Add(new MultiplayerDeck_Plugin.BossBattleNet());
            }
        }

        public static void ApplyDeckState(List<string> deck, List<string> usedDeck)
        {
            if (BattleSystem.instance == null)
            {
                return;
            }

            BattleTeam team = BattleSystem.instance.AllyTeam;
            ReplaceSkillList(team.Skills_Deck, deck);
            ReplaceSkillList(team.Skills_UsedDeck, usedDeck);
            BattleSystem.instance.ActWindow?.Init(team);
        }

        public static void ReceiveDeckContribution(RemotePlayer player, List<string> deck)
        {
            if (player == null)
            {
                return;
            }

            deckContributions[player.getAccountID().ToString()] = new List<string>(deck);
            if (TogetherManager.currentLobby == null || !TogetherManager.currentLobby.isOwner())
            {
                return;
            }

            BuildAndBroadcastCommonDeck();
        }

        public static void ApplyTurnActionNum(int value)
        {
            if (BattleSystem.instance != null && BattleSystem.instance.AllyTeam != null)
            {
                BattleSystem.instance.AllyTeam.TurnActionNum = value;
            }
        }

        public static void ApplyRemoteSkillName(string skillName)
        {
            if (BattleSystem.instance != null && !string.IsNullOrEmpty(skillName))
            {
                BattleChar.SkillNameOutOrigin(BattleSystem.instance, skillName, true);
            }
        }

        public static void ReceiveExchangedSkill(string skillKey)
        {
            if (BattleSystem.instance == null || string.IsNullOrEmpty(skillKey))
            {
                return;
            }

            Skill skill = CreateLocalSkill(skillKey);
            if (skill != null)
            {
                BattleSystem.instance.AllyTeam.Add(skill, false);
            }
        }

        public static void OnLocalSkillUsed(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return;
            }

            NetworkHelper.sendSkillPlayed(skill.MySkill.Name);
            NetworkHelper.sendDeckState();
        }

        public static void OnSkillDrawn(Skill skill)
        {
            ReassignForeignSkillMaster(skill);
            NetworkHelper.sendDeckState();
        }

        private static void SendDeckContributionOnce()
        {
            if (contributionSent)
            {
                return;
            }

            contributionSent = true;
            List<string> keys = LocalInitialDeckKeys();
            if (TogetherManager.currentUser != null)
            {
                deckContributions[TogetherManager.currentUser.getAccountID().ToString()] = new List<string>(keys);
            }
            NetworkHelper.sendDeckContribution(keys);
            if (TogetherManager.currentLobby != null && TogetherManager.currentLobby.isOwner())
            {
                BuildAndBroadcastCommonDeck();
            }
        }

        private static List<string> LocalInitialDeckKeys()
        {
            BattleTeam team = BattleSystem.instance.AllyTeam;
            if (team.ALLSKILLLIST.Count > 0)
            {
                return SkillKeys(team.ALLSKILLLIST);
            }

            return SkillKeys(team.Skills_Deck);
        }

        private static void BuildAndBroadcastCommonDeck()
        {
            List<string> commonDeck = new List<string>();
            foreach (KeyValuePair<string, List<string>> contribution in deckContributions)
            {
                commonDeck.AddRange(contribution.Value);
            }

            if (commonDeck.Count == 0)
            {
                return;
            }

            Shuffle(commonDeck);
            ApplyDeckState(commonDeck, new List<string>());
            NetworkHelper.sendDeckState(commonDeck, new List<string>());
        }

        private static void TrackClickWasteExchange()
        {
            BattleTeam team = BattleSystem.instance.AllyTeam;
            if (lastDiscardCount < 0)
            {
                SnapshotHand();
                return;
            }

            bool discardWasUsed = team.DiscardCount < lastDiscardCount;
            if (discardWasUsed && lastHandKeys.Count > team.Skills.Count)
            {
                List<string> now = SkillKeys(team.Skills);
                string removed = FindRemovedKey(lastHandKeys, now);
                if (!string.IsNullOrEmpty(removed))
                {
                    RemoveOneSkillByKey(team.Skills_UsedDeck, removed);
                    string target = RandomOtherPlayerAccountId();
                    if (!string.IsNullOrEmpty(target))
                    {
                        NetworkHelper.sendExchangeSkill(target, removed);
                    }
                    NetworkHelper.sendDeckState();
                }
            }

            SnapshotHand();
        }

        private static void SnapshotHand()
        {
            lastHandKeys.Clear();
            if (BattleSystem.instance != null)
            {
                lastHandKeys.AddRange(SkillKeys(BattleSystem.instance.AllyTeam.Skills));
                lastDiscardCount = BattleSystem.instance.AllyTeam.DiscardCount;
            }
        }

        private static void SendDeckStateWhenChanged()
        {
            if (TogetherManager.currentLobby == null || !TogetherManager.currentLobby.isOwner())
            {
                return;
            }

            BattleTeam team = BattleSystem.instance.AllyTeam;
            int deckHash = SkillListHash(team.Skills_Deck);
            int usedHash = SkillListHash(team.Skills_UsedDeck);
            if (deckHash == lastDeckHash && usedHash == lastUsedHash)
            {
                return;
            }

            lastDeckHash = deckHash;
            lastUsedHash = usedHash;
            NetworkHelper.sendDeckState();
        }

        private static int SkillListHash(List<Skill> skills)
        {
            unchecked
            {
                int hash = 17;
                foreach (Skill skill in skills)
                {
                    hash = hash * 31 + GetSkillKey(skill).GetHashCode();
                }

                return hash;
            }
        }

        private static void ReplaceSkillList(List<Skill> target, List<string> keys)
        {
            target.Clear();
            foreach (string key in keys)
            {
                Skill skill = CreateLocalSkill(key);
                if (skill != null)
                {
                    target.Add(skill);
                }
            }
        }

        private static Skill CreateLocalSkill(string key)
        {
            BattleTeam team = BattleSystem.instance.AllyTeam;
            BattleChar master = FindBestLocalMaster(key);
            if (master == null)
            {
                master = team.AliveChars.Count > 0 ? team.AliveChars.Random() : team.DummyChar;
            }

            return Skill.TempSkill(key, master, team);
        }

        private static void ReassignForeignSkillMaster(Skill skill)
        {
            if (skill == null || skill.Master == null || BattleSystem.instance == null)
            {
                return;
            }

            BattleTeam team = BattleSystem.instance.AllyTeam;
            if (team.AliveChars.Contains(skill.Master))
            {
                return;
            }

            BattleChar replacement = FindBestLocalMaster(skill.Master.Info.GetData.Role.Key);
            if (replacement != null)
            {
                SetSkillMaster(skill, replacement);
            }
        }

        private static void SetSkillMaster(Skill skill, BattleChar replacement)
        {
            skill.Master = replacement;

            /*System.Reflection.FieldInfo field = typeof(Skill).GetField("Master");
            if (field != null)
            {
                field.SetValue(skill, replacement);
                return;
            }

            System.Reflection.PropertyInfo property = typeof(Skill).GetProperty("Master");
            if (property != null && property.CanWrite)
            {
                property.SetValue(skill, replacement, null);
            }*/
        }

        private static BattleChar FindBestLocalMaster(string skillKey)
        {
            /*foreach (BattleChar ally in BattleSystem.instance.AllyTeam.AliveChars)
            {
                foreach (Skill skill in ally.Skills)
                {
                    if (GetSkillKey(skill) == skillKey)
                    {
                        return ally;
                    }
                }
            }

            return BattleSystem.instance.AllyTeam.AliveChars.Count > 0
                ? BattleSystem.instance.AllyTeam.AliveChars.Random()
                : null;*/

            return BattleSystem.instance.AllyTeam.AliveChars.Random();
        }

        public static string GetSkillKey(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID;
        }

        public static List<string> SkillKeys(List<Skill> skills)
        {
            List<string> keys = new List<string>();
            foreach (Skill skill in skills)
            {
                keys.Add(GetSkillKey(skill));
            }

            return keys;
        }

        private static string FindRemovedKey(List<string> before, List<string> after)
        {
            List<string> remaining = new List<string>(after);
            foreach (string key in before)
            {
                if (remaining.Contains(key))
                {
                    remaining.Remove(key);
                }
                else
                {
                    return key;
                }
            }

            return string.Empty;
        }

        private static void RemoveOneSkillByKey(List<Skill> skills, string key)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (GetSkillKey(skills[i]) == key)
                {
                    skills.RemoveAt(i);
                    return;
                }
            }
        }

        private static string RandomOtherPlayerAccountId()
        {
            if (TogetherManager.currentUser == null)
            {
                return string.Empty;
            }

            List<RemotePlayer> others = new List<RemotePlayer>();
            foreach (RemotePlayer player in TogetherManager.players)
            {
                if (!player.isUser(TogetherManager.currentUser.steamUser))
                {
                    others.Add(player);
                }
            }

            if (others.Count == 0)
            {
                return string.Empty;
            }

            return others[UnityEngine.Random.Range(0, others.Count)].getAccountID().ToString();
        }

        private static void Shuffle(List<string> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                int j = UnityEngine.Random.Range(i, keys.Count);
                string temp = keys[i];
                keys[i] = keys[j];
                keys[j] = temp;
            }
        }
    }

    public class BattleSyncPassive : PassiveBase, IP_BattleStart_Ones, IP_Draw, IP_SkillUseHand_Team
    {
        public void BattleStart(BattleSystem battleSystem)
        {
            MultiplayerBattleSync.EnsureBattlePassive();
        }

        public System.Collections.IEnumerator Draw(Skill skill, bool notDraw)
        {
            if (!notDraw)
            {
                MultiplayerBattleSync.OnSkillDrawn(skill);
            }

            yield return null;
        }

        public void SKillUseHand_Team(Skill skill)
        {
            MultiplayerBattleSync.OnLocalSkillUsed(skill);
        }
    }
}
