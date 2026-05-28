using Steamworks;
using UnityEngine;

namespace MultiplayerDeck
{
    public class SteamCallbacks
    {
        protected static Callback<LobbyInvite_t> LobbyInvite;

        protected static Callback<LobbyEnter_t> LobbyEnter;

        protected static Callback<LobbyDataUpdate_t> LobbyDataUpdate;

        protected static Callback<LobbyChatUpdate_t> LobbyChatUpdate;

        protected static Callback<LobbyChatMsg_t> LobbyChatMsg;

        protected static Callback<LobbyMatchList_t> LobbyMatchList;

        protected static Callback<LobbyCreated_t> LobbyCreated;

        protected static Callback<GameLobbyJoinRequested_t> GameLobbyJoinRequested;

        protected static Callback<AvatarImageLoaded_t> AvatarImageLoaded;

        protected static Callback<PersonaStateChange_t> PersonaStateChange;

        protected static Callback<P2PSessionConnectFail_t> P2PSessionConnectFail;

        protected static Callback<P2PSessionRequest_t> P2PSessionRequest;

        public static void callbackInit()
        {
            if (SteamManager.Initialized)
            {
                SteamCallbacks.LobbyInvite = Callback<LobbyInvite_t>.Create(new Callback<LobbyInvite_t>.DispatchDelegate(SteamCallbacks.onLobbyInvite));
                SteamCallbacks.LobbyEnter = Callback<LobbyEnter_t>.Create(new Callback<LobbyEnter_t>.DispatchDelegate(SteamCallbacks.onLobbyEnter));
                SteamCallbacks.LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(new Callback<LobbyDataUpdate_t>.DispatchDelegate(SteamCallbacks.onLobbyDataUpdate));
                SteamCallbacks.LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(new Callback<LobbyChatUpdate_t>.DispatchDelegate(SteamCallbacks.onLobbyChatUpdate));
                SteamCallbacks.LobbyChatMsg = Callback<LobbyChatMsg_t>.Create(new Callback<LobbyChatMsg_t>.DispatchDelegate(SteamCallbacks.onLobbyChatMessage));
                SteamCallbacks.LobbyMatchList = Callback<LobbyMatchList_t>.Create(new Callback<LobbyMatchList_t>.DispatchDelegate(SteamCallbacks.onLobbyMatchList));
                SteamCallbacks.LobbyCreated = Callback<LobbyCreated_t>.Create(new Callback<LobbyCreated_t>.DispatchDelegate(SteamCallbacks.onLobbyCreated));
                SteamCallbacks.GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(new Callback<GameLobbyJoinRequested_t>.DispatchDelegate(SteamCallbacks.onGameLobbyJoinRequested));
                SteamCallbacks.AvatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(new Callback<AvatarImageLoaded_t>.DispatchDelegate(SteamCallbacks.onAvatarImageLoaded));
                SteamCallbacks.PersonaStateChange = Callback<PersonaStateChange_t>.Create(new Callback<PersonaStateChange_t>.DispatchDelegate(SteamCallbacks.onPersonaStateChange));
                SteamCallbacks.P2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.Create(new Callback<P2PSessionConnectFail_t>.DispatchDelegate(SteamCallbacks.onP2PSessionConnectFail));
                SteamCallbacks.P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(new Callback<P2PSessionRequest_t>.DispatchDelegate(SteamCallbacks.onP2PSessionRequest));
            }
        }

        private static void onLobbyInvite(ulong user, ulong lobby, ulong gameID)
        {
            Debug.Log("Got Invited! :) -  ID: " + lobby);
        }

        public static void onLobbyInvite(LobbyInvite_t callback)
        {
            onLobbyInvite(callback.m_ulSteamIDUser, callback.m_ulSteamIDLobby, callback.m_ulGameID);
        }

        private static void onLobbyEnter(ulong lobby, uint unused, bool blocked, uint successEnum)
        {
            Debug.Log("Entered Lobby: " + successEnum + " - " + lobby);
            if (!blocked && successEnum == 1)
            {
                TogetherManager.currentLobby = new SteamLobby(NetworkHelper.steam, new CSteamID(lobby));
                if (TogetherManager.currentLobby == null)
                {
                    Debug.Log("MakeCurrentLobby: Fail");
                }
                else
                {
                    Debug.Log("MakeCurrentLobby: Success");
                }
                TogetherManager.players = TogetherManager.currentLobby.GetLobbyMembers();
                NetworkHelper.AddPlayer(TogetherManager.currentUser);
                VoteManager.Instance.SyncPlayersWithLobby();
                NetworkHelper.SendData(NetDataType.Version);
            }
            else
            {
                Debug.Log(">>>");
            }
            NetworkHelper.SendData(NetDataType.Version);
        }

        public static void onLobbyEnter(LobbyEnter_t callback)
        {
            onLobbyEnter(callback.m_ulSteamIDLobby, callback.m_rgfChatPermissions, callback.m_bLocked, callback.m_EChatRoomEnterResponse);
        }

        private static void onLobbyDataUpdate(ulong lobby, ulong playerUpdated, byte success)
        {
            if (success != 0)
            {
                Debug.Log("Lobby Data Updated for some damn reason");
            }
        }

        public static void onLobbyDataUpdate(LobbyDataUpdate_t callback)
        {
            onLobbyDataUpdate(callback.m_ulSteamIDLobby, callback.m_ulSteamIDMember, callback.m_bSuccess);
        }

        public static void onLobbyChatUpdate(CSteamID lobby, CSteamID targetPlayer, CSteamID causePlayer, EChatMemberStateChange even)
        {
            if ((int)even == 1)
            {
                NetworkHelper.AddPlayer(new RemotePlayer(targetPlayer));
                NetworkHelper.SendData(NetDataType.Version);
                NetworkHelper.SendData(NetDataType.Ready);
            }
            RemotePlayer player = SteamIntegration.GetPlayer(targetPlayer);
            if ((int)even == 2)
            {
                NetworkHelper.RemovePlayer(player);
            }
            if ((int)even == 4)
            {
                NetworkHelper.RemovePlayer(player);
            }
            if ((int)even == 8)
            {
                NetworkHelper.RemovePlayer(player);
            }
            if ((int)even == 16)
            {
                NetworkHelper.RemovePlayer(player);
            }
            if (TogetherManager.currentLobby == null)
            {
                return;
            }
            TogetherManager.currentLobby.GetOwnerName();
            TogetherManager.players = TogetherManager.currentLobby.GetLobbyMembers();
            VoteManager.Instance.SyncPlayersWithLobby();
            if (TogetherManager.currentLobby.IsOwner())
            {
                SteamMatchmaking.SetLobbyData(lobby, "members", TogetherManager.currentLobby.GetMemberNameList());
            }
        }

        public static void onLobbyChatUpdate(LobbyChatUpdate_t callback)
        {
            onLobbyChatUpdate(new CSteamID(callback.m_ulSteamIDLobby), new CSteamID(callback.m_ulSteamIDUserChanged), new CSteamID(callback.m_ulSteamIDMakingChange), (EChatMemberStateChange)callback.m_rgfChatMemberStateChange);
        }

        public static void onLobbyChatMessage(CSteamID lobby, CSteamID chatter, byte chatType, uint chatIndice)
        {
            Debug.Log("Lobby Chat message");
        }

        public static void onLobbyChatMessage(LobbyChatMsg_t callback)
        {
            onLobbyChatMessage(new CSteamID(callback.m_ulSteamIDLobby), new CSteamID(callback.m_ulSteamIDUser), callback.m_eChatEntryType, callback.m_iChatID);
        }

        public static void onLobbyMatchList(int lobbiesMatching)
        {
            Debug.Log("Lobby Match List: " + lobbiesMatching);
            for (int i = 0; i < lobbiesMatching; i++)
            {
                NetworkHelper.lobbies.Add(new SteamLobby(NetworkHelper.steam, SteamMatchmaking.GetLobbyByIndex(i)));
            }
        }

        public static void onLobbyMatchList(LobbyMatchList_t callback)
        {
            onLobbyMatchList((int)callback.m_nLobbiesMatching);
        }

        public static void onLobbyCreated(EResult result, ulong lobby)
        {
            Debug.Log(string.Concat(new string[]
            {
                "Lobby Created: ",
                result.ToString(),
                " - Steam - ",
                lobby.ToString(),
                " - ID: ",
                lobby.ToString()
            }));
            TogetherManager.currentLobby = new SteamLobby(NetworkHelper.steam, new CSteamID(lobby));
            TogetherManager.players = TogetherManager.currentLobby.GetLobbyMembers();
            NetworkHelper.AddPlayer(new RemotePlayer(SteamMatchmaking.GetLobbyOwner(new CSteamID(lobby))));
            NetworkHelper.UpdateLobbyData();
            NetworkHelper.SendData(NetDataType.Version);
        }

        public static void onLobbyCreated(LobbyCreated_t callback)
        {
            onLobbyCreated(callback.m_eResult, callback.m_ulSteamIDLobby);
        }

        public static void onGameLobbyJoinRequested(CSteamID steamIDLobby, CSteamID steamIDFriend)
        {
            Debug.Log("Entered via invite/join - " + steamIDLobby.ToString() + " - ID: " + steamIDLobby.ToString());
            TogetherManager.ClearMultiplayerData();
            SteamMatchmaking.JoinLobby(steamIDLobby);
            TogetherManager.currentLobby = new SteamLobby(NetworkHelper.steam, steamIDLobby);
            TogetherManager.players = TogetherManager.currentLobby.GetLobbyMembers();
            NetworkHelper.AddPlayer(TogetherManager.currentUser);
            VoteManager.Instance.SyncPlayersWithLobby();
        }

        public static void onGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            onGameLobbyJoinRequested(callback.m_steamIDLobby, callback.m_steamIDFriend);
        }

        public static void onAvatarImageLoaded(CSteamID steamID, int image, int width, int height)
        {
            Debug.Log("Steam Avatar is downloaded! " + steamID.ToString() + " - size: " + width);
            SteamIntegration.GetPlayer(steamID)?.UpdateAvatar(image);
        }

        public static void onAvatarImageLoaded(AvatarImageLoaded_t callback)
        {
            onAvatarImageLoaded(callback.m_steamID, callback.m_iImage, callback.m_iWide, callback.m_iTall);
        }

        public static void onPersonaStateChange(ulong steamID, EPersonaChange change)
        {
            if ((int)change == 64)
            {
                Debug.Log("Steam Avatar is available: " + steamID);
                SteamIntegration.GetPlayer(new CSteamID(steamID))?.GetAvatar();
            }
        }

        public static void onPersonaStateChange(PersonaStateChange_t callback)
        {
            onPersonaStateChange(callback.m_ulSteamID, callback.m_nChangeFlags);
        }

        public static void onP2PSessionConnectFail(CSteamID paramSteamID, byte paramP2PSessionError)
        {
            Debug.Log("onP2PSessionConnectFail");
        }

        public static void onP2PSessionConnectFail(P2PSessionConnectFail_t callback)
        {
            onP2PSessionConnectFail(callback.m_steamIDRemote, callback.m_eP2PSessionError);
        }

        public static void onP2PSessionRequest(CSteamID paramSteamID)
        {
            Debug.Log("onP2PSessionRequest");
            SteamNetworking.AcceptP2PSessionWithUser(paramSteamID);
        }

        public static void onP2PSessionRequest(P2PSessionRequest_t callback)
        {
            onP2PSessionRequest(callback.m_steamIDRemote);
        }
    }
}
