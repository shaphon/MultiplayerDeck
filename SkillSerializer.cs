using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerDeck
{
    public static class SkillSerializer
    {
        // 转 DTO
        public static SkillNetworkDTO ToDTO(Skill skill)
        {
            return new SkillNetworkDTO
            {
                SkillKey = skill.MySkill?.KeyID,
                MasterKey = skill.Master?.Info.KeyData,
                ExtendedData = skill.AllExtendeds
                    .Where(se => se.Data != null && !se.isDataExtended)
                    .Select(se => (se.Data.Key, se.BattleExtended)).ToList()
            };
        }

        // 从 DTO 重建（需要在游戏上下文环境中）
        public static Skill FromDTO(SkillNetworkDTO dto, BattleChar master, BattleTeam team)
        {
            Skill skill = Skill.TempSkill(dto.SkillKey, master, team);
            foreach ((string key, bool battle) seData in dto.ExtendedData)
            {
                if (seData.battle)
                {
                    skill.ExtendedAdd_Battle(seData.key);
                }
                else
                {
                    skill.ExtendedAdd(seData.key);
                }
            }
            return skill;
        }
    }

    [Serializable]
    public class SkillNetworkDTO
    {
        // 只传输必要的最小数据
        public string SkillKey;              // GDESkillData.Key
        public string MasterKey;              // 角色唯一标识

        // 扩展数据用键值对传输
        public List<(string, bool)> ExtendedData;
    }
}
