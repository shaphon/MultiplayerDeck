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
        private string lastBattleKey;

        public void Initialize()
        {
            bossClear = false;
            VoteManager.Instance.AbortCurrentVote();
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

            FieldSystem.instance.BattleStart(new GDEEnemyQueueData(QueueData),
                StageSystem.instance.StageData.BattleMap.Key,
                NormalBattle,
                Cursed,
                RewardKey,
                Preset,
                NoGameover);
        }

        public void GotoNextStage()
        {
            Initialize();

            if (StageSystem.instance?.Map?.MainCamp != null)
            {
                FieldSystem.DelayInput(Camp._NextMap());
                return;
            }

            if (UnityEngine.Object.FindObjectOfType<MiniBossObject>() != null)
            {
                FieldSystem.instance.StartCoroutine(_Camp());
                IEnumerator _Camp()
                {
                    yield return UIManager.inst.FadeBlack_Out(1f);
                    FieldSystem.instance.NextStage();
                    yield return UIManager.inst.FadeBlack_In(1f);
                }
                return;
            }

            if (UnityEngine.Object.FindObjectOfType<Stage1Events>() != null)
            {
                Debug.Log(string.Format("Start ReturnArk, stage key: {0}[{1}], now date: {2}", PlayData.TSavedata.NowStageMapKey, PlayData.TSavedata.StageNum, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                StageSystem.instance.CanNextStage = false;
                FieldSystem.instance.BackButtonAni.SetBool("On", false);
                FieldSystem.DelayInput(FieldSystem.instance._ReturnArkbutton(false));
                return;
            }

            if (StageSystem.instance == null || StageSystem.instance.Map == null)
            {
                FieldSystem.instance.StartCoroutine(StageEnter());
                IEnumerator StageEnter()
                {
                    foreach (SoundGroupVariation soundGroupVariation in MasterAudio.GetAllPlayingVariationsInBus("Ambi"))
                    {
                        soundGroupVariation.FadeOutNow();
                    }
                    foreach (SoundGroupVariation soundGroupVariation2 in MasterAudio.GetAllPlayingVariationsInBus("BGM"))
                    {
                        soundGroupVariation2.FadeOutNow();
                    }
                    MasterAudio.FadeOutAllOfSound("bangjoo_side_loop", 4f);
                    MasterAudio.FadeOutAllOfSound("bangjoo_side_ambience", 4f);
                    yield return FieldSystem.instance.StartCoroutine(UIManager.inst.FadeSquare_Out());
                    yield return new WaitForSeconds(1f);
                    if (SaveManager.NowData.GameOptions.CasualMode && SaveManager.savemanager.TempSave != null && SaveManager.savemanager.TempSave.Party.Count != 0)
                    {
                        SaveManager.savemanager.OneSaveLoad();
                        FieldSystem.LoadOneSaveMap();
                        yield return new WaitForSeconds(3f);
                    }
                    else
                    {
                        FieldSystem.instance.StageStart("");
                        yield return new WaitForSeconds(3f);
                    }
                }
                return;
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
                m.BossClear = true;
                return;
            }
            var s = UnityEngine.Object.FindObjectOfType<Stage1Events>();
            if (s != null)
            {
                s.BossClear = true;
                return;
            }
        }
    }
}
