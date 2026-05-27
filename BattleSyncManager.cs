using GameDataEditor;
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
        public bool deckSyncing;
        public bool enemyHpSyncing;
        public bool turnActionNumSyncing;
        public BattleStartDeckManager battleStartDeckManager = new BattleStartDeckManager();

        public void Tick()
        {
            if (BattleSystem.instance == null)
            {
                return;
            }
            SendDeckStateWhenChanged();
            SendTurnActionNumWhenChanged();
        }

        public void Initialize()
        {
            lastDeckHash = 0;
            lastUsedHash = 0;
            lastTurnActionNum = 0;
            deckSyncing = false;
            enemyHpSyncing = false;
            turnActionNumSyncing = false;
            battleStartDeckManager.Initialize();
        }

        public void ApplyDeckState(bool usedDeck, List<Skill> newDeck)
        {
            if (BattleSystem.instance == null || newDeck == null)
            {
                return;
            }
            deckSyncing = true;
            List<Skill> targetDeck = usedDeck ? BattleSystem.instance.AllyTeam.Skills_UsedDeck : BattleSystem.instance.AllyTeam.Skills_Deck;
            targetDeck.Clear();
            targetDeck.AddRange(newDeck);
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
                if (!BattleSystem.instance.EnemyCheck && !BattleSystem.instance.NowEndedTurn)
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
                NetworkHelper.SendDeckState(team.Skills_Deck, false);
            }
            if (usedChange)
            {
                NetworkHelper.SendDeckState(team.Skills_UsedDeck, true);
            }
            
        }

        private void SendTurnActionNumWhenChanged()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer)
            {
                return;
            }

            if (BattleSystem.instance.NowEndedTurn)
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
                    NetworkHelper.SendTurnActionNum(newTurnActionNum);
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
                }

                return hash;
            }
        }        

        public static string GetSkillKey(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID;
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
        
        public void Initialize()
        {
            combinedDeck.Clear();
            deckContributions.Clear();
            deckSent = false;
            deckReceived = false;
        }

        public void SendRequestForBattleStartDeck()
        {
            Debug.Log("[DeckSync] SendRequestForBattleStartDeck()");

            if (TogetherManager.currentLobby == null || !TogetherManager.currentLobby.IsOwner())
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
                Debug.Log("[DeckSync] BattleSystem.instance.StartCoroutine(SendRequest());");

                while (deckContributions.Count < TogetherManager.players.Count - 1)
                {
                    Debug.Log("[DeckSync] Host sending request for battle start deck");

                    NetworkHelper.SendData(NetDataType.RequestForBattleStartDeck);
                    yield return new WaitForSecondsRealtime(0.2f);
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
                    .Select((s, index) => SkillSerializer.CreateSkillFromDTO(s, index))
                    .Where(s => s != null)
                    .ToList();


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

            if (TogetherManager.currentLobby == null || TogetherManager.currentLobby.IsOwner())
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
