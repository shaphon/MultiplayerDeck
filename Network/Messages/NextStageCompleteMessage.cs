using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>下一关准备完成消息。无负载。</summary>
    public class NextStageCompleteMessage : NetworkMessage
    {
        public override void Serialize(BinaryWriter writer) { }
        public override void Deserialize(BinaryReader reader) { }

        public override void Handle(RemotePlayer sender)
        {
            if (MultiplayerDeck_Plugin.IsLobbyOwner)
                StageSyncManager.Instance.PlayerNextStageComplete(sender);
            else
                VoteManager.Instance.syncing = false;
        }
    }
}
