using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>玩家位置同步消息。</summary>
    public class PlayerPositionMessage : NetworkMessage
    {
        public float X;
        public float Y;
        public float JumpY;
        public float Timestamp;
        public bool IsMoving;
        public bool FacingRight;
        public string SkinName;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
            bw.Write(JumpY);
            bw.Write(Timestamp);
            bw.Write(IsMoving);
            bw.Write(FacingRight);
            bw.Write(SkinName ?? string.Empty);
        }

        public override void Deserialize(BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
            JumpY = br.ReadSingle();
            Timestamp = br.ReadSingle();
            IsMoving = br.ReadBoolean();
            FacingRight = br.ReadBoolean();
            SkinName = br.ReadString();
        }

        public override void Handle(RemotePlayer sender)
        {
            if (sender != null)
            {
                MultiLucySkelController.OnReceiveRemoteState(
                    sender.steamUser.m_SteamID,
                    new Vector2(X, Y), JumpY, Timestamp, IsMoving, FacingRight, SkinName);
            }
        }
    }
}
