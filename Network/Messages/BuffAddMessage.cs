using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>Buff 添加消息。</summary>
    public class BuffAddMessage : NetworkMessage
    {
        public string BuffKey;
        public string TargetCharKey;
        public int TargetPosition;
        public bool TargetIsAlly;
        public string UserCharKey;
        public int UserPosition;
        public bool UserIsAlly;
        public int StackNum;
        public int Lifetime;
        public byte[] CustomData;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(BuffKey ?? "");
            bw.Write(TargetCharKey ?? "");
            bw.Write(TargetPosition);
            bw.Write(TargetIsAlly);
            bw.Write(UserCharKey ?? "");
            bw.Write(UserPosition);
            bw.Write(UserIsAlly);
            bw.Write(StackNum);
            bw.Write(Lifetime);
            if (CustomData != null && CustomData.Length > 0)
            {
                bw.Write(CustomData.Length);
                bw.Write(CustomData);
            }
            else
            {
                bw.Write(0);
            }
        }

        public override void Deserialize(BinaryReader br)
        {
            BuffKey = br.ReadString();
            TargetCharKey = br.ReadString();
            TargetPosition = br.ReadInt32();
            TargetIsAlly = br.ReadBoolean();
            UserCharKey = br.ReadString();
            UserPosition = br.ReadInt32();
            UserIsAlly = br.ReadBoolean();
            StackNum = br.ReadInt32();
            Lifetime = br.ReadInt32();
            int customDataLen = br.ReadInt32();
            CustomData = customDataLen > 0 ? br.ReadBytes(customDataLen) : null;
        }

        public override void Handle(RemotePlayer sender)
        {
            if (sender != TogetherManager.currentUser)
            {
                BuffSyncManager.HandleRemoteBuffAdd(BuffKey, TargetCharKey, TargetPosition, TargetIsAlly,
                    UserCharKey, UserPosition, UserIsAlly, StackNum, Lifetime, CustomData);
            }
        }
    }
}
