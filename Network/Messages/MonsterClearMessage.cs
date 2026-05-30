using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>怪物清除消息。</summary>
    public class MonsterClearMessage : NetworkMessage
    {
        public float X;
        public float Y;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
        }

        public override void Deserialize(BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
        }

        public override void Handle(RemotePlayer sender)
        {
            StageSyncManager.Instance.MonsterClear(new Vector2(X, Y));
        }
    }
}
