using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>大厅关闭消息。</summary>
    public class LobbyClosedMessage : NetworkMessage
    {
        public ulong LobbyId;
        public string Reason;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(LobbyId);
            bw.Write(Reason ?? string.Empty);
        }

        public override void Deserialize(BinaryReader br)
        {
            LobbyId = br.ReadUInt64();
            Reason = br.ReadString();
        }

        public override void Handle(RemotePlayer sender)
        {
            if (TogetherManager.ActiveLobby != null
                && TogetherManager.ActiveLobby.steamID.m_SteamID == LobbyId)
            {
                Debug.Log("[MultiplayerDeck] Lobby closed by owner: " + Reason);
                MultiLucySkelController.CleanupAllRemotePlayers();
                TogetherManager.ClearMultiplayerData();
                VoteManager.Instance.AbortAllVotes();
            }
        }
    }
}
