using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 战斗消息：BattleStart, DeckSync, Draw, EnemyHP, TurnActionNum, ExchangeSkill,
    /// SkillPlayed, NetWorkSkillEffect。
    /// </summary>
    public class BattleMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public BattleMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.BattleStart,              ReadBattleStart },
                { NetDataType.RequestForBattleStartDeck, ReadRequestForBattleStartDeck },
                { NetDataType.BattleStartDeck,           ReadBattleStartDeck },
                { NetDataType.DeckState,                 ReadDeckState },
                { NetDataType.DeckMutationReport,        ReadDeckMutationReport },
                { NetDataType.RequestDraw,               ReadRequestDraw },
                { NetDataType.DrawResult,                ReadDrawResult },
                { NetDataType.EnemyHP,                   ReadEnemyHP },
                { NetDataType.TurnActionNum,             ReadTurnActionNum },
                { NetDataType.ExchangeSkill,             ReadExchangeSkill },
                { NetDataType.SkillPlayed,               ReadSkillPlayed },
                { NetDataType.NetWorkSkillEffect,        ReadNetWorkSkillEffect },
            };
        }

        private static void ReadBattleStart(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            string queueData = br.ReadString();
            bool normalBattle = br.ReadBoolean();
            bool cursed = br.ReadBoolean();
            string rewardKey = br.ReadString();
            string preset = br.ReadString();
            bool noGameover = br.ReadBoolean();
            Debug.Log("[MultiplayerDeck] BattleStart " + queueData);
            StageSyncManager.Instance.StartBattleFromNetwork(queueData, normalBattle, cursed, rewardKey, preset, noGameover);
        }

        private static void ReadRequestForBattleStartDeck(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            if (NetworkHelper.IsLobbyActive() && !NetworkHelper.IsLobbyOwner())
            {
                BattleSyncManager.Instance.SendPersonalDeck();
            }
        }

        private static void ReadBattleStartDeck(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            Debug.Log("[DeckSync] Received BattleStartDeck. IsOwner=" + NetworkHelper.IsLobbyOwner());
            if (NetworkHelper.IsLobbyOwner())
            {
                List<SkillNetworkDTO> deck = SkillSerializer.SkillDTOListDeserialize(br);
                BattleSyncManager.Instance.ReceiveDeckContribution(sender, deck);
            }
            else
            {
                List<Skill> deck = SkillSerializer.SkillListDeserialize(br);
                BattleSyncManager.Instance.ReceiveCombinedDeck(sender, deck);
            }
        }

        private static void ReadDeckState(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            if (!BattleSyncManager.Instance.battleStartDeckManager.deckReceived)
            {
                Debug.Log("[DeckSync] Ignored DeckState before battle start deck sync completed.");
                return;
            }
            int version = br.ReadInt32();
            bool usedDeck = br.ReadBoolean();
            List<Skill> skills = SkillSerializer.SkillListDeserialize(br);
            BattleSyncManager.Instance.ApplyDeckState(usedDeck, skills, version, sender);
        }

        private static void ReadDeckMutationReport(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            bool usedDeck = br.ReadBoolean();
            List<Skill> skills = SkillSerializer.SkillListDeserialize(br);
            BattleSyncManager.Instance.ReceiveDeckMutationReport(sender, usedDeck, skills);
        }

        private static void ReadRequestDraw(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            int count = br.ReadInt32();
            BattleSyncManager.Instance.ReceiveDrawRequest(sender, count);
        }

        private static void ReadDrawResult(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            ulong targetPlayerId = br.ReadUInt64();
            int version = br.ReadInt32();
            List<SkillNetworkDTO> cards = SkillSerializer.SkillDTOListDeserialize(br);
            BattleSyncManager.Instance.ApplyDrawResult(targetPlayerId, version, cards);
        }

        private static void ReadEnemyHP(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            string enemyKey = br.ReadString();
            int position = br.ReadInt32();
            int hp = br.ReadInt32();
            BattleSyncManager.Instance.ApplyEnemyHp(enemyKey, position, hp);
        }

        private static void ReadTurnActionNum(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            int value = br.ReadInt32();
            BattleSyncManager.Instance.ApplyTurnActionNum(value);
        }

        private static void ReadExchangeSkill(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            ulong targetAccountId = br.ReadUInt64();
            Skill skill = SkillSerializer.SkillDeserialize(br);
            if (skill == null) return;
            if (sender != TogetherManager.currentUser
                && targetAccountId == TogetherManager.currentUser.steamUser.m_SteamID)
            {
                BattleSyncManager.Instance.ReceiveExchangedSkill(skill);
            }
        }

        private static void ReadSkillPlayed(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            string skillName = br.ReadString();
            if (sender != TogetherManager.currentUser)
            {
                BattleSyncManager.Instance.ApplyRemoteSkillName(skillName);
            }
        }

        private static void ReadNetWorkSkillEffect(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            SkillExtended_Network.ApplySkillEffect(br);
        }
    }
}
