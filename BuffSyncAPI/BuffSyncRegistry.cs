using GameDataEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BuffSyncAPI
{
    public delegate void RemoteBuffAddHandler(string buffKey, string targetCharKey, int targetPosition, bool targetIsAlly, string userCharKey, int userPosition, bool userIsAlly, int stackNum, int lifetime, byte[] customData,BuffAddDelegate SafeBuffAdd);
    public delegate Buff BuffAddDelegate(BattleChar target, string key, BattleChar user, bool noEffect = false, int stackNum = 0, bool noStack = false, int lifeTime = 0, bool noLifeTime = false);
    public class BuffSyncConfig
    {
        public string BuffKey;
        public RemoteBuffAddHandler OnRemoteAdd;
        public Func<BattleChar, BattleChar, bool> ShouldSyncFilter;
        public Func<Buff, byte[]> SerializeCustomData;
        public Action<Buff, byte[]> ApplyCustomData;

        public BuffSyncConfig(string buffKey)
        {
            BuffKey = buffKey;
        }

        public void DefaultRemoteAdd(string buffKey, string targetCharKey, int targetPosition, bool targetIsAlly, string userCharKey, int userPosition, bool userIsAlly, int stackNum, int lifetime, byte[] customData,BuffAddDelegate SafeBuffAdd)
        {
            BattleChar target = BuffSyncRegistry.FindBattleChar(targetCharKey, targetPosition, targetIsAlly);
            BattleChar user = BuffSyncRegistry.FindBattleChar(userCharKey, userPosition, userIsAlly);
            if (target == null || user == null)
            {
                Debug.LogError($"[BuffSyncAPI] Failed to find characters for buff sync: Target({targetCharKey}, Pos {targetPosition}, Ally {targetIsAlly}), User({userCharKey}, Pos {userPosition}, Ally {userIsAlly})");
                return;
            }
            Buff newBuff = SafeBuffAdd(target, buffKey, user, false, stackNum, false, lifetime, false);
            if (newBuff != null && ApplyCustomData != null && customData != null)
            {
                ApplyCustomData(newBuff, customData);
            }
        }

        public BuffSyncConfig OnRemoteAddCallback(RemoteBuffAddHandler handler)
        {
            OnRemoteAdd = handler;
            return this;
        }

        public BuffSyncConfig WithFilter(Func<BattleChar, BattleChar, bool> filter)
        {
            ShouldSyncFilter = filter;
            return this;
        }

        public BuffSyncConfig WithCustomData(Func<Buff, byte[]> serializer, Action<Buff, byte[]> applier)
        {
            SerializeCustomData = serializer;
            ApplyCustomData = applier;
            return this;
        }
    }

    public static class BuffSyncRegistry
    {
        

       
        private static Dictionary<string, BuffSyncConfig> registeredBuffs = new Dictionary<string, BuffSyncConfig>();


        public static void RegisterBuffSync(BuffSyncConfig config)
        {
            if (string.IsNullOrEmpty(config.BuffKey))
            {
                Debug.LogError("[BuffSyncAPI] Cannot register buff with empty key");
                return;
            }

            if (registeredBuffs.ContainsKey(config.BuffKey))
            {
                Debug.LogWarning("[BuffSyncAPI] Buff already registered: " + config.BuffKey + ", overwriting");
            }

            registeredBuffs[config.BuffKey] = config;
            Debug.Log("[BuffSyncAPI] Registered buff sync: " + config.BuffKey);
        }

        public static void UnregisterBuffSync(string buffKey)
        {
            if (registeredBuffs.ContainsKey(buffKey))
            {
                registeredBuffs.Remove(buffKey);
                Debug.Log("[BuffSyncAPI] Unregistered buff sync: " + buffKey);
            }
        }

        public static bool IsBuffRegistered(string buffKey)
        {
            return registeredBuffs.ContainsKey(buffKey);
        }

        public static BuffSyncConfig GetConfig(string buffKey)
        {
            if (registeredBuffs.TryGetValue(buffKey, out var config))
            {
                return config;
            }
            return null;
        }

        public static bool ShouldSyncBuff(string buffKey, BattleChar user, BattleChar target)
        {
            if (!registeredBuffs.TryGetValue(buffKey, out var config))
            {
                return false;
            }

            if (config.ShouldSyncFilter != null && !config.ShouldSyncFilter(user, target))
            {
                return false;
            }

            return true;
        }

        

        public static BattleChar FindBattleChar(string charKey, int position, bool isAlly)
        {
            if (BattleSystem.instance == null || string.IsNullOrEmpty(charKey) || position < 0)
            {
                return null;
            }

            if (isAlly)
            {
                
                return FindBestAlly(charKey);
            }
            else
            {
                List<BattleChar> list = BattleSystem.instance.EnemyTeam.AliveChars.FindAll(enemy => enemy.Info.KeyData == charKey);
                if (list.IsValidIndex(position))
                {
                    return list[position];
                }
                return null;
            }
        }

        public static BattleChar FindBestAlly(string originalAlly)
        {
            List<BattleChar> allChars = BattleSystem.instance.AllyTeam.Chars;
            if (allChars == null || allChars.Count == 0)
            {
                return null;
            }

            BattleChar battleChar = allChars.FirstOrDefault(bc => bc.Info.KeyData == originalAlly);
            if (battleChar != null)
            {
                return battleChar;
            }

            if (originalAlly == "Lucy")
            {
                return BattleSystem.instance.AllyTeam.LucyAlly;
            }

            List<BattleChar> aliveChars = BattleSystem.instance.AllyTeam.AliveChars;
            if (!string.IsNullOrEmpty(originalAlly) && aliveChars != null && aliveChars.Count > 0)
            {
                GDECharacterData originalMasterData = new GDECharacterData(originalAlly);
                List<BattleChar> sameRole = aliveChars.FindAll(bc => bc.Info.GetData.Role.Key == originalMasterData.Role.Key);

                if (sameRole.Count > 0)
                {
                    return sameRole.Random();
                }
                else
                {
                    return aliveChars.Random();
                }
            }

            return allChars.Random();
        }
    }
}
