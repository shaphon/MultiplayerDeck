using DarkTonic.MasterAudio;
using GameDataEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TileTypes;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;

namespace MultiplayerDeck
{
    public class StageSyncManager
    {
        private static StageSyncManager _instance;
        public static StageSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StageSyncManager();
                }
                return _instance;
            }
        }

        public bool bossClear;
        public bool forceNextStage;
        private HashSet<ulong> playersNextStageComplete = new HashSet<ulong>();
        private string lastBattleKey;

        public void Initialize()
        {
            bossClear = false;
            playersNextStageComplete.Clear();
            VoteManager.Instance.AbortAllVotes();
        }

        public void Tick()
        {
            if (StageSystem.instance == null)
            {
                return;
            }
            //BroadcastLocalBattleStart();
        }

        private void BroadcastLocalBattleStart()
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer || BattleSystem.instance == null || BattleSystem.instance.MainQueueData == null)
            {
                return;
            }

            string key = BattleSystem.instance.MainQueueData.Key;
            if (key == lastBattleKey)
            {
                return;
            }

            lastBattleKey = key;
            NetworkHelper.SendBattleStart(key, BattleSystem.instance.BossBattle, BattleSystem.instance.CurseBattle, "", "", false);
        }

        public void StartBattleFromNetwork(string QueueData, bool NormalBattle, bool Cursed, string RewardKey, string Preset, bool NoGameover)
        {
            if (BattleSystem.instance != null || FieldSystem.instance == null || StageSystem.instance == null)
            {
                return;
            }

            FieldSystem.DelayInput(BattleStart());
            IEnumerator BattleStart()
            {
                FieldSystem.instance.BattleStart(new GDEEnemyQueueData(QueueData), StageSystem.instance.StageData.BattleMap.Key, NormalBattle, Cursed, RewardKey, Preset, NoGameover);
                yield break;
            }
        }

        public void GotoNextStage(bool crimson = false, bool azar = false)
        {
            forceNextStage = true;
            VoteManager.Instance.syncing = true;
            Initialize();

            if (crimson)
            {
                UnityEngine.Object.FindObjectOfType<RedWall>()?.Enter();
                return;
            }

            if (azar)
            {
                StageSystem.instance?.Map?.MainCamp?.NextMap_Master();
                return;
            }

            var camp = StageSystem.instance?.Map?.MainCamp;
            if (camp != null)
            {
                camp.NextMap();
                return;
            }

            var miniBossObject = UnityEngine.Object.FindObjectOfType<MiniBossObject>();
            if (miniBossObject != null)
            {
                miniBossObject.GoCamp();
                return;
            }

            var stage1Events = UnityEngine.Object.FindObjectOfType<Stage1Events>();
            if (stage1Events != null)
            {
                FieldSystem.instance.ReturnArkButton();
                return;
            }

            var door = UnityEngine.Object.FindObjectOfType<Door>();
            if (door != null)
            {
                door.Trigger();
                return;
            }
        }

        public void PlayerNextStageComplete(RemotePlayer playerInfo)
        {
            playersNextStageComplete.Add(playerInfo.steamUser.m_SteamID);

            if (playersNextStageComplete.Count == TogetherManager.players.Count - 1)
            {
                NetworkHelper.SendData(NetDataType.NextStageComplete);
                VoteManager.Instance.syncing = false;
            }
        }

        public void MonsterClear(Vector2 Pos)
        {
            if (StageSystem.instance.Map == null 
                || StageSystem.instance.Map.MapObject == null
                || Pos.x >= StageSystem.instance.Map.MapObject.GetLength(0)
                || Pos.y >= StageSystem.instance.Map.MapObject.GetLength(1)
                || StageSystem.instance.Map.EventTileList == null)
            {
                return;
            }
            StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y].Info.Type = new Road();
            StageSystem.instance.Map.EventTileList.Remove(StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y]);
            StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y].Info.Cursed = false;
            if (StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y].MonsterEffect != null)
            {
                StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y].MonsterEffect.GetComponentInChildren<ParticleSystem>().emissionRate = 0f;
                Object.Destroy(StageSystem.instance.Map.MapObject[(int)Pos.x, (int)Pos.y].MonsterEffect);
            }
            StageSystem.instance.UpdateMove();
        }

        public void BossClear()
        {
            var m = UnityEngine.Object.FindObjectOfType<MiniBossObject>();
            if (m != null)
            {
                Debug.Log("[BossClear] MiniBossClear Complete");
                m.BossClear = true;
                m.BossFogObject.SetActive(false);
                return;
            }
            var s = UnityEngine.Object.FindObjectOfType<Stage1Events>();
            if (s != null)
            {
                Debug.Log("[BossClear] BossClear Complete");
                s.BossClear = true;
                s.BossFogObject.SetActive(false);
                return;
            }

            Debug.Log("[BossClear] Boss Object Not Found");
        }
    }
}
