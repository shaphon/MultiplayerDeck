using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 大厅生命周期消息：LobbyClosed, Test。
    /// </summary>
    public class LobbyMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public LobbyMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.LobbyClosed, ReadLobbyClosed },
                { NetDataType.Test,        ReadTest },
            };
        }

        private static void ReadLobbyClosed(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            ulong lobbyId = br.ReadUInt64();
            string reason = br.ReadString();
            if (TogetherManager.ActiveLobby != null
                && TogetherManager.ActiveLobby.steamID.m_SteamID == lobbyId)
            {
                Debug.Log("[MultiplayerDeck] Lobby closed by owner: " + reason);
                MultiLucySkelController.CleanupAllRemotePlayers();
                TogetherManager.ClearMultiplayerData();
                VoteManager.Instance.AbortAllVotes();
            }
        }

        private static void ReadTest(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            string text = br.ReadString();
            Debug.Log("Test " + text);
            if (FieldSystem.instance != null)
            {
                FieldSystem.instance.BattleStart(
                    new GameDataEditor.GDEEnemyQueueData(text),
                    StageSystem.instance.StageData.BattleMap.Key,
                    true, false, "", "", false);
            }
        }
    }
}
