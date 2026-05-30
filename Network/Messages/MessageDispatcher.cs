using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 消息路由器。根据 NetDataType 直接分发到对应的 MessageHandler 委托。
    /// 无 switch-case，完全是字典查找。
    /// </summary>
    public static class MessageDispatcher
    {
        private static readonly Dictionary<NetDataType, MessageHandler> _handlers =
            new Dictionary<NetDataType, MessageHandler>();

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            Register(new BattleMessageHandler());
            Register(new StageMessageHandler());
            Register(new VoteMessageHandler());
            Register(new PlayerMessageHandler());
            Register(new BuffMessageHandler());
            Register(new LobbyMessageHandler());

            _initialized = true;
        }

        public static void Register(IMessageHandler handler)
        {
            foreach (var kvp in handler.Handlers)
            {
                if (_handlers.ContainsKey(kvp.Key))
                {
                    Debug.LogWarning("[MessageDispatcher] Duplicate handler for " + kvp.Key);
                }
                _handlers[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 路由一条消息。读取 NetDataType → 查表 → 调用委托。
        /// </summary>
        public static void Dispatch(byte[] data, RemotePlayer sender)
        {
            if (sender != null &&
                TogetherManager.currentUser != null &&
                sender.IsUser(TogetherManager.currentUser.steamUser))
            {
                return;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    NetDataType type = (NetDataType)br.ReadInt32();

                    MessageHandler handler;
                    if (_handlers.TryGetValue(type, out handler))
                    {
                        handler(br, data, sender);
                    }
                    else
                    {
                        Debug.LogWarning("[MessageDispatcher] Unhandled message type: " + type);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
