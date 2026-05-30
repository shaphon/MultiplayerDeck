using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 消息处理委托。reader 已越过 NetDataType（即 reader 定位在消息体的起始位置）。
    /// </summary>
    public delegate void MessageHandler(BinaryReader reader, byte[] rawData, RemotePlayer sender);

    /// <summary>
    /// 消息处理器接口。每个实现提供一组 (NetDataType → 处理函数) 的映射。
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>返回本处理器负责的所有消息类型及其处理函数的映射</summary>
        IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }
    }
}
