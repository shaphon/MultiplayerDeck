using System;
using System.Collections.Generic;
using GameDataEditor;
using ChronoArkMod.Plugin;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MultiplayerDeck
{
    public class MultiplayerDeck_Plugin : ChronoArkPlugin
    {
        private static GameObject runnerObject;

        public override void Initialize()
        {
            EnsureRunner();
        }

        public override void Dispose()
        {
            if (runnerObject != null)
            {
                Object.Destroy(runnerObject);
                runnerObject = null;
            }
        }

        private static void EnsureRunner()
        {
            if (runnerObject != null)
            {
                return;
            }

            runnerObject = new GameObject("MultiplayerDeck_Runtime");
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<Runtime>();
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

        public static void MyBossEnterFriend(string bossKey)
        {
            StartBattleFromNetwork(bossKey, true, false);
        }

        public static void StartBattleFromNetwork(string QueueData, bool NormalBattle, bool Cursed, string RewardKey, string Preset, bool NoGameover)
        {
            if (BattleSystem.instance != null || FieldSystem.instance == null || StageSystem.instance == null)
            {
                return;
            }

            FieldSystem.instance.BattleAfterDelegate = (FieldSystem.BattleAfterDel)Delegate.Combine(
                FieldSystem.instance.BattleAfterDelegate,
                new FieldSystem.BattleAfterDel(BossAfter));
            FieldSystem.instance.BattleStart(new GDEEnemyQueueData(QueueData),
                StageSystem.instance.StageData.BattleMap.Key,
                NormalBattle,
                Cursed,
                RewardKey,
                Preset,
                NoGameover);
        }

        public static void BossAfter()
        {
            if (StageSystem.instance != null)
            {
                StageSystem.instance.CanNextStage = true;
            }
        }

        public static string MyBossEnterMessage(StageSystem instance)
        {
            List<GDEEnemyQueueData> candidates = new List<GDEEnemyQueueData>();
            foreach (GDEEnemyQueueData boss in instance.StageData.Bosses)
            {
                if (PlayData.TSavedata.SpRule == null ||
                    PlayData.TSavedata.SpRule.RuleChange.BanBoss.Find(a => a == boss.Key) == null)
                {
                    if (!boss.Lock || SaveManager.IsUnlock(boss.Key, SaveManager.NowData.unlockList.UnlockBoss))
                    {
                        candidates.Add(boss);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                candidates.Add(instance.StageData.Bosses[0]);
            }

            if (PlayData.TSavedata.SwordSanctuary)
            {
                PlayData.TSavedata.SwordSanctuary = false;
                return GDEItemKeys.EnemyQueue_Queue_DorchiX;
            }

            return candidates.Random(RandomClassKey.Boss).Key;
        }

        public class BossBattleNet : PassiveBase
        {
            public readonly List<BattleEnemy> enemyList = new List<BattleEnemy>();

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (BattleSystem.instance == null)
                {
                    return;
                }

                foreach (BattleEnemy enemy in BattleSystem.instance.EnemyList)
                {
                    if (enemy.Boss && !enemyList.Contains(enemy))
                    {
                        enemyList.Add(enemy);
                        enemy.BuffAdd("GiantNet", enemy, false, 0, false, -1, false);
                        if (enemy.HP == 0)
                        {
                            enemy.Dead(false);
                        }
                    }
                }
            }
        }

        private class Runtime : MonoBehaviour
        {
            private bool steamInitialized;
            private bool windowShow = true;
            private Rect windowRect = new Rect(1032f, 700f, 520f, 95f);
            private string lastBattleKey;
            private string lastStageKey;
            private int lastTurnActionNum = -1;
            private int lastSoul = int.MinValue;
            private int lastGold = int.MinValue;
            private bool nextStageVoteAvailable;

            private void Update()
            {
                InitializeSteamWhenReady();

                if (steamInitialized)
                {
                    NetworkHelper.update();
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    windowShow = !windowShow;
                }

                MultiplayerBattleSync.Tick();
                GateNextStageBehindVote();
                BroadcastLocalBattleStart();
                //BroadcastStageState();
                BroadcastTurnActionNum();
                BroadcastSharedResources();
            }

            private void InitializeSteamWhenReady()
            {
                if (steamInitialized)
                {
                    return;
                }

                if (SteamManager.Initialized)
                {
                    NetworkHelper.initialize();
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
                    NetworkHelper.createLobby();
                }

                GUI.enabled = nextStageVoteAvailable || !IsMultiplayer;
                if (GUILayout.Button("Vote Next"))
                {
                    NetworkHelper.SubmitNextStageVote();
                }
                GUI.enabled = true;

                GUILayout.EndHorizontal();
                GUI.DragWindow();
            }

            private void BroadcastLocalBattleStart()
            {
                if (!IsMultiplayer || BattleSystem.instance == null || BattleSystem.instance.MainQueueData == null)
                {
                    return;
                }

                string key = BattleSystem.instance.MainQueueData.Key;
                if (key == lastBattleKey)
                {
                    return;
                }

                lastBattleKey = key;
                NetworkHelper.sendBattleStart(key, BattleSystem.instance.BossBattle, BattleSystem.instance.CurseBattle, "", "", false);
            }

            private void BroadcastStageState()
            {
                if (!IsLobbyOwner || StageSystem.instance == null || StageSystem.instance.StageData == null)
                {
                    return;
                }

                string key = StageSystem.instance.StageData.Key;
                if (key == lastStageKey)
                {
                    return;
                }

                lastStageKey = key;
                NetworkHelper.sendStageState(key, StageSystem.instance.PlayerPos);
            }

            private void BroadcastTurnActionNum()
            {
                if (!IsMultiplayer || BattleSystem.instance == null || BattleSystem.instance.AllyTeam == null)
                {
                    lastTurnActionNum = -1;
                    return;
                }

                int value = BattleSystem.instance.AllyTeam.TurnActionNum;
                if (value == lastTurnActionNum)
                {
                    return;
                }

                lastTurnActionNum = value;
                NetworkHelper.sendTurnActionNum(value);
            }

            private void BroadcastSharedResources()
            {
                if (!IsMultiplayer || PlayData.TSavedata == null)
                {
                    return;
                }

                if (PlayData.TSavedata._Soul != lastSoul)
                {
                    lastSoul = PlayData.TSavedata._Soul;
                    NetworkHelper.sendData(NetworkHelper.dataType.Soul);
                }
                if (PlayData.TSavedata._Gold != lastGold)
                {
                    lastGold = PlayData.TSavedata._Gold;
                    NetworkHelper.sendData(NetworkHelper.dataType.Gold);
                }
            }

            private void GateNextStageBehindVote()
            {
                if (!IsMultiplayer || StageSystem.instance == null)
                {
                    nextStageVoteAvailable = false;
                    return;
                }

                if (StageSystem.instance.CanNextStage)
                {
                    nextStageVoteAvailable = true;
                    StageSystem.instance.CanNextStage = false;
                }
            }
        }
    }
}
