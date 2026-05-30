using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>抽牌请求消息。</summary>
    public class RequestDrawMessage : NetworkMessage
    {
        public int Count;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Count);
        }

        public override void Deserialize(BinaryReader br)
        {
            Count = br.ReadInt32();
        }

        public override void Handle(RemotePlayer sender)
        {
            BattleSyncManager.Instance.ReceiveDrawRequest(sender, Count);
        }
    }
}
