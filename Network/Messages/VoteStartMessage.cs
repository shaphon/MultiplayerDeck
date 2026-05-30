using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>投票开始消息。</summary>
    public class VoteStartMessage : NetworkMessage
    {
        public VoteManager.VoteTheme Theme;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write((int)Theme);
        }

        public override void Deserialize(BinaryReader br)
        {
            Theme = (VoteManager.VoteTheme)br.ReadInt32();
        }

        public override void Handle(RemotePlayer sender)
        {
            VoteManager.Instance.StartVoteFromNetwork(Theme);
        }
    }
}
