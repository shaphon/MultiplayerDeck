using System;
using Steamworks;
using UnityEngine;

namespace MultiplayerDeck
{
	public class SteamIntegration : ILobbyService
	{
		private static readonly int maxPlayersCount = 4;

		public static int channel;

		public static RemotePlayer GetPlayer(CSteamID steamID)
		{
			foreach (RemotePlayer player in TogetherManager.players)
			{
				if (player.IsUser(steamID))
				{
					return player;
				}
			}
			return null;
		}

		public RemotePlayer MakeCurrentUser()
		{
			return new RemotePlayer(SteamUser.GetSteamID());
		}

		public void CreateLobby()
		{
			SteamMatchmaking.CreateLobby((ELobbyType)2, maxPlayersCount);
		}

		public void SetLobbyPrivate(bool priv)
		{
			if (priv)
			{
				SteamMatchmaking.SetLobbyType(TogetherManager.currentLobby.GetID(), (ELobbyType)1);
			}
			else
			{
				SteamMatchmaking.SetLobbyType(TogetherManager.currentLobby.GetID(), (ELobbyType)2);
			}
		}

		public void GetLobbies()
		{
			SteamMatchmaking.AddRequestLobbyListDistanceFilter((ELobbyDistanceFilter)3);
			SteamMatchmaking.RequestLobbyList();
		}

		public void GetPacket(Packet packet)
		{
			uint num = default(uint);
			SteamNetworking.IsP2PPacketAvailable(out num, channel);
			if (num != 0)
			{
				byte[] array = new byte[num];
				CSteamID steamID = default(CSteamID);
				//Debug.Log("We have a packet of size " + num);
				try
				{
					uint num2 = default(uint);
					SteamNetworking.ReadP2PPacket(array, num, out num2, out steamID, 0);
				}
				catch (Exception ex)
				{
					Debug.Log("Reading the packet failed: " + ex.Message);
					Debug.LogError("Reading the packet failed: " + ex.Message);
					packet.Clear();
					return;
				}
				packet.Set(GetPlayer(steamID), array);
			}
			else
			{
				packet.Clear();
			}
		}

		public void SendPacket(byte[] data)
		{
			foreach (RemotePlayer player in TogetherManager.players)
			{
				try
				{
					bool flag = SteamNetworking.SendP2PPacket(player.steamUser, data, (uint)data.Length, (EP2PSend)2, 0);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			}
		}

		public void MessageUser(RemotePlayer player)
		{
			SteamFriends.ActivateGameOverlayToUser("Chat", player.steamUser);
		}
	}
}
