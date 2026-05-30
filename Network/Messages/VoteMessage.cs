using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>投票消息。</summary>
    public class VoteMessage : NetworkMessage
    {
        public VoteManager.VoteTheme Theme;
        public ulong PlayerId;
        public bool Cancel;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write((int)Theme);
            bw.Write(PlayerId);
            bw.Write(Cancel);
        }

        public override void Deserialize(BinaryReader br)
        {
            Theme = (VoteManager.VoteTheme)br.ReadInt32();
            PlayerId = br.ReadUInt64();
            Cancel = br.ReadBoolean();
        }

        public override void Handle(RemotePlayer sender)
        {
            VoteManager.Instance.VoteFromNetwork(Theme, PlayerId, Cancel);
        }
    }
}
