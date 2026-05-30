using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>战斗开始消息。</summary>
    public class BattleStartMessage : NetworkMessage
    {
        public string QueueData;
        public bool NormalBattle;
        public bool Cursed;
        public string RewardKey;
        public string Preset;
        public bool NoGameover;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(QueueData ?? string.Empty);
            bw.Write(NormalBattle);
            bw.Write(Cursed);
            bw.Write(RewardKey);
            bw.Write(Preset);
            bw.Write(NoGameover);
        }

        public override void Deserialize(BinaryReader br)
        {
            QueueData = br.ReadString();
            NormalBattle = br.ReadBoolean();
            Cursed = br.ReadBoolean();
            RewardKey = br.ReadString();
            Preset = br.ReadString();
            NoGameover = br.ReadBoolean();
        }

        public override void Handle(RemotePlayer sender)
        {
            Debug.Log("[MultiplayerDeck] BattleStart " + QueueData);
            StageSyncManager.Instance.StartBattleFromNetwork(QueueData, NormalBattle, Cursed, RewardKey, Preset, NoGameover);
        }
    }
}
