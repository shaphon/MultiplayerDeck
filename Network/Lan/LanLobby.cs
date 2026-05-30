using System.Collections.Generic;
using System.Text;
using Steamworks;

namespace MultiplayerDeck.Network
{
    public class LanLobbyInfo
    {
        public string Name;
        public string IpAddress;
        public int PlayerCount;
        public int Port;
    }

    public class LanLobby : ILobby
    {
        public CSteamID steamID { get; set; }
        public CSteamID ownerID { get; set; }
        public string name { get; set; } = "";
        public string owner { get; set; } = "";
        public int capacity { get; set; } = 4;

        private LanManager _manager;

        public LanLobby(LanManager manager)
        {
            _manager = manager;
            steamID = new CSteamID(0x4C414E4C4F424259u); // "LANLOBBY" marker
            ownerID = new CSteamID(1u); // host is always ID 1
            owner = manager.LocalPlayerName;
            name = manager.LocalPlayerName + "'s LAN Game";
        }

        public bool IsOwner()
        {
            return _manager.IsHost;
        }

        public CSteamID GetID()
        {
            return steamID;
        }

        public List<RemotePlayer> GetLobbyMembers()
        {
            return _manager.GetPlayers();
        }

        public int GetMemberCount()
        {
            return _manager.PlayerCount;
        }

        public int GetCapacity()
        {
            return capacity;
        }

        public string GetOwnerName()
        {
            return owner;
        }

        public string GetMemberNameList()
        {
            StringBuilder sb = new StringBuilder();
            foreach (RemotePlayer p in _manager.GetPlayers())
            {
                sb.Append(p.userName);
                sb.Append("\t");
            }
            return sb.ToString().Trim();
        }

        public void LeaveLobby()
        {
            _manager.Disconnect();
        }

        public void SetJoinable(bool toggle) { }

        public void SetPrivate(bool toggle) { }

        public void Join() { }

        public void FetchAllMetadata() { }

        public void NewOwner()
        {
            // In LAN mode, host is always owner. If host leaves, lobby disbands.
        }

        public void SetMetadata(Dictionary<string, string> pairs) { }

        public string GetMetadata(string key)
        {
            return "";
        }

        public AccountID_t GetOwner()
        {
            return ownerID.GetAccountID();
        }
    }
}
