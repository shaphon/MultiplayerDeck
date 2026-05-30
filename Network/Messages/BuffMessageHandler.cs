using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// Buff 同步消息：BuffAdd。
    /// </summary>
    public class BuffMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public BuffMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.BuffAdd, ReadBuffAdd },
            };
        }

        private static void ReadBuffAdd(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            string buffKey = br.ReadString();
            string targetCharKey = br.ReadString();
            int targetPosition = br.ReadInt32();
            bool targetIsAlly = br.ReadBoolean();
            string userCharKey = br.ReadString();
            int userPosition = br.ReadInt32();
            bool userIsAlly = br.ReadBoolean();
            int stackNum = br.ReadInt32();
            int lifetime = br.ReadInt32();
            int customDataLen = br.ReadInt32();
            byte[] customData = null;
            if (customDataLen > 0)
            {
                customData = br.ReadBytes(customDataLen);
            }
            if (sender != TogetherManager.currentUser)
            {
                BuffSyncManager.HandleRemoteBuffAdd(buffKey, targetCharKey, targetPosition, targetIsAlly,
                    userCharKey, userPosition, userIsAlly, stackNum, lifetime, customData);
            }
        }
    }
}
