using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 地图同步消息。payload 用 XML 序列化的 StageMapSerializer.NetStageMapPacket。
    /// </summary>
    public class StageMapMessage : NetworkMessage
    {
        public byte[] Payload;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(Payload.Length);
            bw.Write(Payload);
        }

        public override void Deserialize(BinaryReader br)
        {
            int len = br.ReadInt32();
            Payload = br.ReadBytes(len);
        }

        public override void Handle(RemotePlayer sender)
        {
            UnityEngine.Debug.Log("[MultiplayerDeck] Received StageMap, IsLobbyOwner=" + MultiplayerDeck_Plugin.IsLobbyOwner);
            if (!MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                StageMapSerializer.mapPacket = StageMapSerializer.DeserializeMapPacketFromPayload(Payload);
                StageSyncManager.Instance.GotoNextStage();
            }
        }
    }
}
