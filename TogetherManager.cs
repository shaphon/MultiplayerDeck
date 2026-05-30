using System.Collections.Generic;

namespace MultiplayerDeck
{
	public class TogetherManager
	{
		public static SteamLobby currentLobby;

		public static LanLobby lanLobby;

		public static RemotePlayer currentUser;

		public static List<RemotePlayer> players = new List<RemotePlayer>();

		public static ILobby ActiveLobby
		{
			get { return (ILobby)currentLobby ?? lanLobby; }
		}

		public static bool IsLanMode
		{
			get { return lanLobby != null; }
		}

		public static RemotePlayer GetCurrentUser()
		{
			foreach (RemotePlayer player in players)
			{
				if (player.IsUser(currentUser.steamUser))
				{
					return player;
				}
			}
			return currentUser;
		}

		public static void ClearMultiplayerData()
		{
			currentLobby = null;
			lanLobby = null;
			players.Clear();
		}
	}
}
