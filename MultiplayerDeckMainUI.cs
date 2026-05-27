using ChronoArkMod.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerDeck
{
    public class MultiplayerDeckMainUI : ChronoArkPluginMonoBehaviour
    {
        private bool steamInitialized;
        private bool windowShow = true;
        private Rect windowRect = new Rect(980f, 520f, 560f, 330f);
        private Vector2 lobbyScroll;
        private GUIStyle titleStyle;
        private GUIStyle subTitleStyle;
        private GUIStyle playerNameStyle;
        private GUIStyle mutedStyle;
        private GUIStyle badgeStyle;
        private GUIStyle voteBadgeStyle;
        private Texture2D fallbackAvatar;
        private Texture2D panelTexture;
        private Texture2D rowTexture;
        private Texture2D ownerTexture;
        private Texture2D voteTexture;

        private void Update()
        {
            InitializeSteamWhenReady();

            if (steamInitialized)
            {
                NetworkHelper.Update();
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                windowShow = !windowShow;
            }

            BattleSyncManager.Instance.Tick();
            StageSyncManager.Instance.Tick();
        }

        private void InitializeSteamWhenReady()
        {
            if (steamInitialized)
            {
                return;
            }

            if (SteamManager.Initialized)
            {
                NetworkHelper.Initialize();
                steamInitialized = true;
            }
        }

        private void OnGUI()
        {
            if (!windowShow)
            {
                return;
            }

            windowRect = GUILayout.Window(123, windowRect, Window, "Multiplayer Deck");
        }

        private void Window(int id)
        {
            EnsureGuiResources();
            DrawHeader();

            if (!steamInitialized)
            {
                GUILayout.Label("Steam is not initialized.", mutedStyle);
                GUI.DragWindow();
                return;
            }

            if (!MultiplayerDeck_Plugin.IsMultiplayer)
            {
                DrawNoLobby();
                GUI.DragWindow();
                return;
            }

            DrawLobby();
            GUI.DragWindow();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Multiplayer Deck", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("F: Toggle", mutedStyle, GUILayout.Width(90f));
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawNoLobby()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("No active lobby", subTitleStyle);
            GUILayout.Label("Create a Steam lobby to start multiplayer synchronization.", mutedStyle);
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Lobby", GUILayout.Height(34f)) || (windowShow && Input.GetKeyDown(KeyCode.Return)))
            {
                NetworkHelper.CreateLobby();
            }
            if (GUILayout.Button("Refresh Lobbies", GUILayout.Height(34f)))
            {
                NetworkHelper.GetLobbies();
            }
            GUILayout.EndHorizontal();
            DrawLobbyList();
            GUILayout.EndVertical();
        }

        private void DrawLobbyList()
        {
            if (NetworkHelper.lobbies == null || NetworkHelper.lobbies.Count == 0)
            {
                return;
            }

            GUILayout.Space(10f);
            GUILayout.Label("Available Lobbies", subTitleStyle);
            foreach (SteamLobby lobby in NetworkHelper.lobbies)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(string.Format("{0}  ({1}/{2})", string.IsNullOrEmpty(lobby.name) ? lobby.owner + "'s Lobby" : lobby.name, lobby.GetMemberCount(), lobby.GetCapacity()), playerNameStyle);
                if (GUILayout.Button("Join", GUILayout.Width(70f)))
                {
                    lobby.Join();
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawLobby()
        {
            SteamLobby lobby = TogetherManager.currentLobby;
            lobby.FetchAllMetadata();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(string.IsNullOrEmpty(lobby.name) ? lobby.owner + "'s Lobby" : lobby.name, subTitleStyle);
            GUILayout.Label(string.Format("Owner: {0}     Players: {1}/{2}", lobby.owner, TogetherManager.players.Count, lobby.GetCapacity()), mutedStyle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Leave", GUILayout.Width(78f), GUILayout.Height(30f)))
            {
                NetworkHelper.LeaveLobby();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            GUI.enabled = lobby.IsOwner();
            if (GUILayout.Button("Disband", GUILayout.Width(86f), GUILayout.Height(30f)))
            {
                NetworkHelper.DisbandLobby();
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            DrawPlayers();
            GUILayout.Space(8f);
            DrawVotePanel();
            GUILayout.EndVertical();
        }

        private void DrawPlayers()
        {
            lobbyScroll = GUILayout.BeginScrollView(lobbyScroll, false, true, GUILayout.Height(150f));
            foreach (RemotePlayer player in TogetherManager.players)
            {
                DrawPlayerRow(player);
            }
            GUILayout.EndScrollView();
        }

        private void DrawPlayerRow(RemotePlayer player)
        {
            Rect rowRect = GUILayoutUtility.GetRect(1f, 54f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rowRect, rowTexture);

            Rect avatarRect = new Rect(rowRect.x + 8f, rowRect.y + 7f, 40f, 40f);
            GUI.DrawTexture(avatarRect, player.pixmap != null ? player.pixmap : fallbackAvatar, ScaleMode.ScaleToFit);

            Rect nameRect = new Rect(rowRect.x + 58f, rowRect.y + 8f, rowRect.width - 170f, 22f);
            GUI.Label(nameRect, string.IsNullOrEmpty(player.userName) ? "Unknown Player" : player.userName, playerNameStyle);

            if (TogetherManager.currentLobby != null && player.IsUser(TogetherManager.currentLobby.ownerID))
            {
                Rect ownerRect = new Rect(rowRect.x + 58f, rowRect.y + 31f, 54f, 18f);
                GUI.DrawTexture(ownerRect, ownerTexture);
                GUI.Label(ownerRect, "Owner", badgeStyle);
            }

            if (VoteManager.Instance.HasPlayerVotedYes(player))
            {
                Rect voteRect = new Rect(rowRect.x + rowRect.width - 46f, rowRect.y + 12f, 30f, 30f);
                GUI.DrawTexture(voteRect, voteTexture);
                int voteCount = VoteManager.Instance.GetActiveVotes().Count(vote => VoteManager.Instance.HasPlayerVotedYes(player, vote.voteTheme));
                GUI.Label(voteRect, voteCount > 1 ? "\u2713" + voteCount : "\u2713", voteBadgeStyle);
            }
        }

        private void DrawVotePanel()
        {
            IReadOnlyList<VoteManager.VoteSession> sessions = VoteManager.Instance.GetActiveVotes();
            GUILayout.BeginVertical(GUI.skin.box);
            if (sessions.Count == 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No active vote", mutedStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            foreach (VoteManager.VoteSession session in sessions)
            {
                DrawVoteSession(session);
            }
            GUILayout.EndVertical();
        }

        private void DrawVoteSession(VoteManager.VoteSession session)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vote: " + GetVoteTitle(session.voteTheme), subTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("{0}/{1}", VoteManager.Instance.GetYesVoteCount(session.voteTheme), session.GetTotalPlayerCount()), mutedStyle, GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = VoteManager.Instance.UnlockYesVoteButton(session.voteTheme);
            if (GUILayout.Button("Agree", GUILayout.Height(28f)))
            {
                VoteManager.Instance.Vote(session.voteTheme);
            }
            GUI.enabled = VoteManager.Instance.HasLocalPlayerVotedYes(session.voteTheme);
            if (GUILayout.Button("Cancel", GUILayout.Height(28f)))
            {
                VoteManager.Instance.Vote(session.voteTheme, true);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static string GetVoteTitle(VoteManager.VoteTheme voteTheme)
        {
            switch (voteTheme)
            {
                case VoteManager.VoteTheme.TurnEnd:
                    return "End Turn";
                case VoteManager.VoteTheme.NextStage:
                    return "Next Stage";
                case VoteManager.VoteTheme.FirstStage:
                    return "Start First Stage";
                case VoteManager.VoteTheme.EnterCrimson:
                    return "Crimson Wilderness";
                case VoteManager.VoteTheme.EnterAzar:
                    return "Ultimate Azar";
                default:
                    return voteTheme.ToString();
            }
        }

        private void EnsureGuiResources()
        {
            if (titleStyle != null)
            {
                return;
            }

            panelTexture = MakeTexture(new Color(0.08f, 0.09f, 0.11f, 0.92f));
            rowTexture = MakeTexture(new Color(0.16f, 0.17f, 0.2f, 0.92f));
            ownerTexture = MakeTexture(new Color(0.24f, 0.31f, 0.45f, 0.95f));
            voteTexture = MakeTexture(new Color(0.16f, 0.62f, 0.29f, 0.95f));
            fallbackAvatar = MakeTexture(new Color(0.24f, 0.25f, 0.28f, 1f));

            GUI.skin.window.normal.background = panelTexture;
            GUI.skin.box.normal.background = panelTexture;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            subTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
            playerNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                clipping = TextClipping.Clip,
                normal = { textColor = Color.white }
            };
            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.72f, 0.75f, 0.8f, 1f) }
            };
            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { textColor = new Color(0.88f, 0.92f, 1f, 1f) }
            };
            voteBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
