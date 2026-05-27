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
                StageMapSyncHelper.NetStageMapPacket packet = StageMapSyncHelper.CreateMapPacket();
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
        private Rect windowRect = new Rect(1032f, 700f, 520f, 95f);

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
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Lobby") || (windowShow && Input.GetKeyDown(KeyCode.Return)))
            {
                NetworkHelper.CreateLobby();
            }

            GUI.enabled = MultiplayerDeck_Plugin.IsMultiplayer;
            if (GUILayout.Button("Vote Next"))
            {
                VoteManager.Instance.Vote(VoteManager.VoteTheme.NextStage);
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
    }
}
