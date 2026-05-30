using MultiplayerDeck.Network;
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
            NextStage,
            FirstStage,
            EnterCrimson,
            EnterAzar
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
                return playerVotes.Count >= GetTotalPlayerCount();
            }

            public int GetTotalPlayerCount()
            {
                if (TogetherManager.ActiveLobby != null)
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

        private readonly Dictionary<VoteTheme, VoteSession> activeVotes = new Dictionary<VoteTheme, VoteSession>();
        private readonly Dictionary<VoteTheme, Action> voteCallbacks = new Dictionary<VoteTheme, Action>();
        private VoteUI currentVoteUI;

        public event Action<VoteSession> OnVoteStarted;
        public event Action<VoteSession> OnVoteUpdated;
        public event Action<VoteSession> OnVoteEnded;

        public bool syncing;

        public IReadOnlyList<VoteSession> GetActiveVotes()
        {
            return activeVotes.Values.Where(vote => vote != null && vote.isActive).OrderBy(vote => vote.voteTheme).ToList();
        }

        public bool HasActiveVote(VoteTheme voteTheme)
        {
            VoteSession session;
            return activeVotes.TryGetValue(voteTheme, out session) && session != null && session.isActive;
        }

        public VoteSession GetVote(VoteTheme voteTheme)
        {
            VoteSession session;
            if (activeVotes.TryGetValue(voteTheme, out session) && session != null && session.isActive)
            {
                return session;
            }
            return null;
        }

        public void StartVote(VoteTheme voteTheme)
        {
            StartVoteInternal(voteTheme, GetCallbackForTheme(voteTheme), true);
        }

        public void StartVote(VoteTheme voteTheme, Action onVoteComplete)
        {
            StartVoteInternal(voteTheme, onVoteComplete, true);
        }

        public void StartVoteFromNetwork(VoteTheme voteTheme)
        {
            if (syncing)
            {
                return;
            }
            StartVoteInternal(voteTheme, GetCallbackForTheme(voteTheme), false);
        }

        private VoteSession StartVoteInternal(VoteTheme voteTheme, Action onVoteComplete, bool broadcast)
        {
            VoteSession existing = GetVote(voteTheme);
            if (existing != null)
            {
                return existing;
            }

            if (onVoteComplete != null)
            {
                voteCallbacks[voteTheme] = onVoteComplete;
            }

            VoteSession session = new VoteSession
            {
                voteTheme = voteTheme,
                onVoteComplete = onVoteComplete ?? GetCallbackForTheme(voteTheme),
                isActive = true
            };

            foreach (RemotePlayer player in TogetherManager.players)
            {
                session.playerVotes[player.steamUser.m_SteamID] = false;
            }
            if (TogetherManager.currentUser != null && !session.playerVotes.ContainsKey(TogetherManager.currentUser.steamUser.m_SteamID))
            {
                session.playerVotes[TogetherManager.currentUser.steamUser.m_SteamID] = false;
            }

            activeVotes[voteTheme] = session;
            Debug.Log("[VoteManager] Started vote for: " + voteTheme);

            if (broadcast)
            {
                MessageSerializer.SendVoteStart(voteTheme);
            }

            OnVoteStarted?.Invoke(session);
            OnVoteUpdated?.Invoke(session);
            return session;
        }

        public void Vote(VoteTheme voteTheme, bool cancel = false)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                Debug.LogWarning("[VoteManager] No active vote session for: " + voteTheme);
                return;
            }

            if (TogetherManager.currentUser == null)
            {
                Debug.LogWarning("[VoteManager] Cannot vote before current user is initialized.");
                return;
            }

            ulong playerId = TogetherManager.currentUser.steamUser.m_SteamID;
            ApplyVote(session, playerId, cancel);
            MessageSerializer.SendVote(voteTheme, playerId, cancel);
        }

        public void VoteFromNetwork(VoteTheme voteTheme, ulong playerId, bool cancel)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                session = StartVoteInternal(voteTheme, GetCallbackForTheme(voteTheme), false);
            }

            ApplyVote(session, playerId, cancel);
        }

        private void ApplyVote(VoteSession session, ulong playerId, bool cancel)
        {
            session.playerVotes[playerId] = !cancel;
            Debug.Log("[VoteManager] Vote " + session.voteTheme + " player " + playerId + ": " + (cancel ? "No" : "Yes"));

            OnVoteUpdated?.Invoke(session);
            CheckVoteCompletion(session.voteTheme);
        }

        private void CheckVoteCompletion(VoteTheme voteTheme)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                return;
            }

            if (session.IsCompleted)
            {
                CompleteVote(voteTheme);
            }
        }

        private void CompleteVote(VoteTheme voteTheme)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                return;
            }

            session.isActive = false;
            activeVotes.Remove(voteTheme);
            Debug.Log("[VoteManager] Vote completed. Type: " + voteTheme);

            session.onVoteComplete?.Invoke();
            OnVoteEnded?.Invoke(session);
        }

        public void AbortVote(VoteTheme voteTheme)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                return;
            }

            session.isActive = false;
            activeVotes.Remove(voteTheme);
            OnVoteEnded?.Invoke(session);
        }

        public void AbortAllVotes()
        {
            List<VoteSession> sessions = GetActiveVotes().ToList();
            activeVotes.Clear();
            foreach (VoteSession session in sessions)
            {
                session.isActive = false;
                OnVoteEnded?.Invoke(session);
            }
        }

        public void SyncPlayersWithLobby()
        {
            foreach (VoteSession session in GetActiveVotes())
            {
                SyncPlayers(session);
                OnVoteUpdated?.Invoke(session);
                CheckVoteCompletion(session.voteTheme);
            }
        }

        private void SyncPlayers(VoteSession session)
        {
            HashSet<ulong> activePlayers = new HashSet<ulong>();
            foreach (RemotePlayer player in TogetherManager.players)
            {
                activePlayers.Add(player.steamUser.m_SteamID);
                if (!session.playerVotes.ContainsKey(player.steamUser.m_SteamID))
                {
                    session.playerVotes[player.steamUser.m_SteamID] = false;
                }
            }

            List<ulong> removedPlayers = session.playerVotes.Keys.Where(id => !activePlayers.Contains(id)).ToList();
            foreach (ulong playerId in removedPlayers)
            {
                session.playerVotes.Remove(playerId);
            }
        }

        public bool HasPlayerVotedYes(RemotePlayer player)
        {
            if (player == null)
            {
                return false;
            }

            return GetActiveVotes().Any(session => HasPlayerVotedYes(player, session.voteTheme));
        }

        public bool HasPlayerVotedYes(RemotePlayer player, VoteTheme voteTheme)
        {
            if (player == null)
            {
                return false;
            }

            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                return false;
            }

            bool voted;
            return session.playerVotes.TryGetValue(player.steamUser.m_SteamID, out voted) && voted;
        }

        public bool HasLocalPlayerVotedYes(VoteTheme voteTheme)
        {
            return HasPlayerVotedYes(TogetherManager.GetCurrentUser(), voteTheme);
        }

        public bool UnlockYesVoteButton(VoteTheme voteTheme)
        {
            if (HasLocalPlayerVotedYes(voteTheme))
            {
                return false;
            }
            if (voteTheme == VoteTheme.FirstStage)
            {
                return PlayData.TSavedata.Party.Count > 0;
            }
            if (voteTheme == VoteTheme.EnterCrimson)
            {
                return PlayData.TSavedata.Crimson_Open;
            }
            if (voteTheme == VoteTheme.EnterAzar)
            {
                return PlayData.TSavedata.UseNecklaceOn;
            }
            return true;
        }

        public int GetYesVoteCount(VoteTheme voteTheme)
        {
            VoteSession session = GetVote(voteTheme);
            if (session == null)
            {
                return 0;
            }

            return session.playerVotes.Values.Count(voted => voted);
        }

        private Action GetCallbackForTheme(VoteTheme voteTheme)
        {
            Action callback;
            if (voteCallbacks.TryGetValue(voteTheme, out callback))
            {
                return callback;
            }

            switch (voteTheme)
            {
                case VoteTheme.TurnEnd:
                    return TurnEnd;
                case VoteTheme.NextStage:
                    return ServerGotoNextStage;
                case VoteTheme.FirstStage:
                    return ServerGotoNextStage;
                case VoteTheme.EnterCrimson:
                    return GotoCrimsonWilderness;
                case VoteTheme.EnterAzar:
                    return GotoUltimateAzar;
                default:
                    return null;
            }
        }

        private void TurnEnd()
        {
            if (BattleSystem.instance == null)
            {
                return;
            }

            BattleSyncManager.Instance.turnEnding = true;

            BattleSystem.instance.TargetSelectCancel();
            BattleSystem.instance.ActWindow.WasteButton.Quit();
            BattleSystem.instance.ActWindow.On = false;
            BattleSystem.instance.ActWindow.TurnEndFlag = true;
            BattleSystem.instance.StartCoroutine(BattleSystem.instance.EnemyTurn(true));
        }

        public static void ServerGotoNextStage()
        {
            if (MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                StageSyncManager.Instance.GotoNextStage();
            }
        }

        public static void GotoCrimsonWilderness()
        {
            StageSyncManager.Instance.GotoNextStage(crimson: true);
        }

        public static void GotoUltimateAzar()
        {
            StageSyncManager.Instance.GotoNextStage(azar: true);
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
