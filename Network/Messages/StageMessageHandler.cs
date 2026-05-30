using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 关卡/地图消息：StageMap, NextStageComplete, MonsterClear, BossClear。
    /// </summary>
    public class StageMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public StageMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.StageMap,          ReadStageMap },
                { NetDataType.NextStageComplete, ReadNextStageComplete },
                { NetDataType.MonsterClear,      ReadMonsterClear },
                { NetDataType.BossClear,         ReadBossClear },
            };
        }

        private static void ReadStageMap(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            Debug.Log("[MultiplayerDeck] Received StageMap, IsLobbyOwner=" + MultiplayerDeck_Plugin.IsLobbyOwner);
            if (!MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                StageMapSerializer.mapPacket = StageMapSerializer.DeserializeMapPacket(raw);
                StageSyncManager.Instance.GotoNextStage();
            }
        }

        private static void ReadNextStageComplete(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            if (MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                StageSyncManager.Instance.PlayerNextStageComplete(sender);
            }
            else
            {
                VoteManager.Instance.syncing = false;
            }
        }

        private static void ReadMonsterClear(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            StageSyncManager.Instance.MonsterClear(new Vector2(x, y));
        }

        private static void ReadBossClear(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            StageSyncManager.Instance.bossClear = true;
        }
    }
}
