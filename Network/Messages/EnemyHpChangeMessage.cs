using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>敌人血量变化消息。</summary>
    public class EnemyHpChangeMessage : NetworkMessage
    {
        public string EnemyKey;
        public int Position;
        public int Hp;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(EnemyKey);
            bw.Write(Position);
            bw.Write(Hp);
        }

        public override void Deserialize(BinaryReader br)
        {
            EnemyKey = br.ReadString();
            Position = br.ReadInt32();
            Hp = br.ReadInt32();
        }

        public override void Handle(RemotePlayer sender)
        {
            BattleSyncManager.Instance.ApplyEnemyHp(EnemyKey, Position, Hp);
        }
    }
}
