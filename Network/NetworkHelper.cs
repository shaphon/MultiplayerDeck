using MultiplayerDeck.Network.Messages;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerDeck.Network
{
    /// <summary>
    /// 传输门面 + 大厅管理。消息路由委托给 MessageDispatcher。
    /// </summary>
    public class NetworkHelper
    {
        public static SteamIntegration steam;

        public static LanManager lan;

        public static List<SteamLobby> lobbies = new List<SteamLobby>();

        public static bool embarked = false;

        public static Packet packet = new Packet();

        public static void Initialize()
        {
            steam = new SteamIntegration();
            SteamCallbacks.callbackInit();
        }

        public static void Update()
        {
            if (IsLanActive())
            {
                lan.GetPacket(packet);
                while (packet.HasPacket() && IsLobbyActive())
                {
                    MessageDispatcher.Dispatch(packet.GetData(), packet.GetPlayer());
                    lan.GetPacket(packet);
                }
                return;
            }

            if (Service() == null)
            {
                return;
            }
            Service().GetPacket(packet);
            while (packet.HasPacket() && TogetherManager.currentLobby != null)
            {
                MessageDispatcher.Dispatch(packet.GetData(), packet.GetPlayer());
                if (Service() != null)
                {
                    Service().GetPacket(packet);
                    continue;
                }
                break;
            }
        }

        public static SteamIntegration Service()
        {
            if (IsLanActive())
            {
                return null;
            }
            if (TogetherManager.currentLobby == null)
            {
                return null;
            }
            if (TogetherManager.currentLobby.service == null)
            {
                return null;
            }
            return TogetherManager.currentLobby.service;
        }

        public static bool IsLanActive()
        {
            return lan != null && lan.IsConnected;
        }

        public static bool IsLobbyActive()
        {
            return TogetherManager.ActiveLobby != null;
        }

        public static bool IsLobbyOwner()
        {
            return TogetherManager.ActiveLobby != null && TogetherManager.ActiveLobby.IsOwner();
        }

        public static void SendToAll(byte[] data)
        {
            if (IsLanActive())
            {
                lan.SendPacket(data);
            }
            else
            {
                Service()?.SendPacket(data);
            }
        }

        public static void UpdateLobbyData()
        {
            if (TogetherManager.IsLanMode)
            {
                return;
            }
            if (TogetherManager.currentLobby != null)
            {
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add("owner", TogetherManager.currentUser.userName);
                dictionary.Add("members", TogetherManager.currentLobby.GetMemberNameList());
                TogetherManager.currentLobby.SetMetadata(dictionary);
            }
        }

        public static void CreateLobby()
        {
            Debug.Log("Creating Lobby...");
            steam.CreateLobby();
        }

        public static void SetLobbyPrivate(bool toggle)
        {
            TogetherManager.ActiveLobby?.SetPrivate(toggle);
        }

        public static void LeaveLobby()
        {
            if (IsLanActive())
            {
                lan.Disconnect();
                return;
            }
            if (TogetherManager.currentLobby != null)
            {
                bool wasOwner = TogetherManager.currentLobby.IsOwner();
                if (wasOwner && TogetherManager.players.Count > 1)
                {
                    TogetherManager.currentLobby.NewOwner();
                }
                TogetherManager.currentLobby.LeaveLobby();
                MultiLucySkelController.CleanupAllRemotePlayers();
                TogetherManager.ClearMultiplayerData();
                VoteManager.Instance.AbortAllVotes();
            }
        }

        public static void DisbandLobby()
        {
            if (IsLanActive())
            {
                lan.Disconnect();
                return;
            }
            if (TogetherManager.currentLobby == null)
            {
                return;
            }

            if (!TogetherManager.currentLobby.IsOwner())
            {
                LeaveLobby();
                return;
            }

            MessageDispatcher.Send(new LobbyClosedMessage { LobbyId = TogetherManager.ActiveLobby?.steamID.m_SteamID ?? 0, Reason = "Owner disbanded lobby" });
            TogetherManager.currentLobby.SetJoinable(false);
            TogetherManager.currentLobby.SetPrivate(true);
            TogetherManager.currentLobby.LeaveLobby();
            MultiLucySkelController.CleanupAllRemotePlayers();
            TogetherManager.ClearMultiplayerData();
            VoteManager.Instance.AbortAllVotes();
        }

        public static void GetLobbies()
        {
            lobbies.Clear();
            steam.GetLobbies();
        }

        public static void AddPlayer(RemotePlayer player)
        {
            if (player == null)
            {
                return;
            }

            foreach (RemotePlayer player2 in TogetherManager.players)
            {
                if (player2.IsUser(player.steamUser))
                {
                    return;
                }
            }
            TogetherManager.players.Add(player);
            Debug.Log("Member joined: " + player.userName);
            VoteManager.Instance.SyncPlayersWithLobby();
        }

        public static void RemovePlayer(RemotePlayer player)
        {
            if (player == null)
            {
                return;
            }

            for (int i = TogetherManager.players.Count - 1; i >= 0; i--)
            {
                if (TogetherManager.players[i].IsUser(player.steamUser))
                {
                    Debug.Log("Member left: " + TogetherManager.players[i].userName);
                    TogetherManager.players.RemoveAt(i);
                }
            }

            VoteManager.Instance.SyncPlayersWithLobby();
        }
    }
}
