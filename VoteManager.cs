using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiplayerDeck
{
    public class VoteManager
    {
        public enum VoteTheme
        {
            TurnEnd,
            NextStage
        }

        public class VoteSession
        {
            public VoteTheme voteTheme;
            public Dictionary<ulong, bool> playerVotes = new Dictionary<ulong, bool>();
            public Action onVoteComplete;
            public bool isActive;
            public bool IsCompleted
            {
                get
                {
                    if (!isActive)
                    {
                        return true;
                    }
                    return AreAllPlayersVoted() && playerVotes.Values.All(vote => vote);
                }
            }

            private bool AreAllPlayersVoted()
            {
                var totalPlayers = GetTotalPlayerCount();
                return playerVotes.Count >= totalPlayers;
            }

            public int GetTotalPlayerCount()
            {
                if (TogetherManager.currentLobby != null)
                {
                    return Math.Max(1, TogetherManager.players.Count);
                }
                return 1;
            }
        }

        private static VoteManager _instance;
        public static VoteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new VoteManager();
                }
                return _instance;
            }
        }

        public VoteSession currentVoteSession;
        private VoteUI currentVoteUI;

        public event Action<VoteSession> OnVoteStarted;
        public event Action<VoteSession> OnVoteUpdated;
        public event Action<VoteSession> OnVoteEnded;

        public void StartVote(VoteTheme voteTheme, Action onVoteComplete)
        {
            /*if (currentVoteSession != null && currentVoteSession.isActive)
            {
                Debug.LogWarning("There's already an active vote session!");
                return;
            }*/

            currentVoteSession = new VoteSession
            {
                voteTheme = voteTheme,
                onVoteComplete = onVoteComplete,
                isActive = true
            };

            foreach (var player in TogetherManager.players)
            {
                currentVoteSession.playerVotes[player.steamUser.m_SteamID] = false;
            }
            if (TogetherManager.currentUser != null && !currentVoteSession.playerVotes.ContainsKey(TogetherManager.currentUser.steamUser.m_SteamID))
            {
                currentVoteSession.playerVotes[TogetherManager.currentUser.steamUser.m_SteamID] = false;
            }

            Debug.Log($"Started vote for: {voteTheme}");
            OnVoteStarted?.Invoke(currentVoteSession);
            OnVoteUpdated?.Invoke(currentVoteSession);
        }

        public void Vote(VoteTheme voteTheme, bool cancel = false)
        {
            if (currentVoteSession == null || !currentVoteSession.isActive)
            {
                Debug.LogWarning("No active vote session!");
                return;
            }

            ulong playerId = TogetherManager.currentUser.steamUser.m_SteamID;
            if (currentVoteSession.playerVotes.ContainsKey(playerId))
            {
                currentVoteSession.playerVotes[playerId] = !cancel;
                Debug.Log($"Player {TogetherManager.currentUser.userName} voted: {(cancel ? "No" : "Yes")}");
            }
            else
            {
                currentVoteSession.playerVotes.Add(playerId, !cancel);
            }

            // 发送投票到网络
            NetworkHelper.SendVote(voteTheme, playerId, cancel);

            OnVoteUpdated?.Invoke(currentVoteSession);
            CheckVoteCompletion();
        }

        public void VoteFromNetwork(VoteTheme voteTheme, ulong playerId, bool cancel)
        {
            if (currentVoteSession == null || !currentVoteSession.isActive)
            {
                Debug.LogWarning("No active vote session to receive vote from network!");
                return;
            }

            if (currentVoteSession.voteTheme != voteTheme)
            {
                Debug.LogWarning("Not correct vote theme from network!");
                return;
            }

            if (currentVoteSession.playerVotes.ContainsKey(playerId))
            {
                currentVoteSession.playerVotes[playerId] = !cancel;
                Debug.Log($"Received vote from network - Player {playerId}: {(cancel ? "No" : "Yes")}");
            }
            else
            {
                currentVoteSession.playerVotes.Add(playerId, !cancel);
            }

            OnVoteUpdated?.Invoke(currentVoteSession);
            CheckVoteCompletion();
        }

        private void CheckVoteCompletion()
        {
            if (currentVoteSession == null) return;

            if (currentVoteSession.IsCompleted)
            {
                CompleteVote();
            }
        }

        private void CompleteVote()
        {
            if (currentVoteSession == null) return;

            currentVoteSession.isActive = false;
            Debug.Log($"Vote completed. Type: {currentVoteSession.voteTheme}");

            // 执行回调
            currentVoteSession.onVoteComplete?.Invoke();

            OnVoteEnded?.Invoke(currentVoteSession);

            // 清除当前投票会话
            currentVoteSession = null;
        }

        public void AbortCurrentVote()
        {
            if (currentVoteSession != null)
            {
                currentVoteSession.isActive = false;
                OnVoteEnded?.Invoke(currentVoteSession);
            }
            currentVoteSession = null;
        }

        public void SyncPlayersWithLobby()
        {
            if (currentVoteSession == null || !currentVoteSession.isActive)
            {
                return;
            }

            HashSet<ulong> activePlayers = new HashSet<ulong>();
            foreach (RemotePlayer player in TogetherManager.players)
            {
                activePlayers.Add(player.steamUser.m_SteamID);
                if (!currentVoteSession.playerVotes.ContainsKey(player.steamUser.m_SteamID))
                {
                    currentVoteSession.playerVotes[player.steamUser.m_SteamID] = false;
                }
            }

            List<ulong> removedPlayers = currentVoteSession.playerVotes.Keys.Where(id => !activePlayers.Contains(id)).ToList();
            foreach (ulong playerId in removedPlayers)
            {
                currentVoteSession.playerVotes.Remove(playerId);
            }

            OnVoteUpdated?.Invoke(currentVoteSession);
            CheckVoteCompletion();
        }

        public bool HasPlayerVotedYes(RemotePlayer player)
        {
            if (player == null || currentVoteSession == null || !currentVoteSession.isActive)
            {
                return false;
            }

            bool voted;
            return currentVoteSession.playerVotes.TryGetValue(player.steamUser.m_SteamID, out voted) && voted;
        }

        public bool HasLocalPlayerVotedYes()
        {
            return HasPlayerVotedYes(TogetherManager.GetCurrentUser());
        }

        public int GetYesVoteCount()
        {
            if (currentVoteSession == null || !currentVoteSession.isActive)
            {
                return 0;
            }

            return currentVoteSession.playerVotes.Values.Count(voted => voted);
        }
    }
    
    public class VoteUI
    {
        public GameObject Root { get; set; }
        public UnityEngine.UI.Text TitleText { get; set; }
        public UnityEngine.UI.Text StatusText { get; set; }
        public UnityEngine.UI.Button YesButton { get; set; }
        public UnityEngine.UI.Button NoButton { get; set; }
    }
}
