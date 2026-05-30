using GameDataEditor;
using MultiplayerDeck.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerDeck
{
    public class BattleSyncManager
    {
        private static BattleSyncManager _instance;
        public static BattleSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BattleSyncManager();
                }
                return _instance;
            }
        }

        private int lastDeckHash;
        private int lastUsedHash;
        private int lastTurnActionNum;
        private int lastAppliedDeckVersion;
        private int lastAppliedUsedDeckVersion;
        private HashSet<int> appliedDrawResultVersions = new HashSet<int>();
        public int deckStateVersion;
        public bool deckSyncing;
        public bool drawResultApplying;
        public bool enemyHpSyncing;
        public bool turnActionNumSyncing;
        public bool turnEnding;
        public BattleStartDeckManager battleStartDeckManager = new BattleStartDeckManager();

        public void Tick()
        {
            if (BattleSystem.instance == null)
            {
                return;
            }
            if (!battleStartDeckManager.localDeck)
            {
                SendDeckStateWhenChanged();
                SendDeckMutationReportWhenChanged();
            }
            SendTurnActionNumWhenChanged();
        }

        public void Initialize()
        {
            lastDeckHash = 0;
            lastUsedHash = 0;
            lastTurnActionNum = 0;
            lastAppliedDeckVersion = 0;
            lastAppliedUsedDeckVersion = 0;
            appliedDrawResultVersions.Clear();
            deckStateVersion = 0;
            deckSyncing = false;
            drawResultApplying = false;
            enemyHpSyncing = false;
            turnActionNumSyncing = false;
            battleStartDeckManager.Initialize();
        }

        public void ApplyDeckState(bool usedDeck, List<Skill> newDeck, int version = 0, RemotePlayer sender = null)
        {
            if (BattleSystem.instance == null || newDeck == null)
            {
                return;
            }
            if (version > 0)
            {
                if (!IsAuthoritativeDeckStateSender(sender))
                {
                    Debug.LogWarning("[DeckSync] Ignored non-host DeckState. version=" + version);
                    return;
                }
                int currentVersion = usedDeck ? lastAppliedUsedDeckVersion : lastAppliedDeckVersion;
                if (version <= currentVersion)
                {
                    Debug.Log("[DeckSync] Ignored stale DeckState. incoming=" + version + ", current=" + currentVersion + ", usedDeck=" + usedDeck);
                    return;
                }
                if (usedDeck)
                {
                    lastAppliedUsedDeckVersion = version;
                }
                else
                {
                    lastAppliedDeckVersion = version;
                }
                deckStateVersion = Math.Max(deckStateVersion, version);
            }
            deckSyncing = true;
            List<Skill> targetDeck = usedDeck ? BattleSystem.instance.AllyTeam.Skills_UsedDeck : BattleSystem.instance.AllyTeam.Skills_Deck;
            targetDeck.Clear();
            targetDeck.AddRange(newDeck);
            UpdateDeckHashes();
        }

        public void RequestDraw(int count)
        {
            if (count <= 0 || !MultiplayerDeck_Plugin.IsMultiplayer || MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                return;
            }

            MessageSerializer.SendRequestDraw(count);
        }

        public void ReceiveDrawRequest(RemotePlayer player, int count)
        {
            if (!MultiplayerDeck_Plugin.IsLobbyOwner || player == null || count <= 0 || BattleSystem.instance == null)
            {
                return;
            }

            BattleSystem.instance.StartCoroutine(AllocateRemoteDraw(player, count));
        }

        private IEnumerator AllocateRemoteDraw(RemotePlayer player, int count)
        {
            List<SkillNetworkDTO> cards = new List<SkillNetworkDTO>();
            BattleTeam team = BattleSystem.instance.AllyTeam;

            for (int i = 0; i < count; i++)
            {
                yield return BattleSystem.instance.StartCoroutine(team.DrawCheck());
                if (team.Skills_Deck.Count == 0)
                {
                    break;
                }

                Skill skill = team.Skills_Deck[0];
                SkillNetworkDTO dto = SkillSerializer.SkillToDTO(skill);
                if (dto != null)
                {
                    cards.Add(dto);
                }
                team.Skills_Deck.RemoveAt(0);
            }

            if (cards.Count == 0)
            {
                yield break;
            }

            deckStateVersion++;
            lastDeckHash = SkillListHash(team.Skills_Deck);
            lastUsedHash = SkillListHash(team.Skills_UsedDeck);
            MessageSerializer.SendDrawResult(player.steamUser.m_SteamID, deckStateVersion, cards);
            MessageSerializer.SendDeckState(team.Skills_Deck, false);
            MessageSerializer.SendDeckState(team.Skills_UsedDeck, true);
        }

        public void ApplyDrawResult(ulong targetPlayerId, int version, List<SkillNetworkDTO> cards)
        {
            if (BattleSystem.instance == null || cards == null || cards.Count == 0)
            {
                return;
            }
            if (version > 0 && appliedDrawResultVersions.Contains(version))
            {
                Debug.Log("[DeckSync] Ignored duplicated DrawResult. version=" + version);
                return;
            }
            if (version > 0)
            {
                appliedDrawResultVersions.Add(version);
            }
            deckStateVersion = Math.Max(deckStateVersion, version);

            bool targetIsCurrentUser = TogetherManager.currentUser != null &&
                TogetherManager.currentUser.steamUser.m_SteamID == targetPlayerId;

            List<Skill> drawnSkills = new List<Skill>();
            foreach (SkillNetworkDTO dto in cards)
            {
                Skill skill = SkillSerializer.CreateSkillFromDTO(dto);
                deckSyncing = true;
                RemoveSkillFromDecks(dto);
                if (targetIsCurrentUser && skill != null)
                {
                    drawnSkills.Add(skill);
                }
            }

            if (targetIsCurrentUser)
            {
                BattleSystem.instance.StartCoroutine(AddDrawnSkills(drawnSkills));
            }
            else
            {
                UpdateDeckHashes();
                deckSyncing = false;
            }
        }

        private IEnumerator AddDrawnSkills(List<Skill> skills)
        {
            drawResultApplying = true;
            foreach (Skill skill in skills)
            {
                if (skill != null)
                {
                    yield return BattleSystem.instance.StartCoroutine(BattleSystem.instance.AllyTeam._Add(skill, NotDraw: false));
                    BattleSystem.instance.AllyTeam.DeckDrawAni();
                }
            }
            UpdateDeckHashes();
            deckSyncing = false;
            drawResultApplying = false;
        }

        public void ReceiveDeckMutationReport(RemotePlayer player, bool usedDeck, List<Skill> newDeck)
        {
            if (!MultiplayerDeck_Plugin.IsLobbyOwner || player == null || BattleSystem.instance == null || newDeck == null)
            {
                return;
            }

            List<Skill> targetDeck = usedDeck ? BattleSystem.instance.AllyTeam.Skills_UsedDeck : BattleSystem.instance.AllyTeam.Skills_Deck;
            targetDeck.Clear();
            targetDeck.AddRange(newDeck);

            deckStateVersion++;
            UpdateDeckHashes();
            MessageSerializer.SendDeckState(targetDeck, usedDeck);
            //Debug.Log("[DeckSync] Accepted DeckMutationReport. player=" + player.userName + ", usedDeck=" + usedDeck + ", count=" + newDeck.Count + ", version=" + deckStateVersion);
        }

        public void ApplyEnemyHp(string enemyKey, int position, int hp)
        {
            if (BattleSystem.instance == null)
            {
                return;
            }
            enemyHpSyncing = true;
            List<BattleChar> list = BattleSystem.instance.EnemyTeam.AliveChars.FindAll(enemy => enemy.Info.KeyData == enemyKey);
            if (list.IsValidIndex(position))
            {
                list[position].HP = hp;
            }
            enemyHpSyncing = false;
        }

        public void ApplyTurnActionNum(int value)
        {
            if (BattleSystem.instance != null && BattleSystem.instance.AllyTeam != null)
            {
                turnActionNumSyncing = true;
                BattleSystem.instance.AllyTeam.TurnActionNum = value;
                if (!BattleSystem.instance.EnemyCheck && !turnEnding)
                {
                    BattleSystem.instance.StartCoroutine(BattleSystem.instance.EnemyTurn(false));
                }
                turnActionNumSyncing = false;
            }
        }

        public void ReceiveExchangedSkill(Skill skill)
        {
            if (BattleSystem.instance == null || skill == null)
            {
                return;
            }
            BattleSystem.instance.AllyTeam.Add(skill, true);
        }

        public void ApplyRemoteSkillName(string skillName)
        {
            if (BattleSystem.instance != null && !string.IsNullOrEmpty(skillName))
            {
                BattleChar.SkillNameOutOrigin(BattleSystem.instance, skillName, true);
            }
        }

        private void SendDeckStateWhenChanged()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer)
            {
                return;
            }
            if (!MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                return;
            }
            if (!battleStartDeckManager.deckReceived)
            {
                return;
            }

            BattleTeam team = BattleSystem.instance.AllyTeam;
            int deckHash = SkillListHash(team.Skills_Deck);
            int usedHash = SkillListHash(team.Skills_UsedDeck);
            bool deckChange = (deckHash != lastDeckHash);
            bool usedChange = (usedHash != lastUsedHash);

            lastDeckHash = deckHash;
            lastUsedHash = usedHash;
            if (deckSyncing)
            {
                deckSyncing = false;
                return;
            }
            if (deckChange)
            {
                deckStateVersion++;
                MessageSerializer.SendDeckState(team.Skills_Deck, false);
            }
            if (usedChange)
            {
                deckStateVersion++;
                MessageSerializer.SendDeckState(team.Skills_UsedDeck, true);
            }
            
        }

        private void SendDeckMutationReportWhenChanged()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer || MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                return;
            }
            if (!battleStartDeckManager.deckReceived || deckSyncing || drawResultApplying)
            {
                if (deckSyncing && !drawResultApplying)
                {
                    deckSyncing = false;
                }
                return;
            }

            BattleTeam team = BattleSystem.instance.AllyTeam;
            int deckHash = SkillListHash(team.Skills_Deck);
            int usedHash = SkillListHash(team.Skills_UsedDeck);
            bool deckChange = deckHash != lastDeckHash;
            bool usedChange = usedHash != lastUsedHash;

            lastDeckHash = deckHash;
            lastUsedHash = usedHash;

            if (deckChange)
            {
                MessageSerializer.SendDeckMutationReport(team.Skills_Deck, false);
            }
            if (usedChange)
            {
                MessageSerializer.SendDeckMutationReport(team.Skills_UsedDeck, true);
            }
        }

        private void SendTurnActionNumWhenChanged()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer)
            {
                return;
            }

            if (turnEnding)
            {
                return;
            }

            int newTurnActionNum = BattleSystem.instance.AllyTeam.TurnActionNum;
            if (newTurnActionNum != lastTurnActionNum)
            {
                lastTurnActionNum = newTurnActionNum;
                if (turnActionNumSyncing)
                {
                    turnActionNumSyncing = false;
                    return;
                }
                if (newTurnActionNum > 0)
                {
                    MessageSerializer.SendTurnActionNum(newTurnActionNum);
                }
            }
        }

        private static int SkillListHash(List<Skill> skills)
        {
            unchecked
            {
                int hash = 17;
                foreach (Skill skill in skills)
                {
                    hash = hash * 31 + GetSkillKey(skill).GetHashCode();
                    hash = hash * 31 + (skill.CharinfoSkilldata?.Seed ?? 0);
                }
                return hash;
            }
        }        

        private void UpdateDeckHashes()
        {
            if (BattleSystem.instance == null || BattleSystem.instance.AllyTeam == null)
            {
                return;
            }

            lastDeckHash = SkillListHash(BattleSystem.instance.AllyTeam.Skills_Deck);
            lastUsedHash = SkillListHash(BattleSystem.instance.AllyTeam.Skills_UsedDeck);
        }

        public static string GetSkillKey(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID;
        }

        private static bool IsAuthoritativeDeckStateSender(RemotePlayer sender)
        {
            if (TogetherManager.ActiveLobby == null)
            {
                return false;
            }
            if (sender == null)
            {
                return MultiplayerDeck_Plugin.IsLobbyOwner;
            }
            return TogetherManager.ActiveLobby.ownerID.m_SteamID == sender.steamUser.m_SteamID;
        }

        private static void RemoveSkillFromDecks(SkillNetworkDTO dto)
        {
            if (dto == null || BattleSystem.instance == null)
            {
                return;
            }

            RemoveSkillFromList(BattleSystem.instance.AllyTeam.Skills_Deck, dto);
            RemoveSkillFromList(BattleSystem.instance.AllyTeam.Skills_UsedDeck, dto);
        }

        private static bool RemoveSkillFromList(List<Skill> skills, SkillNetworkDTO dto)
        {
            if (skills == null)
            {
                return false;
            }

            int index = skills.FindIndex(skill =>
                skill != null &&
                skill.CharinfoSkilldata != null &&
                dto.Seed != 0 &&
                skill.CharinfoSkilldata.Seed == dto.Seed);

            if (index < 0)
            {
                index = skills.FindIndex(skill =>
                    GetSkillKey(skill) == dto.SkillKey &&
                    (skill.Master?.Info?.KeyData ?? "") == (dto.MasterKey ?? ""));
            }

            if (index < 0)
            {
                return false;
            }

            skills.RemoveAt(index);
            return true;
        }

        public static ulong RandomOtherPlayerId()
        {
            if (TogetherManager.currentUser == null)
            {
                return default;
            }

            List<RemotePlayer> others = new List<RemotePlayer>();
            foreach (RemotePlayer player in TogetherManager.players)
            {
                if (!player.IsUser(TogetherManager.currentUser.steamUser))
                {
                    others.Add(player);
                }
            }

            if (others.Count == 0)
            {
                return default;
            }

            return others[UnityEngine.Random.Range(0, others.Count)].steamUser.m_SteamID;
        }

        public void SendRequestForBattleStartDeck()
        {
            battleStartDeckManager.SendRequestForBattleStartDeck();
        }

        public void SendPersonalDeck()
        {
            battleStartDeckManager.SendPersonalDeck();
        }

        public void ReceiveDeckContribution(RemotePlayer player, List<SkillNetworkDTO> deck)
        {
            battleStartDeckManager.ReceiveDeckContribution(player, deck);
        }

        public void SendCombinedDeck()
        {
            battleStartDeckManager.SendCombinedDeck();
        }

        public void ReceiveCombinedDeck(RemotePlayer player, List<Skill> deck)
        {
           battleStartDeckManager.ReceiveCombinedDeck(player, deck);
        }
    }

    public class BattleStartDeckManager
    {
        private List<SkillNetworkDTO> combinedDeck = new List<SkillNetworkDTO>();
        private HashSet<ulong> deckContributions = new HashSet<ulong>();
        private bool deckSent;
        public bool deckReceived;
        public bool localDeck;
        
        public void Initialize()
        {
            combinedDeck.Clear();
            deckContributions.Clear();
            deckSent = false;
            deckReceived = false;
            localDeck = false;
        }

        public void SendRequestForBattleStartDeck()
        {
            if (TogetherManager.ActiveLobby == null || !MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                return;
            }

            if (BattleSystem.instance == null || BattleSystem.instance.AllyTeam == null)
            {
                Debug.LogWarning("[DeckSync] Cannot request deck before BattleSystem is ready.");
                return;
            }

            combinedDeck.AddRange(BattleSystem.instance.AllyTeam.Skills_Deck
                .Select(s => SkillSerializer.SkillToDTO(s))
                .Where(dto => dto != null));

            Debug.Log("[DeckSync] Host deck DTO count: " + combinedDeck.Count);

            if (TogetherManager.players.Count <= 1)
            {
                deckReceived = true;
                return;
            }

            BattleSystem.instance.StartCoroutine(SendRequest());
            IEnumerator SendRequest()
            {
                while (deckContributions.Count < TogetherManager.players.Count - 1)
                {
                    Debug.Log("[DeckSync] Host sending request for battle start deck");

                    MessageSerializer.SendData(NetDataType.RequestForBattleStartDeck);
                    yield return new WaitForSecondsRealtime(1f);
                }
            }
        }

        public void SendPersonalDeck()
        {
            if (BattleSystem.instance == null || deckSent)
            {
                return;
            }

            int deckCount = BattleSystem.instance.AllyTeam?.Skills_Deck?.Count ?? 0;
            Debug.Log("[DeckSync] Client received request. Send personal deck count: " + deckCount);

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.BattleStartDeck);
                SkillSerializer.SkillListSerialize(binaryWriter, BattleSystem.instance.AllyTeam.Skills_Deck);
            }
            NetworkHelper.Service()?.SendPacket(memoryStream.ToArray());

            deckSent = true;
        }

        public void ReceiveDeckContribution(RemotePlayer player, List<SkillNetworkDTO> deck)
        {
            if (player == null)
            {
                return;
            }
            ulong playerID = player.steamUser.m_SteamID;
            if (!deckContributions.Contains(playerID))
            {
                if (deck != null)
                {
                    combinedDeck.AddRange(deck.Where(dto => dto != null));
                }
                deckContributions.Add(playerID);

                Debug.Log("[DeckSync] Host received deck contribution. Player ID: " + playerID + ", dto count: " + (deck == null ? -1 : deck.Count));
            }

            if (deckContributions.Count == TogetherManager.players.Count - 1)
            {
                combinedDeck = BattleTeam.Shuffle<SkillNetworkDTO>(combinedDeck);

                List<Skill> newDeck = combinedDeck
                    .Select(s => SkillSerializer.CreateSkillFromDTO(s))
                    .Where(s => s != null).ToList();

                Debug.Log("[DeckSync] Host rebuilt combined deck count: " + newDeck.Count);
                if (combinedDeck.Count > 0 && newDeck.Count == 0)
                {
                    Debug.LogError("[DeckSync] Host rebuilt combined deck as 0. Keeping local deck to avoid clearing it.");
                }
                else
                {
                    BattleSyncManager.Instance.ApplyDeckState(false, newDeck);
                }
                deckReceived = true;
                
                SendCombinedDeck();
            }
        }

        public void SendCombinedDeck()
        {
            if (BattleSystem.instance == null)
            {
                return;
            }

            Debug.Log("[DeckSync] Host sending combined DTO count: " + combinedDeck.Count);

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.BattleStartDeck);
                SkillSerializer.SkillDTOListSerialize(binaryWriter, combinedDeck);
            }
            NetworkHelper.Service()?.SendPacket(memoryStream.ToArray());
        }

        public void ReceiveCombinedDeck(RemotePlayer player, List<Skill> deck)
        {
            Debug.Log("[DeckSync] Client ReceiveCombinedDeck deck count: " + (deck == null ? -1 : deck.Count));

            if (TogetherManager.ActiveLobby == null || MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                return;
            }

            if (deck == null || deck.Count == 0)
            {
                Debug.LogError("[DeckSync] Client received empty combined deck. Keeping local deck and releasing battle start wait.");
                deckReceived = true;
                return;
            }

            BattleSyncManager.Instance.ApplyDeckState(false, deck);
            deckReceived = true;
        }
    }
}
