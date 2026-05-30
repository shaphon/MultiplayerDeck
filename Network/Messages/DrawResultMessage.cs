using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>抽牌结果消息。</summary>
    public class DrawResultMessage : NetworkMessage
    {
        public ulong TargetPlayerId;
        public int Version;
        public List<SkillNetworkDTO> Cards;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(TargetPlayerId);
            bw.Write(Version);
            SkillSerializer.SkillDTOListSerialize(bw, Cards ?? new List<SkillNetworkDTO>());
        }

        public override void Deserialize(BinaryReader br)
        {
            TargetPlayerId = br.ReadUInt64();
            Version = br.ReadInt32();
            Cards = SkillSerializer.SkillDTOListDeserialize(br);
        }

        public override void Handle(RemotePlayer sender)
        {
            BattleSyncManager.Instance.ApplyDrawResult(TargetPlayerId, Version, Cards);
        }
    }
}
