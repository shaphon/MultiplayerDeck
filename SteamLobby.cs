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
			TogetherManager.currentUser = service.MakeCurrentUser();
			steamID = id;
			try
			{
				ownerID = SteamMatchmaking.GetLobbyOwner(steamID);
				memberNames = SteamMatchmaking.GetLobbyData(steamID, "members").Split('\t').ToList();
			}
			catch
			{
			}
			FetchAllMetadata();
		}

		public void FetchAllMetadata()
		{
			name = GetMetadata("name");
			owner = GetOwnerName();
			capacity = GetCapacity();
			members = GetMemberCount();
		}

		public string GetOwnerName()
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

		public bool IsOwner()
		{
			return TogetherManager.GetCurrentUser().IsUser(ownerID);
		}

		public void NewOwner()
		{
			foreach (RemotePlayer player in TogetherManager.players)
			{
				if (!TogetherManager.currentUser.IsUser(player.steamUser))
				{
					SteamMatchmaking.SetLobbyData(steamID, "owner", player.userName);
					SteamMatchmaking.SetLobbyOwner(steamID, player.steamUser);
					ownerID = player.steamUser;
					break;
				}
			}
		}

		public int GetMemberCount()
		{
			try
			{
				return SteamMatchmaking.GetNumLobbyMembers(steamID);
			}
			catch
			{
				return memberNames.Count();
			}
		}

		public List<RemotePlayer> GetLobbyMembers()
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

		public string GetMemberNameList()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (RemotePlayer player in players)
			{
				stringBuilder.Append(player.userName);
				stringBuilder.Append("\t");
			}
			return stringBuilder.ToString().Trim();
		}

		public CSteamID GetID()
		{
			return steamID;
		}

		public void LeaveLobby()
		{
			SteamMatchmaking.LeaveLobby(steamID);
		}

		public void SetJoinable(bool toggle)
		{
			SteamMatchmaking.SetLobbyJoinable(steamID, toggle);
		}

		public void SetPrivate(bool toggle)
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

		public void Join()
		{
			SteamMatchmaking.JoinLobby(steamID);
		}

		public int GetCapacity()
		{
			try
			{
				return SteamMatchmaking.GetLobbyMemberLimit(steamID);
			}
			catch
			{
				return capacity;
			}
		}

		public string GetMetadata(string key)
		{
			return SteamMatchmaking.GetLobbyData(steamID, key);
		}

		public void SetMetadata(Dictionary<string, string> pairs)
		{
			foreach (KeyValuePair<string, string> pair in pairs)
			{
				SteamMatchmaking.SetLobbyData(steamID, pair.Key, pair.Value);
			}
		}

		public AccountID_t GetOwner()
		{
			return ownerID.GetAccountID();
		}
	}
}
