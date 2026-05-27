using System.Collections.Generic;

namespace MultiplayerDeck
{
	public class TogetherManager
	{
		public static SteamLobby currentLobby;

		public static RemotePlayer currentUser;

		public static List<RemotePlayer> players = new List<RemotePlayer>();

		public static RemotePlayer getCurrentUser()
		{
			foreach (RemotePlayer player in players)
			{
				if (player.isUser(currentUser.steamUser))
				{
					return player;
				}
			}
			return currentUser;
		}

		public static void clearMultiplayerData()
		{
			currentLobby = null;
			players.Clear();
		}
	}
}
