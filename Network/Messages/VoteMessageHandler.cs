using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 投票消息：VoteStart, Vote。
    /// </summary>
    public class VoteMessageHandler : IMessageHandler
    {
        public IReadOnlyDictionary<NetDataType, MessageHandler> Handlers { get; }

        public VoteMessageHandler()
        {
            Handlers = new Dictionary<NetDataType, MessageHandler>
            {
                { NetDataType.VoteStart, ReadVoteStart },
                { NetDataType.Vote,      ReadVote },
            };
        }

        private static void ReadVoteStart(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            VoteManager.VoteTheme voteTheme = (VoteManager.VoteTheme)br.ReadInt32();
            VoteManager.Instance.StartVoteFromNetwork(voteTheme);
        }

        private static void ReadVote(BinaryReader br, byte[] raw, RemotePlayer sender)
        {
            VoteManager.VoteTheme voteTheme = (VoteManager.VoteTheme)br.ReadInt32();
            ulong playerId = br.ReadUInt64();
            bool cancel = br.ReadBoolean();
            VoteManager.Instance.VoteFromNetwork(voteTheme, playerId, cancel);
        }
    }
}
