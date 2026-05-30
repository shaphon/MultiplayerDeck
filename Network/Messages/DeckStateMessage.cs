using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>牌组状态同步消息。</summary>
    public class DeckStateMessage : NetworkMessage
    {
        public int Version;
        public bool UsedDeck;
        public List<Skill> Skills;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Version);
            bw.Write(UsedDeck);
            SkillSerializer.SkillListSerialize(bw, Skills ?? new List<Skill>());
        }

        public override void Deserialize(BinaryReader br)
        {
            Version = br.ReadInt32();
            UsedDeck = br.ReadBoolean();
            Skills = SkillSerializer.SkillListDeserialize(br);
        }

        public override void Handle(RemotePlayer sender)
        {
            if (!BattleSyncManager.Instance.battleStartDeckManager.deckReceived)
            {
                Debug.Log("[DeckSync] Ignored DeckState before battle start deck sync completed.");
                return;
            }
            BattleSyncManager.Instance.ApplyDeckState(UsedDeck, Skills, Version, sender);
        }
    }
}
