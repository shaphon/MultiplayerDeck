using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>牌组变异报告消息。</summary>
    public class DeckMutationReportMessage : NetworkMessage
    {
        public bool UsedDeck;
        public List<Skill> Skills;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(UsedDeck);
            SkillSerializer.SkillListSerialize(bw, Skills ?? new List<Skill>());
        }

        public override void Deserialize(BinaryReader br)
        {
            UsedDeck = br.ReadBoolean();
            Skills = SkillSerializer.SkillListDeserialize(br);
        }

        public override void Handle(RemotePlayer sender)
        {
            BattleSyncManager.Instance.ReceiveDeckMutationReport(sender, UsedDeck, Skills);
        }
    }
}
