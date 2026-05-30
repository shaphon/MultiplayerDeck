using BuffSyncAPI;
using ChronoArkMod.Plugin;
using GameDataEditor;
using HarmonyLib;
using MultiplayerDeck.Network;
using System;
using UnityEngine;

namespace MultiplayerDeck
{
    public class BuffSyncManager
    {

        public static void RegisterChronoArkBuffs()
        {
            BuffSyncRegistry.RegisterBuffSync(new BuffSyncConfig(GDEItemKeys.Buff_B_Hein_T_2));
        }


        public static void HandleRemoteBuffAdd(string buffKey, string targetCharKey, int targetPosition, bool targetIsAlly, string userCharKey, int userPosition, bool userIsAlly, int stackNum, int lifetime, byte[] customData)
        {
            if (BattleSystem.instance == null)
            {
                Debug.LogWarning("[BuffSyncAPI] Cannot handle remote buff add: BattleSystem is null");
                return;
            }

            BattleChar target = BuffSyncRegistry.FindBattleChar(targetCharKey, targetPosition, targetIsAlly);
            BattleChar user = BuffSyncRegistry.FindBattleChar(userCharKey, userPosition, userIsAlly);

            if (target == null)
            {
                Debug.LogWarning("[BuffSyncAPI] Cannot find target char: " + targetCharKey + ", position=" + targetPosition + ", isAlly=" + targetIsAlly);
                return;
            }
            if(BuffSyncRegistry.GetConfig(buffKey) is BuffSyncConfig config)
            {
                if (config.OnRemoteAdd != null)
                {
                    config.OnRemoteAdd(buffKey, targetCharKey, targetPosition, targetIsAlly, userCharKey, userPosition, userIsAlly, stackNum, lifetime, customData,BuffSyncPatch.AddBuffDirectly);
                }
                else
                {
                    config.DefaultRemoteAdd(buffKey, targetCharKey, targetPosition, targetIsAlly, userCharKey, userPosition, userIsAlly, stackNum, lifetime, customData,BuffSyncPatch.AddBuffDirectly);
                }
            }
           
        }

        [HarmonyPatch(typeof(BattleChar), "BuffAdd", new Type[] { typeof(string), typeof(BattleChar), typeof(bool), typeof(int), typeof(bool), typeof(int), typeof(bool) })]
        public static class BuffSyncPatch
        {
            [HarmonyReversePatch]
            public static Buff OriginalBuffAdd(BattleChar __instance, string key, BattleChar UseState, bool noEffect, int stackNum, bool noStack, int lifeTime, bool noLifeTime)
            {
                throw new NotImplementedException("Harmony reverse patch not applied");
            }

            public static Buff AddBuffDirectly(BattleChar target, string key, BattleChar user, bool noEffect = false, int stackNum = 0, bool noStack = false, int lifeTime = 0, bool noLifeTime = false)
            {
                return OriginalBuffAdd(target, key, user, noEffect, stackNum, noStack, lifeTime, noLifeTime);
            }

            [HarmonyPostfix]
            public static void Postfix(BattleChar __instance, string key, BattleChar UseState, ref Buff __result)
            {
                if (!MultiplayerDeck_Plugin.IsMultiplayer || __result == null)
                {
                    return;
                }

                if (!BuffSyncRegistry.ShouldSyncBuff(key, UseState, __instance))
                {
                    return;
                }

                BuffSyncConfig config = BuffSyncRegistry.GetConfig(key);
                if (config == null)
                {
                    return;
                }

                byte[] customData = null;
                if (config.SerializeCustomData != null)
                {
                    try
                    {
                        customData = config.SerializeCustomData(__result);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[BuffSync] Failed to serialize custom data for buff: " + key + ", error: " + e.Message);
                    }
                }

                string targetKey = __instance.Info.KeyData;
                bool targetIsAlly = __instance is BattleAlly;
                int targetPosition;
                if (targetIsAlly)
                {
                    targetPosition = BattleSystem.instance.AllyList.FindIndex(c => c == __instance);
                }
                else
                {
                    string keyData = __instance.Info.KeyData;
                    var sameKeyEnemies = BattleSystem.instance.EnemyTeam.AliveChars.FindAll(e => e.Info.KeyData == keyData);
                    targetPosition = sameKeyEnemies.FindIndex(e => e == __instance);
                }

                string userKey = "";
                int userPosition = -1;
                bool userIsAlly = false;
                if (UseState != null)
                {
                    userKey = UseState.Info.KeyData;
                    userIsAlly = UseState is BattleAlly;
                    if (userIsAlly)
                    {
                        userPosition = BattleSystem.instance.AllyList.FindIndex(c => c == UseState);
                    }
                    else
                    {
                        string userKeyData = UseState.Info.KeyData;
                        var sameKeyUserEnemies = BattleSystem.instance.EnemyTeam.AliveChars.FindAll(e => e.Info.KeyData == userKeyData);
                        userPosition = sameKeyUserEnemies.FindIndex(e => e == UseState);
                    }
                }

                MessageSerializer.SendBuffAdd(key, targetKey, targetPosition, targetIsAlly, userKey, userPosition, userIsAlly, __result.StackNum, __result.LifeTime, customData);
            }


        }

    }

}
