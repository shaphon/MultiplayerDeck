using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 消息路由器。通过反射自动发现所有 NetworkMessage 子类，无需手动注册。
    ///
    /// 发送：MessageDispatcher.Send(new XxxMessage { ... });
    /// 接收：自动反序列化 → 调用 message.Handle(sender)
    /// </summary>
    public static class MessageDispatcher
    {
        /// <summary>MessageId (hash) → 工厂方法</summary>
        private static readonly Dictionary<int, Func<NetworkMessage>> _factories =
            new Dictionary<int, Func<NetworkMessage>>();

        /// <summary>MessageId → Type，用于调试</summary>
        private static readonly Dictionary<int, Type> _typeMap =
            new Dictionary<int, Type>();

        /// <summary>message Type → MessageId，用于发送时快速查找</summary>
        private static readonly Dictionary<Type, int> _typeToId =
            new Dictionary<Type, int>();

        private static bool _initialized;

        static MessageDispatcher()
        {
            AutoRegister();
        }

        /// <summary>
        /// 显式初始化（确保静态构造函数已执行）。
        /// </summary>
        public static void Initialize()
        {
            // 静态构造函数已处理，此方法用于触发类型加载
        }

        /// <summary>
        /// 扫描当前 Assembly 中所有 NetworkMessage 子类，自动注册。
        /// </summary>
        private static void AutoRegister()
        {
            if (_initialized) return;

            var messageTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(NetworkMessage)) && !t.IsAbstract)
                .ToList();

            var seenHashes = new Dictionary<int, Type>();

            foreach (var type in messageTypes)
            {
                int msgId = MessageHash.Compute(type.FullName);

                if (seenHashes.TryGetValue(msgId, out var existingType))
                {
                    Debug.LogError(
                        $"[MessageDispatcher] Hash collision! " +
                        $"{type.FullName} and {existingType.FullName} both hash to {msgId:X8}. " +
                        $"Rename one of the classes.");
                    continue;
                }

                seenHashes[msgId] = type;
                _typeMap[msgId] = type;
                _typeToId[type] = msgId;

                // 编译工厂委托
                var factory = CreateFactory(type);
                _factories[msgId] = factory;
            }

            _initialized = true;
            Debug.Log($"[MessageDispatcher] Auto-registered {_factories.Count} message types.");
        }

        private static Func<NetworkMessage> CreateFactory(Type type)
        {
            // 使用 Activator 作为默认工厂。对于频繁创建的消息类型，可后续优化为 Expression.New。
            return () => (NetworkMessage)Activator.CreateInstance(type);
        }

        // ============ Send ============

        /// <summary>
        /// 序列化并广播一条消息到所有其他玩家。
        /// 消息的 MessageId 自动写入数据流头部。
        /// </summary>
        public static void Send(NetworkMessage message)
        {
            if (message == null) return;

            Type type = message.GetType();
            if (!_typeToId.TryGetValue(type, out int msgId))
            {
                Debug.LogError($"[MessageDispatcher] Unregistered message type: {type.FullName}");
                return;
            }

            message.MessageId = msgId;

            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(msgId);
                message.Serialize(bw);
            }
            NetworkHelper.SendToAll(ms.ToArray());
        }

        /// <summary>
        /// 序列化并通过指定 service 发送（用于需要指定传输通道的场景）。
        /// </summary>
        public static void SendVia(NetworkMessage message, SteamIntegration service)
        {
            if (message == null || service == null) return;

            Type type = message.GetType();
            if (!_typeToId.TryGetValue(type, out int msgId))
            {
                Debug.LogError($"[MessageDispatcher] Unregistered message type: {type.FullName}");
                return;
            }

            message.MessageId = msgId;

            MemoryStream ms = new MemoryStream();
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(msgId);
                message.Serialize(bw);
            }
            service.SendPacket(ms.ToArray());
        }

        // ============ Receive ============

        /// <summary>
        /// 接收一条消息。由 NetworkHelper.Update() 调用。
        /// </summary>
        public static void Dispatch(byte[] data, RemotePlayer sender)
        {
            // 过滤自己发送的消息
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
                    int msgId = br.ReadInt32();

                    if (_factories.TryGetValue(msgId, out var factory))
                    {
                        NetworkMessage msg = factory();
                        msg.MessageId = msgId;
                        msg.Deserialize(br);
                        msg.Handle(sender);
                    }
                    else
                    {
                        string typeName = _typeMap.TryGetValue(msgId, out var t) ? t.Name : "Unknown";
                        Debug.LogWarning($"[MessageDispatcher] Unhandled message: {msgId:X8} ({typeName})");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ============ Debug ============

        /// <summary>
        /// 获取所有已注册的消息类型信息（调试用）。
        /// </summary>
        public static IReadOnlyDictionary<int, Type> GetRegisteredTypes() => _typeMap;
    }
}
