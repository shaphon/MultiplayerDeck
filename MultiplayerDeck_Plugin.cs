using ChronoArkMod;
using ChronoArkMod.Plugin;
using GameDataEditor;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static readonly int timeoutMaxFrame = 600;
        public static bool IsMultiplayer => TogetherManager.currentLobby != null;
        public static bool IsLobbyOwner
        {
            get
            {
                return IsMultiplayer
                    && TogetherManager.currentLobby.IsOwner();
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

                Debug.Log("[DeckSync] Enter BattleSystem.BattleStart Postfix");

                __result = WaitForCombinedDeckReceived(__result);
                IEnumerator WaitForCombinedDeckReceived(IEnumerator origin)
                {
                    BattleSyncManager.Instance.Initialize();
                    if (IsLobbyOwner)
                    {
                        Debug.Log("[DeckSync] Host Start Waiting For Combined Deck Received.");
                        BattleSyncManager.Instance.SendRequestForBattleStartDeck();
                    }
                    else
                    {
                        Debug.Log("[DeckSync] Client Start Waiting For Combined Deck Received.");
                    }
                    int waitFrame = 0;
                    while (!BattleSyncManager.Instance.battleStartDeckManager.deckReceived)
                    {
                        waitFrame++;
                        if (waitFrame >= timeoutMaxFrame)
                        {
                            Debug.LogError("[DeckSync] Timed out waiting for battle start deck. Continuing with local deck.");
                            BattleSyncManager.Instance.battleStartDeckManager.deckReceived = true;
                            break;
                        }
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

        [HarmonyPatch(typeof(FieldSystem), "BattleStartCo")]
        public static class BattleStartSyncPatch
        {
            [HarmonyPostfix]
            public static void Postfix(GDEEnemyQueueData QueueData, string MapKey, bool curse, string RewardKey, string Preset, bool NomalBattle, bool NoGameOver)
            {
                if(!IsMultiplayer)
                {
                    return;
                }
                NetworkHelper.SendBattleStart(QueueData.Key, NomalBattle, curse, RewardKey, Preset, NoGameOver);
            }
        }

        [HarmonyPatch]
        public static class NextStagePrevention
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(MiniBossObject), "GoCamp");
                yield return AccessTools.Method(typeof(FieldSystem), "ReturnArkButton");
                yield return AccessTools.Method(typeof(Camp), "NextMap");
                yield return AccessTools.Method(typeof(Door), "Trigger");
                yield return AccessTools.Method(typeof(RedWall), "Enter");
                yield return AccessTools.Method(typeof(Camp), "NextMap_Master");
            }

            public static bool Prefix()
            {
                if (!IsMultiplayer)
                {
                    return true;
                }
                if (StageSyncManager.Instance.forceNextStage)
                {
                    StageSyncManager.Instance.forceNextStage = false;
                    return true;
                }
                return false;
            }
        }

        [HarmonyPatch]
        public static class ActivateNextStageVotePatch
        {
            [HarmonyPatch(typeof(CharSelectMainUIV2), "Apply")]
            [HarmonyPostfix]
            public static void CharSelectionApply()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                if (!VoteManager.Instance.HasActiveVote(VoteManager.VoteTheme.FirstStage))
                {
                    VoteManager.Instance.StartVote(VoteManager.VoteTheme.FirstStage);
                }
            }


            [HarmonyPatch(typeof(FieldSystem), "BattleEnd")]
            [HarmonyPostfix]
            public static void BattleEnd(ref IEnumerator __result)
            {
                if (!IsMultiplayer)
                {
                    return;
                }

                __result = StartNextStageVote(__result);
                IEnumerator StartNextStageVote(IEnumerator origin)
                {
                    yield return origin;
                    VoteManager.Instance.AbortVote(VoteManager.VoteTheme.TurnEnd);
                    Debug.Log("[MultiplayerDeck] CanNextStage: " + StageSyncManager.Instance.bossClear);
                    if (StageSyncManager.Instance.bossClear)
                    {
                        VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage);
                    }
                }
            }

            [HarmonyPatch(typeof(FieldSystem), "CampfireMap")]
            [HarmonyPostfix]
            public static void Campfire()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage);
            }

            [HarmonyPatch(typeof(RedWall), "CrimsonWallOff")]
            [HarmonyPostfix]
            public static void CrimsonWilderness()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                VoteManager.Instance.StartVote(VoteManager.VoteTheme.EnterCrimson);
            }

            [HarmonyPatch(typeof(StageCrimsonChest), "RDMainReward")]
            [HarmonyPostfix]
            public static void LeaveCrimsonWilderness()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                VoteManager.Instance.StartVote(VoteManager.VoteTheme.NextStage);
            }

            [HarmonyPatch(typeof(UseNecklaceUI), "On")]
            [HarmonyPostfix]
            public static void UltimateAzar()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                VoteManager.Instance.StartVote(VoteManager.VoteTheme.EnterAzar);
            }
        }

        [HarmonyPatch]
        public static class StageMapSyncPatch
        {
            [HarmonyPatch(typeof(HexGenerator), "GeneratorMap")]
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

            [HarmonyPatch(typeof(FieldSystem), "StageStart")]
            [HarmonyPostfix]
            public static void ServerSendMapAndClientLoadMapCompletion()
            {
                if (!IsMultiplayer)
                {
                    return;
                }
                if (IsLobbyOwner)
                {
                    StageMapSyncHelper.NetStageMapPacket packet = StageMapSyncHelper.CreateMapPacket();
                    byte[] data = StageMapSyncHelper.SerializeMapPacket(packet);
                    Debug.Log("[MultiplayerDeck] Map Packet Size: " + data.Count());
                    NetworkHelper.Service()?.SendPacket(StageMapSyncHelper.SerializeMapPacket(packet));
                }
                else
                {
                    NetworkHelper.SendData(NetDataType.NextStageComplete);
                }
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

                Debug.Log("[BossClear] MiniBossClearSetterPatch Enter");

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

                Debug.Log("[BossClear] BossClearSetterPatch Enter");

                if (value && !StageSyncManager.Instance.bossClear)
                {
                    StageSyncManager.Instance.bossClear = true;
                    NetworkHelper.SendData(NetDataType.BossClear);
                }
            }
        }
    }
}
