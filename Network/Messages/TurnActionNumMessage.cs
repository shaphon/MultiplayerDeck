using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>回合行动数同步消息。</summary>
    public class TurnActionNumMessage : NetworkMessage
    {
        public int Value;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Value);
        }

        public override void Deserialize(BinaryReader br)
        {
            Value = br.ReadInt32();
        }

        public override void Handle(RemotePlayer sender)
        {
            BattleSyncManager.Instance.ApplyTurnActionNum(Value);
        }
    }
}
