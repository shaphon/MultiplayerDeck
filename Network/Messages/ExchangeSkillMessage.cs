using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>交换技能消息。</summary>
    public class ExchangeSkillMessage : NetworkMessage
    {
        public ulong TargetAccountId;
        public Skill Skill;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(TargetAccountId);
            SkillSerializer.SkillSerialize(bw, Skill);
        }

        public override void Deserialize(BinaryReader br)
        {
            TargetAccountId = br.ReadUInt64();
            Skill = SkillSerializer.SkillDeserialize(br);
        }

        public override void Handle(RemotePlayer sender)
        {
            if (Skill == null) return;
            if (sender != TogetherManager.currentUser
                && TargetAccountId == TogetherManager.currentUser.steamUser.m_SteamID)
            {
                BattleSyncManager.Instance.ReceiveExchangedSkill(Skill);
            }
        }
    }
}
