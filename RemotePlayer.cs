using System;
using Steamworks;
using UnityEngine;

namespace MultiplayerDeck
{
	public class RemotePlayer
	{
		public CSteamID steamUser;

		public string userName = "";

		public int avatarID = -1;

		public Texture2D pixmap;

		public RemotePlayer(CSteamID steamuser)
		{
			steamUser = steamuser;
			userName = SteamFriends.GetFriendPersonaName(steamUser).Trim();
			getAvatar();
		}

		public void getAvatar()
		{
			bool flag = SteamFriends.RequestUserInformation(steamUser, false);
			int largeFriendAvatar = SteamFriends.GetLargeFriendAvatar(steamUser);
			updateAvatar(largeFriendAvatar);
		}

		public void updateAvatar(int imageID)
		{
			Debug.Log("~~~~~~~~~~~~~~~~~~~~~ Starting Steam Avatar ~~~~~~~~~~~~~~~~~~~~~");
			Debug.Log("ImageID: " + imageID);
			if (imageID != avatarID)
			{
				uint num = default(uint);
				uint num2 = default(uint);
				SteamUtils.GetImageSize(imageID, out num, out num2);
				Debug.Log("W: " + num + ", H: " + num2);
				byte[] array = new byte[(int)(num * num2 * 4)];
				try
				{
					Debug.Log("Image downloaded: " + SteamUtils.GetImageRGBA(imageID, array, (int)(num * num2 * 4)));
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
				pixmap = new Texture2D((int)num, (int)num2, (TextureFormat)4, false);
				pixmap.LoadRawTextureData(array);
				pixmap.Apply();
				CSteamID val = steamUser;
				avatarID = imageID;
				Debug.Log("We have completed creating the Steam image");
			}
		}

		public bool isUser(CSteamID player)
		{
			return steamUser.GetAccountID() == player.GetAccountID();
		}

		public AccountID_t getAccountID()
		{
			return steamUser.GetAccountID();
		}
	}
}
