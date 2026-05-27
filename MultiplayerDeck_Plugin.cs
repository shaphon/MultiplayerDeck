using ChronoArkMod;
using ChronoArkMod.Plugin;
using GameDataEditor;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerDeck
{
    public class MultiplayerDeck_Plugin : ChronoArkPlugin
    {
        private Harmony harmony;

        public override void Initialize()
        {
            this.harmony = new Harmony(base.GetGuid());
            this.harmony.PatchAll();
        }

        public override void Dispose()
        {
            this.harmony.UnpatchSelf();
        }

        public static bool IsMultiplayer => TogetherManager.currentLobby != null;

        public static bool IsLobbyOwner
        {
            get
            {
                return IsMultiplayer
                    && TogetherManager.currentLobby.ownerID == TogetherManager.currentUser.steamUser;
            }
        }

        private static readonly int timeoutMaxFrame = 600;

        [HarmonyPatch(typeof(BattleSystem), "BattleStart")]
        public static class BattleStartDeckPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref IEnumerator __result)
            {
                if (!IsMultiplayer)
                {
                    return;
                }

                BattleSyncManager.Instance.Initialize();
                if (IsLobbyOwner)
                {
                    BattleSyncManager.Instance.SendRequestForBattleStartDeck();
                }

                __result = WaitForCombinedDeckReceived(__result);
                IEnumerator WaitForCombinedDeckReceived(IEnumerator origin)
                {
                    while (!BattleSyncManager.Instance.battleStartDeckManager.deckReceived)
                    {
                        yield return new WaitForFixedUpdate();
                    }
                    yield return origin;
                }
            }
        }

        [HarmonyPatch(typeof(BattleSystem))]
        public static class TurnEndSyncPatch
        {
            [HarmonyPatch("TurnEnd")]
            [HarmonyPrefix]
            public static bool TurnEndPrefix(BattleSystem __instance)
            {
                if (!IsMultiplayer)
                {
                    return true;
                }

                __instance.ActWindow.On = false;
                VoteManager.Instance.Vote(VoteManager.VoteTheme.TurnEnd);
                return false;
            }

            [HarmonyPatch("ForceTurnEnd")]
            [HarmonyPostfix]
            public static void ForceTurnEndPostfix(BattleSystem __instance, ref IEnumerator __result)
            {
                if (!IsMultiplayer)
                {
                    return;
                }

                __result = ForceTurnEndIEnumerator(__result);
                IEnumerator ForceTurnEndIEnumerator(IEnumerator origin)
                {
                    __instance.ActWindow.On = false;
                    VoteManager.Instance.Vote(VoteManager.VoteTheme.TurnEnd);
                    yield break;
                }
            }
        }

        [HarmonyPatch(typeof(SkillButton), "_Waste")]
        public static class ExchangePatch
        {
            [HarmonyPrefix]
            public static void Prefix(SkillButton __instance)
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                if (__instance.BClickWaste)
                {
                    int seed = __instance.Myskill.CharinfoSkilldata.Seed;
                    NetworkHelper.SendExchangeSkill(BattleSyncManager.RandomOtherPlayerId(), __instance.Myskill.CloneSkill(true));
                    BattleSystem.instance.StartCoroutine(RemoveWastedSkill());
                    IEnumerator RemoveWastedSkill()
                    {
                        yield return new WaitForFixedUpdate();
                        Skill skill = BattleSystem.instance.AllyTeam.Skills_UsedDeck.FirstOrDefault(s => s.CharinfoSkilldata.Seed == seed);
                        if (skill != null)
                        {
                            BattleSystem.instance.AllyTeam.Skills_UsedDeck.Remove(skill);
                            yield break;
                        }
                        skill = BattleSystem.instance.AllyTeam.Skills_Deck.FirstOrDefault(s => s.CharinfoSkilldata.Seed == seed);
                        if (skill != null)
                        {
                            BattleSystem.instance.AllyTeam.Skills_Deck.Remove(skill);
                            yield break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FieldSystem), "BattleStart")]
        public static class BattleStartSyncPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GDEEnemyQueueData QueueData, string MapKey, bool NomalBattle, bool Curse, string RewardKey, string Preset, bool NoGameover)
            {
                if(!IsMultiplayer)
                {
                    return;
                }
                NetworkHelper.SendBattleStart(QueueData.Key, NomalBattle, Curse, RewardKey, Preset, NoGameover);
            }
        }

        [HarmonyPatch]
        public static class NextStageSyncPatch
        {
            [HarmonyPatch(typeof(MiniBossObject), "GoCamp")]
            [HarmonyPrefix]
            public static bool MiniBossNextStagePrevention()
            {
                if (!IsMultiplayer)
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(FieldSystem), "ReturnArkButton")]
            [HarmonyPrefix]
            public static bool BossNextStagePrevention()
            {
                if (!IsMultiplayer)
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(Camp), "NextMap")]
            [HarmonyPrefix]
            public static bool CampNextStagePrevention()
            {
                if (!IsMultiplayer)
                {
                    return true;
                }
                return false;
            }

            [HarmonyPatch(typeof(Door), "Trigger")]
            [HarmonyPrefix]
            public static bool DoorNextStagePrevention()
            {
                if (!IsMultiplayer)
                {
                    return true;
                }
                if (VoteManager.Instance.currentVoteSession == null)
                {
                    VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage, ServerGotoNextStage);
                }
                return false;
            }


            [HarmonyPatch(typeof(FieldSystem), "BattleEnd")]
            [HarmonyPostfix]
            public static void NextStageVote(ref IEnumerator __result)
            {
                if (!IsMultiplayer)
                {
                    return;
                }

                __result = StartNextStageVote(__result);
                IEnumerator StartNextStageVote(IEnumerator origin)
                {
                    yield return origin;
                    VoteManager.Instance.AbortCurrentVote();
                    Debug.Log("[MultiplayerDeck] CanNextStage: " + StageSyncManager.Instance.bossClear);
                    if (StageSyncManager.Instance.bossClear)
                    {
                        VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage, ServerGotoNextStage);
                    }
                }
            }

            [HarmonyPatch(typeof(FieldSystem), "CampfireMap")]
            [HarmonyPostfix]
            public static void NextStageVoteCamp()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage, ServerGotoNextStage);
            }
        }

        [HarmonyPatch(typeof(HexGenerator), "GeneratorMap")]
        public static class StageMapSyncPatch
        {
            [HarmonyPrefix]
            public static bool ClientLoadReceivedMap(ref HexMap __result)
            {
                if (!IsMultiplayer || IsLobbyOwner)
                {
                    return true;
                }
                if (StageMapSyncHelper.mapPacket != null)
                {
                    var packet = StageMapSyncHelper.mapPacket;
                    StageMapSyncHelper.mapPacket = null;
                    __result = StageMapSyncHelper.LoadRemoteMap(packet);
                    return false;
                }
                Debug.LogWarning("[MultiplayerDeck] StageMapSyncHelper.mapPacket is null. Map Not Synchronized");
                return true;
            }

            [HarmonyPostfix]
            public static void ServerSendMap(HexMap __result)
            {
                if (!IsMultiplayer || !IsLobbyOwner)
                {
                    return;
                }
                StageMapSyncHelper.NetStageMapPacket packet = StageMapSyncHelper.CreateMapPacket(__result);
                byte[] data = StageMapSyncHelper.SerializeMapPacket(packet);
                Debug.Log("[MultiplayerDeck] Map Packet Size: " + data.Count());
                NetworkHelper.Service()?.SendPacket(StageMapSyncHelper.SerializeMapPacket(packet));
            }
        }


        [HarmonyPatch(typeof(StageSystem), "MonsterTileDelete")]
        public static class MonsterClearPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Vector2 Pos)
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                NetworkHelper.SendMonsterClear(Pos);
            }
        }

        [HarmonyPatch]
        public static class BossClearPatch
        {
            [HarmonyPatch(typeof(MiniBossObject), "BossClear", MethodType.Setter)]
            [HarmonyPostfix]
            public static void MiniBossClear(bool value)
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                if (value && !StageSyncManager.Instance.bossClear)
                {
                    StageSyncManager.Instance.bossClear = true;
                    NetworkHelper.SendData(NetDataType.BossClear);
                }
            }

            [HarmonyPatch(typeof(Stage1Events), "BossClear", MethodType.Setter)]
            [HarmonyPostfix]
            public static void BossClear(bool value)
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                if (value && !StageSyncManager.Instance.bossClear)
                {
                    StageSyncManager.Instance.bossClear = true;
                    NetworkHelper.SendData(NetDataType.BossClear);
                }
            }
        }

        public static void ServerGotoNextStage()
        {
            if (MultiplayerDeck_Plugin.IsLobbyOwner)
            {
                StageSyncManager.Instance.GotoNextStage();
            }
        }
    }

    public class MultiplayerSync : ChronoArkPluginMonoBehaviour
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
                GUI.Label(voteRect, "\u2713", voteBadgeStyle);
            }
        }

        private void DrawVotePanel()
        {
            VoteManager.VoteSession session = VoteManager.Instance.currentVoteSession;
            GUILayout.BeginVertical(GUI.skin.box);
            if (session == null || !session.isActive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No active vote", mutedStyle);
                GUILayout.FlexibleSpace();
                /*if (GUILayout.Button("Vote Next", GUILayout.Width(110f), GUILayout.Height(28f)))
                {
                    if (VoteManager.Instance.currentVoteSession == null)
                    {
                        VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage, MultiplayerDeck_Plugin.ServerGotoNextStage);
                    }
                    VoteManager.Instance.Vote(VoteManager.VoteTheme.NextStage);
                }*/
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Vote: " + GetVoteTitle(session.voteTheme), subTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(string.Format("{0}/{1}", VoteManager.Instance.GetYesVoteCount(), session.GetTotalPlayerCount()), mutedStyle, GUILayout.Width(50f));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = !VoteManager.Instance.HasLocalPlayerVotedYes();
            if (GUILayout.Button("Agree", GUILayout.Height(30f)))
            {
                VoteManager.Instance.Vote(session.voteTheme);
            }
            GUI.enabled = VoteManager.Instance.HasLocalPlayerVotedYes();
            if (GUILayout.Button("Cancel", GUILayout.Height(30f)))
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
