using System.Collections.Generic;
using Steamworks;

namespace MultiplayerDeck
{
    public interface ILobbyService
    {
        void GetPacket(Packet packet);
        void SendPacket(byte[] data);
    }

    public interface ILobby
    {
        CSteamID steamID { get; }
        CSteamID ownerID { get; }
        string name { get; }
        string owner { get; }
        int capacity { get; }

        bool IsOwner();
        CSteamID GetID();
        List<RemotePlayer> GetLobbyMembers();
        int GetMemberCount();
        int GetCapacity();
        string GetOwnerName();
        string GetMemberNameList();
        void LeaveLobby();
        void SetJoinable(bool toggle);
        void SetPrivate(bool toggle);
        void Join();
        void FetchAllMetadata();
        void NewOwner();
        void SetMetadata(Dictionary<string, string> pairs);
        string GetMetadata(string key);
        AccountID_t GetOwner();
    }
}
