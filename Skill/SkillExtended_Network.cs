using ChronoArkMod;
using GameDataEditor;
using MultiplayerDeck.Network;
using MultiplayerDeck.Network.Messages;
using NLog.Targets;
using Spine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Debug = UnityEngine.Debug;

namespace MultiplayerDeck
{
    public class SkillExtended_Network : Skill_Extended
    {
        private List<int> CustomNumbers = new List<int>();

        public void AddCustomNumbers(bool reset, params int[] numbers)
        {
            if (reset)
            {
                CustomNumbers.Clear();
            }
            CustomNumbers.AddRange(numbers);
        }

        public virtual void LocalSkillEffect(List<BattleChar> Targets)
        {

        }

        public virtual void RemoteSkillEffect(List<BattleChar> Targets)
        {

        }

        public override void SkillUseSingle(Skill SkillD, List<BattleChar> Targets)
        {
            base.SkillUseSingle(SkillD, Targets);
            LocalSkillEffect(Targets);
            SendSkillEffect(SkillD, Targets);
        }

        protected void SendSkillEffect(Skill SkillD, List<BattleChar> Targets)
        {
            var msg = new NetworkSkillEffectMessage
            {
                SkillKey = string.IsNullOrEmpty(SkillD.MySkill.KeyID) ? SkillD.MySkill.Key : SkillD.MySkill.KeyID,
            };

            if (Targets != null)
            {
                foreach (BattleChar target in Targets)
                {
                    TargetInformation info = TargetHelper.GetCertainTargetInformation(target);
                    msg.Targets.Add(new NetworkSkillEffectMessage.TargetInfo(info.isEnemy, info.key, info.position));
                }
            }

            foreach (int number in CustomNumbers)
                msg.CustomNumbers.Add(number);

            MessageDispatcher.Send(msg);
        }

        /// <summary>
        /// 应用远程技能效果（从已反序列化的数据）。
        /// </summary>
        public static bool ApplySkillEffect(string skillKey, List<(bool isEnemy, string key, int position)> targetInfos, List<int> customNumbers)
        {
            Type type = ModManager.GetType(skillKey);
            if (type == null)
            {
                type = ModManager.GetType(skillKey.Split('.').Last());
            }
            if (type == null || !typeof(SkillExtended_Network).IsAssignableFrom(type))
            {
                Debug.LogError("[NetworkSkill] Unknown network skill: " + skillKey);
                return false;
            }

            SkillExtended_Network extended = (SkillExtended_Network)Activator.CreateInstance(type);

            List<BattleChar> targets = new List<BattleChar>();
            if (targetInfos != null)
            {
                foreach (var info in targetInfos)
                {
                    BattleChar target = TargetHelper.FindCertainTarget(info.isEnemy, info.key, info.position);
                    if (target != null)
                        targets.Add(target);
                }
            }

            if (customNumbers != null)
                extended.CustomNumbers.AddRange(customNumbers);

            extended.RemoteSkillEffect(targets);
            return true;
        }
    }

    public class TargetInformation
    {
        public bool isEnemy;
        public string key;
        public int position;
    }

    public static class TargetHelper
    {
        public static TargetInformation GetCertainTargetInformation(BattleChar battleChar)
        {
            TargetInformation targetInformation = new TargetInformation();
            if (battleChar.Info.Ally)
            {
                targetInformation.isEnemy = false;
                targetInformation.key = battleChar.Info.KeyData ?? "";
                targetInformation.position = BattleSystem.instance?.AllyTeam?.Chars?.IndexOf(battleChar) ?? 0;
            }
            else
            {
                targetInformation.isEnemy = true;
                targetInformation.key = battleChar.Info.KeyData ?? "";
                targetInformation.position = BattleSystem.instance?.EnemyTeam?.AliveChars?.FindAll(enemy => enemy.Info.KeyData == targetInformation.key).IndexOf(battleChar) ?? 0;
            }
            return targetInformation;
        }

        public static BattleChar FindCertainTarget(TargetInformation targetInformation)
        {
            return FindCertainTarget(targetInformation.isEnemy, targetInformation.key, targetInformation.position);
        }

        public static BattleChar FindCertainTarget(bool isEnemy, string key, int position)
        {
            if (isEnemy)
            {
                List<BattleChar> list = BattleSystem.instance?.EnemyTeam?.AliveChars?.FindAll(enemy => enemy.Info.KeyData == key);
                if (list == null)
                {
                    return null;
                }

                if (list.IsValidIndex(position))
                {
                    return list[position];
                }
                else
                {
                    return list.FirstOrDefault();
                }
            }
            else
            {
                List<BattleChar> chars = BattleSystem.instance?.AllyTeam?.Chars;
                if (chars != null && chars.IsValidIndex(position) && !chars[position].IsDead && !chars[position].GetStat.Vanish)
                {
                    return chars[position];
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
