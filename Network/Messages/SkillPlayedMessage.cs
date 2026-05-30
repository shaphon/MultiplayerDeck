using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>技能使用消息。</summary>
    public class SkillPlayedMessage : NetworkMessage
    {
        public string SkillName;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(SkillName ?? string.Empty);
        }

        public override void Deserialize(BinaryReader br)
        {
            SkillName = br.ReadString();
        }

        public override void Handle(RemotePlayer sender)
        {
            if (sender != TogetherManager.currentUser)
                BattleSyncManager.Instance.ApplyRemoteSkillName(SkillName);
        }
    }
}
