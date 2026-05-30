using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>测试消息，用于快速启动战斗。</summary>
    public class TestMessage : NetworkMessage
    {
        public string QueueKey = "Queue_S4_King";

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(QueueKey ?? "");
        }

        public override void Deserialize(BinaryReader br)
        {
            QueueKey = br.ReadString();
        }

        public override void Handle(RemotePlayer sender)
        {
            Debug.Log("Test " + QueueKey);
            if (FieldSystem.instance != null)
            {
                FieldSystem.instance.BattleStart(
                    new GameDataEditor.GDEEnemyQueueData(QueueKey),
                    StageSystem.instance.StageData.BattleMap.Key,
                    true, false, "", "", false);
            }
        }
    }
}
