using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 网络消息抽象基类。
    /// 继承此类即可自动注册：无需修改枚举、无需手动注册、无需中心化配置。
    ///
    /// MessageId 自动由 type.FullName 的 FNV-1a hash 生成，稳定且全局唯一。
    /// </summary>
    public abstract class NetworkMessage
    {
        /// <summary>
        /// 网络传输用的消息 ID。由 MessageDispatcher 在注册时自动设置（= Hash(type.FullName)）。
        /// </summary>
        internal int MessageId { get; set; }

        /// <summary>
        /// 将消息负载序列化到 BinaryWriter。
        /// 不要写入 MessageId（框架自动处理）。
        /// </summary>
        public abstract void Serialize(BinaryWriter writer);

        /// <summary>
        /// 从 BinaryReader 反序列化消息负载。
        /// 不要读取 MessageId（框架已读取）。
        /// </summary>
        public abstract void Deserialize(BinaryReader reader);

        /// <summary>
        /// 接收到消息后的处理逻辑。默认空实现（fire-and-forget 消息不需要处理）。
        /// </summary>
        public virtual void Handle(RemotePlayer sender) { }
    }
}
