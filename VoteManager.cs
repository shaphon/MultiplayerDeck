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
                    return TogetherManager.players.Count;
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

            Debug.Log($"Started vote for: {voteTheme}");
            OnVoteStarted?.Invoke(currentVoteSession);
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

            // 发送投票到网络
            NetworkHelper.SendVote(voteTheme, playerId, cancel);

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
            currentVoteSession = null;
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