using System;
using Steamworks;
using UnityEngine;

namespace MultiplayerDeck
{
	public class SteamIntegration
	{
		public static int channel;

		public static RemotePlayer getPlayer(CSteamID steamID)
		{
			foreach (RemotePlayer player in TogetherManager.players)
			{
				if (player.isUser(steamID))
				{
					return player;
				}
			}
			return null;
		}

		public RemotePlayer makeCurrentUser()
		{
			return new RemotePlayer(SteamUser.GetSteamID());
		}

		public void createLobby()
		{
			SteamMatchmaking.CreateLobby((ELobbyType)2, 4);
		}

		public void setLobbyPrivate(bool priv)
		{
			if (priv)
			{
				SteamMatchmaking.SetLobbyType(TogetherManager.currentLobby.getID(), (ELobbyType)1);
			}
			else
			{
				SteamMatchmaking.SetLobbyType(TogetherManager.currentLobby.getID(), (ELobbyType)2);
			}
		}

		public void getLobbies()
		{
			SteamMatchmaking.AddRequestLobbyListDistanceFilter((ELobbyDistanceFilter)3);
			SteamMatchmaking.RequestLobbyList();
		}

		public void getPacket(Packet packet)
		{
			uint num = default(uint);
			SteamNetworking.IsP2PPacketAvailable(out num, channel);
			if (num != 0)
			{
				byte[] array = new byte[num];
				CSteamID steamID = default(CSteamID);
				Debug.Log("We have a packet of size " + num);
				try
				{
					uint num2 = default(uint);
					SteamNetworking.ReadP2PPacket(array, num, out num2, out steamID, 0);
				}
				catch (Exception ex)
				{
					Debug.Log("Reading the packet failed: " + ex.Message);
					Debug.LogError("Reading the packet failed: " + ex.Message);
					packet.clear();
					return;
				}
				packet.set(getPlayer(steamID), array);
			}
			else
			{
				packet.clear();
			}
		}

		public void sendPacket(byte[] data)
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

		public void messageUser(RemotePlayer player)
		{
			SteamFriends.ActivateGameOverlayToUser("Chat", player.steamUser);
		}
	}
}
