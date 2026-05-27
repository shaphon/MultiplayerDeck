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
            if (BattleSystem.instance == null)
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
                BattleSystem.instance.StartCoroutine(BattleSystem.instance.EnemyTurn(false));
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

            BattleTeam team = BattleSystem.instance.AllyTeam;
            int deckHash = SkillListHash(team.Skills_Deck);
            int usedHash = SkillListHash(team.Skills_UsedDeck);
            bool deckChange = (deckHash != lastDeckHash);
            bool usedChange = (deckHash != lastDeckHash);

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

            int newTurnActionNum = BattleSystem.instance.AllyTeam.TurnActionNum;
            if (newTurnActionNum != lastTurnActionNum)
            {
                lastTurnActionNum = newTurnActionNum;
                if (turnActionNumSyncing)
                {
                    turnActionNumSyncing = false;
                    return;
                }
                NetworkHelper.SendTurnActionNum(newTurnActionNum);
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
            if (TogetherManager.currentLobby == null || !TogetherManager.currentLobby.IsOwner())
            {
                return;
            }

            combinedDeck.AddRange(BattleSystem.instance.AllyTeam.Skills_Deck.Select(s => SkillSerializer.SkillToDTO(s)));

            BattleSystem.instance.StartCoroutine(SendRequest());
            IEnumerator SendRequest()
            {
                while (deckContributions.Count < TogetherManager.players.Count - 1)
                {
                    NetworkHelper.SendData(NetDataType.RequestForBattleStartDeck);
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
            NetworkHelper.SendDeckState(BattleSystem.instance.AllyTeam.Skills_Deck);
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
                combinedDeck.AddRange(deck);
                deckContributions.Add(playerID);
            }

            if (deckContributions.Count == TogetherManager.players.Count - 1)
            {
                List<Skill> newDeck = combinedDeck.Select(s => SkillSerializer.CreateSkillFromDTO(s)).ToList();
                BattleSyncManager.Instance.ApplyDeckState(false, newDeck);
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

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.DeckState);
                binaryWriter.Write(false);
                SkillSerializer.SkillDTOListSerialize(binaryWriter, combinedDeck);
            }
            NetworkHelper.Service()?.SendPacket(memoryStream.ToArray());
        }

        public void ReceiveCombinedDeck(RemotePlayer player, List<Skill> deck)
        {
            if (TogetherManager.currentLobby == null || TogetherManager.currentLobby.IsOwner())
            {
                return;
            }
            BattleSyncManager.Instance.ApplyDeckState(false, deck);
            BattleSystem.instance.AllyTeam.ShuffleDeck();
            deckReceived = true;
        }
    }
}
