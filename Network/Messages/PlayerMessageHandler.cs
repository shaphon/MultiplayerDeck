using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 玩家同步消息：PlayerPosition。
    /// </summary>
    public class PlayerMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public PlayerMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.PlayerPosition, ReadPlayerPosition },
            };
        }

        private static void ReadPlayerPosition(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float jumpY = br.ReadSingle();
            float timestamp = br.ReadSingle();
            bool isMoving = br.ReadBoolean();
            bool facingRight = br.ReadBoolean();
            string skinName = br.ReadString();
            if (sender != null)
            {
                MultiLucySkelController.OnReceiveRemoteState(
                    sender.steamUser.m_SteamID,
                    new Vector2(x, y), jumpY, timestamp, isMoving, facingRight, skinName);
            }
        }
    }
}
