using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;
using UnityEngine;

namespace MultiplayerDeck
{
	public class SteamLobby
	{
		public CSteamID steamID;

		public CSteamID ownerID;

		public SteamIntegration service;

		public List<RemotePlayer> players = new List<RemotePlayer>();

		public List<string> memberNames = new List<string>();

		public string name = "";

		public string owner = "MegaCrit";

		public int capacity = 6;

		public int members = 0;

		public SteamLobby(SteamIntegration service, CSteamID id)
		{
			this.service = service;
			TogetherManager.currentUser = service.makeCurrentUser();
			steamID = id;
			try
			{
				ownerID = SteamMatchmaking.GetLobbyOwner(steamID);
				memberNames = SteamMatchmaking.GetLobbyData(steamID, "members").Split('\t').ToList();
			}
			catch
			{
			}
			fetchAllMetadata();
		}

		public void fetchAllMetadata()
		{
			name = getMetadata("name");
			owner = getOwnerName();
			capacity = getCapacity();
			members = getMemberCount();
		}

		public string getOwnerName()
		{
			try
			{
				owner = SteamMatchmaking.GetLobbyData(steamID, "owner");
				ownerID = SteamMatchmaking.GetLobbyOwner(steamID);
			}
			catch
			{
			}
			return owner;
		}

		public bool isOwner()
		{
			return TogetherManager.getCurrentUser().isUser(ownerID);
		}

		public void newOwner()
		{
			foreach (RemotePlayer player in TogetherManager.players)
			{
				if (!TogetherManager.currentUser.isUser(player.steamUser))
				{
					SteamMatchmaking.SetLobbyData(steamID, "owner", player.userName);
					SteamMatchmaking.SetLobbyOwner(steamID, player.steamUser);
					ownerID = player.steamUser;
					break;
				}
			}
		}

		public int getMemberCount()
		{
			return memberNames.Count();
		}

		public List<RemotePlayer> getLobbyMembers()
		{
			int num = 1;
			try
			{
				num = SteamMatchmaking.GetNumLobbyMembers(steamID);
				Debug.Log("get Members in  lobby: " + num);
			}
			catch
			{
			}
			players.Clear();
			try
			{
				for (int i = 0; i < num; i++)
				{
					RemotePlayer remotePlayer = new RemotePlayer(SteamMatchmaking.GetLobbyMemberByIndex(steamID, i));
					players.Add(remotePlayer);
					Debug.Log("get Members created: " + remotePlayer.userName);
				}
			}
			catch
			{
			}
			return players;
		}

		public string getMemberNameList()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (RemotePlayer player in players)
			{
				stringBuilder.Append(player.userName);
				stringBuilder.Append("\t");
			}
			return stringBuilder.ToString().Trim();
		}

		public CSteamID getID()
		{
			return steamID;
		}

		public void leaveLobby()
		{
			SteamMatchmaking.LeaveLobby(steamID);
		}

		public void setJoinable(bool toggle)
		{
			SteamMatchmaking.SetLobbyJoinable(steamID, toggle);
		}

		public void setPrivate(bool toggle)
		{
			if (toggle)
			{
				SteamMatchmaking.SetLobbyType(steamID, (ELobbyType)1);
			}
			else
			{
				SteamMatchmaking.SetLobbyType(steamID, (ELobbyType)2);
			}
		}

		public void join()
		{
			SteamMatchmaking.JoinLobby(steamID);
		}

		public int getCapacity()
		{
			return 10;
		}

		public string getMetadata(string key)
		{
			return SteamMatchmaking.GetLobbyData(steamID, key);
		}

		public void setMetadata(Dictionary<string, string> pairs)
		{
			foreach (KeyValuePair<string, string> pair in pairs)
			{
				SteamMatchmaking.SetLobbyData(steamID, pair.Key, pair.Value);
			}
		}

		public AccountID_t getOwner()
		{
			return ownerID.GetAccountID();
		}
	}
}