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
using ChronoArkMod.ModData;
namespace MultiplayerDeck
{
    public class MultiplayerDeck_ModDefinition : ModDefinition
    {
        private static readonly BattleSyncPassive BattleSync = new BattleSyncPassive();

        public override Type ModItemKeysType => typeof(ModItemKeys);

        public override List<object> BattleSystem_ModIReturn(Type type)
        {
            List<object> list = new List<object>();
            if (type == typeof(IP_BattleStart_Ones) || type == typeof(IP_Draw) || type == typeof(IP_SkillUseHand_Team))
            {
                list.Add(BattleSync);
            }
            return list;
        }
    }
}
