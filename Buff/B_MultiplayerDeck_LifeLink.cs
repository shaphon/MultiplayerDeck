using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using GameDataEditor;
using I2.Loc;
using DarkTonic.MasterAudio;
using ChronoArkMod;
using ChronoArkMod.Plugin;
using ChronoArkMod.Template;
using Debug = UnityEngine.Debug;
namespace MultiplayerDeck
{
	/// <summary>
	/// 生命链接
	/// </summary>
    public class B_MultiplayerDeck_LifeLink : Buff, IP_HPChange1
    {
        public override void Init()
        {
            base.Init();
            this.PlusPerStat.MaxHP = (TogetherManager.players.Count - 1) * 100 + 25;
        }

        public override void BuffOneAwake()
        {
            base.BuffOneAwake();
            this.PlusPerStat.MaxHP = (TogetherManager.players.Count - 1) * 100 + 25;
            this.BChar.HP = this.BChar.GetStat.maxhp;
        }

        public void HPChange1(BattleChar Char, bool Healed, int PreHPNum, int NewHPNum)
        {
            if (BattleSyncManager.Instance.enemyHpSyncing)
            {
                return;
            }

            if (Char == this.BChar)
            {
                string key = this.BChar.Info.KeyData;
                int position = BattleSystem.instance.EnemyTeam.AliveChars.FindAll(enemy => enemy.Info.KeyData == key).FindIndex(enemy => enemy == this.BChar);
                NetworkHelper.SendEnemyHpChange(key, position, NewHPNum);
            }
        }
    }
}