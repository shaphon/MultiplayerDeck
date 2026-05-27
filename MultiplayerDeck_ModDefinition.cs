using ChronoArkMod;
using ChronoArkMod.ModData;
using ChronoArkMod.Plugin;
using ChronoArkMod.Template;
using DarkTonic.MasterAudio;
using GameDataEditor;
using I2.Loc;
using Spine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
namespace MultiplayerDeck
{
    public class MultiplayerDeck_ModDefinition : ModDefinition
    {
        private static readonly BattleSyncPassive BattleSync = new BattleSyncPassive();

        public override Type ModItemKeysType => typeof(ModItemKeys);

        public override List<object> BattleSystem_ModIReturn(Type type)
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer)
            {
                return base.BattleSystem_ModIReturn();
            }

            List<object> list = new List<object>();
            if (type == typeof(IP_EnemyAwake) || type == typeof(IP_PlayerTurn) || type == typeof(IP_ParticleOut_After_Global))
            {
                list.Add(BattleSync);
            }
            return list;
        }
    }

    public class BattleSyncPassive : IP_EnemyAwake, IP_PlayerTurn, IP_ParticleOut_After_Global
    {
        public void EnemyAwake(BattleChar Enemy)
        {
            Enemy.BuffAdd(ModItemKeys.Buff_B_MultiplayerDeck_LifeLink, Enemy, false, 0, false, -1, true);
        }

        public void Turn()
        {
            VoteManager.Instance.StartVote(VoteManager.VoteTheme.TurnEnd);
        }

        public IEnumerator ParticleOut_After_Global(Skill SkillD, List<BattleChar> Targets)
        {
            NetworkHelper.SendSkillPlayed(SkillD.MySkill.Name);
            yield break;
        }
    }
}
