using System.Collections.Generic;
using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>网络技能效果消息。</summary>
    public class NetworkSkillEffectMessage : NetworkMessage
    {
        public string SkillKey;
        public List<TargetInfo> Targets = new List<TargetInfo>();
        public List<int> CustomNumbers = new List<int>();

        public struct TargetInfo
        {
            public bool IsEnemy;
            public string Key;
            public int Position;

            public TargetInfo(bool isEnemy, string key, int position)
            {
                IsEnemy = isEnemy;
                Key = key;
                Position = position;
            }
        }

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(SkillKey ?? string.Empty);
            bw.Write(Targets?.Count ?? 0);
            if (Targets != null)
            {
                foreach (var t in Targets)
                {
                    bw.Write(t.IsEnemy);
                    bw.Write(t.Key);
                    bw.Write(t.Position);
                }
            }
            bw.Write(CustomNumbers?.Count ?? 0);
            if (CustomNumbers != null)
            {
                foreach (int n in CustomNumbers)
                    bw.Write(n);
            }
        }

        public override void Deserialize(BinaryReader br)
        {
            SkillKey = br.ReadString();
            int targetCount = br.ReadInt32();
            Targets = new List<TargetInfo>(targetCount);
            for (int i = 0; i < targetCount; i++)
            {
                Targets.Add(new TargetInfo(
                    br.ReadBoolean(),
                    br.ReadString(),
                    br.ReadInt32()
                ));
            }
            int numCount = br.ReadInt32();
            CustomNumbers = new List<int>(numCount);
            for (int i = 0; i < numCount; i++)
                CustomNumbers.Add(br.ReadInt32());
        }

        public override void Handle(RemotePlayer sender)
        {
            // Convert TargetInfo list to (bool, string, int) tuples for ApplySkillEffect overload
            var targetInfos = new List<(bool, string, int)>();
            foreach (var t in Targets)
                targetInfos.Add((t.IsEnemy, t.Key, t.Position));

            SkillExtended_Network.ApplySkillEffect(SkillKey, targetInfos, CustomNumbers);
        }
    }
}
