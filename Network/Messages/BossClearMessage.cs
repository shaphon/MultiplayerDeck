using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>Boss 清除消息。无负载。</summary>
    public class BossClearMessage : NetworkMessage
    {
        public override void Serialize(BinaryWriter writer) { }
        public override void Deserialize(BinaryReader reader) { }

        public override void Handle(RemotePlayer sender)
        {
            StageSyncManager.Instance.bossClear = true;
        }
    }
}
