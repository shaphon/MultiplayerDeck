using GameDataEditor;
using MultiplayerDeck.Network.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network
{
    /// <summary>
    /// 消息序列化（发送端）。消息接收路由已迁移到 MessageDispatcher。
    /// </summary>
    public static class MessageSerializer
    {
        static MessageSerializer()
        {
            MessageDispatcher.Initialize();
        }

        public static void SendData(NetDataType type)
        {
            byte[] array = GenerateData(type);
            if (array != null)
            {
                NetworkHelper.SendToAll(array);
            }
        }

        private static byte[] GenerateData(NetDataType type)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)type);
                switch (type)
                {
                    case NetDataType.Test:
                    {
                        binaryWriter.Write("Queue_S4_King");
                        break;
                    }
                    default:
                    binaryWriter.Write((int)type);
                    break;
                }
            }
            return memoryStream.ToArray();
        }

        public static void SendBattleStart(string queueData, bool normalBattle, bool cursed, string rewardKey, string preset, bool noGameover)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.BattleStart);
                binaryWriter.Write(queueData ?? string.Empty);
                binaryWriter.Write(normalBattle);
                binaryWriter.Write(cursed);
                binaryWriter.Write(rewardKey);
                binaryWriter.Write(preset);
                binaryWriter.Write(noGameover);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendEnemyHpChange(string enemy, int position, int hp)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.EnemyHP);
                binaryWriter.Write(enemy);
                binaryWriter.Write(position);
                binaryWriter.Write(hp);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendVote(VoteManager.VoteTheme voteTheme, ulong playerId, bool cancel)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.Vote);
                binaryWriter.Write((int)voteTheme);
                binaryWriter.Write(playerId);
                binaryWriter.Write(cancel);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendDeckState(List<Skill> skills, bool usedDeck = false)
        {
            if (BattleSystem.instance == null)
            {
                return;
            }

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.DeckState);
                binaryWriter.Write(BattleSyncManager.Instance.deckStateVersion);
                binaryWriter.Write(usedDeck);
                SkillSerializer.SkillListSerialize(binaryWriter, skills);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendDeckMutationReport(List<Skill> skills, bool usedDeck = false)
        {
            if (BattleSystem.instance == null)
            {
                return;
            }

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.DeckMutationReport);
                binaryWriter.Write(usedDeck);
                SkillSerializer.SkillListSerialize(binaryWriter, skills);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendRequestDraw(int count)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.RequestDraw);
                binaryWriter.Write(count);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendDrawResult(ulong targetPlayerId, int version, List<SkillNetworkDTO> cards)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.DrawResult);
                binaryWriter.Write(targetPlayerId);
                binaryWriter.Write(version);
                SkillSerializer.SkillDTOListSerialize(binaryWriter, cards);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendTurnActionNum(int value)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.TurnActionNum);
                binaryWriter.Write(value);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendSkillPlayed(string skillName)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.SkillPlayed);
                binaryWriter.Write(skillName ?? string.Empty);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendExchangeSkill(ulong targetAccountId, Skill skill)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.ExchangeSkill);
                binaryWriter.Write(targetAccountId);
                SkillSerializer.SkillSerialize(binaryWriter, skill);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendMonsterClear(Vector2 pos)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.MonsterClear);
                binaryWriter.Write(pos.x);
                binaryWriter.Write(pos.y);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendPosition(Vector2 pos, float jumpY, bool isMoving, bool facingRight, string skinName = null)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.PlayerPosition);
                binaryWriter.Write(pos.x);
                binaryWriter.Write(pos.y);
                binaryWriter.Write(jumpY);
                binaryWriter.Write(Time.time);
                binaryWriter.Write(isMoving);
                binaryWriter.Write(facingRight);
                binaryWriter.Write(skinName ?? string.Empty);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendLobbyClosed(string reason)
        {
            if (TogetherManager.ActiveLobby == null)
            {
                return;
            }
            ulong lobbyId = TogetherManager.ActiveLobby.steamID.m_SteamID;

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.LobbyClosed);
                binaryWriter.Write(lobbyId);
                binaryWriter.Write(reason ?? string.Empty);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendVoteStart(VoteManager.VoteTheme voteTheme)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.VoteStart);
                binaryWriter.Write((int)voteTheme);
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }

        public static void SendBuffAdd(string buffKey, string targetCharKey, int targetPosition, bool targetIsAlly, string userCharKey, int userPosition, bool userIsAlly, int stackNum, int lifetime, byte[] customData)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.BuffAdd);
                binaryWriter.Write(buffKey ?? "");
                binaryWriter.Write(targetCharKey ?? "");
                binaryWriter.Write(targetPosition);
                binaryWriter.Write(targetIsAlly);
                binaryWriter.Write(userCharKey ?? "");
                binaryWriter.Write(userPosition);
                binaryWriter.Write(userIsAlly);
                binaryWriter.Write(stackNum);
                binaryWriter.Write(lifetime);
                if (customData != null && customData.Length > 0)
                {
                    binaryWriter.Write(customData.Length);
                    binaryWriter.Write(customData);
                }
                else
                {
                    binaryWriter.Write(0);
                }
            }
            NetworkHelper.SendToAll(memoryStream.ToArray());
        }
    }
}
