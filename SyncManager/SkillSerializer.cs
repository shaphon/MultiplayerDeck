using GameDataEditor;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace MultiplayerDeck
{
    public static class SkillSerializer
    {
        public static void SkillListSerialize(BinaryWriter writer, List<Skill> list)
        {
            writer.Write(list?.Count ?? 0);
            if (list == null)
            {
                return;
            }
            foreach (Skill skill in list)
            {
                WriteDTO(writer, SkillToDTO(skill));
            }
        }

        public static List<Skill> SkillListDeserialize(BinaryReader reader)
        {
            List<Skill> list = new List<Skill>();
            int skillCount = reader.ReadInt32();
            for (int i = 0; i < skillCount; i++)
            {
                Skill skill = CreateSkillFromDTO(ReadDTO(reader));
                if (skill != null)
                {
                    list.Add(skill);
                }
            }
            return list;
        }

        public static void SkillDTOListSerialize(BinaryWriter writer, List<SkillNetworkDTO> list)
        {
            writer.Write(list?.Count ?? 0);
            if (list == null)
            {
                return;
            }
            foreach (SkillNetworkDTO skillDTO in list)
            {
                WriteDTO(writer, skillDTO);
            }
        }

        public static List<SkillNetworkDTO> SkillDTOListDeserialize(BinaryReader reader)
        {
            List<SkillNetworkDTO> list = new List<SkillNetworkDTO>();
            int skillCount = reader.ReadInt32();
            for (int i = 0; i < skillCount; i++)
            {
                list.Add(ReadDTO(reader));
            }
            return list;
        }

        public static void SkillSerialize(BinaryWriter writer, Skill skill)
        {
            WriteDTO(writer, SkillToDTO(skill));
        }

        public static Skill SkillDeserialize(BinaryReader reader)
        {
            return CreateSkillFromDTO(ReadDTO(reader));
        }

        public static void WriteDTO(BinaryWriter writer, SkillNetworkDTO dto)
        {
            if (dto == null)
            {
                writer.Write("");
                writer.Write("");
                writer.Write(0);
                return;
            }

            writer.Write(dto.SkillKey ?? "");
            writer.Write(dto.MasterKey ?? "");
            writer.Write(dto.Seed);
        }

        public static SkillNetworkDTO ReadDTO(BinaryReader reader)
        {
            return new SkillNetworkDTO
            {
                SkillKey = reader.ReadString(),
                MasterKey = reader.ReadString(),
                Seed = reader.ReadInt32()
            };
        }

        // 转 DTO
        public static SkillNetworkDTO SkillToDTO(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return null;
            }
            if (skill.Master.IsLucy)
            {
                return new SkillNetworkDTO
                {
                    SkillKey = string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID,
                    MasterKey = "Lucy",
                    Seed = skill.CharinfoSkilldata?.Seed ?? 0
                };
            }
            else
            {
                return new SkillNetworkDTO
                {
                    SkillKey = string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID,
                    MasterKey = skill.Master?.Info?.KeyData ?? "",
                    Seed = skill.CharinfoSkilldata?.Seed ?? 0
                };
            }
        }

        // 从 DTO 重建（需要在游戏上下文环境中）
        public static Skill CreateSkillFromDTO(SkillNetworkDTO dto)
        {
            if (BattleSystem.instance == null || BattleSystem.instance.AllyTeam == null || dto == null)
            {
                return null;
            }

            BattleChar master = FindBestLocalMaster(dto.MasterKey);
            if (master == null)
            {
                Debug.LogWarning("[SkillSerializer] Cannot find master for skill: " + dto.SkillKey + ", master=" + dto.MasterKey);
                return null;
            }

            Skill skill = CreateFromLocalInitializedSkill(dto);
            if (skill == null)
            {
                if (string.IsNullOrEmpty(dto.SkillKey))
                {
                    Debug.LogWarning("[SkillSerializer] Empty skill key. seed=" + dto.Seed);
                    return null;
                }

                skill = Skill.TempSkill(dto.SkillKey, master, master.MyTeam);
            }

            return skill;
        }

        private static Skill CreateFromLocalInitializedSkill(SkillNetworkDTO dto)
        {
            Skill original = FindLocalInitializedSkill(dto);
            if (original == null)
            {
                return null;
            }

            Skill skill = original.CloneSkill(true);
            return skill;
        }

        private static Skill FindLocalInitializedSkill(SkillNetworkDTO dto)
        {
            if (dto.Seed == 0)
            {
                return null;
            }

            IEnumerable<Skill> candidates = BattleSystem.instance.AllyTeam.Skills_Deck
                .Concat(BattleSystem.instance.AllyTeam.Skills_UsedDeck);

            return candidates.FirstOrDefault(skill =>
                skill.CharinfoSkilldata != null &&
                skill.CharinfoSkilldata.Seed == dto.Seed);
        }

        public static BattleChar FindBestLocalMaster(string originalMaster)
        {
            List<BattleChar> allChars = BattleSystem.instance.AllyTeam.Chars;
            if (allChars == null || allChars.Count == 0)
            {
                return null;
            }
  
            BattleChar battleChar = allChars.FirstOrDefault(bc => bc.Info.KeyData == originalMaster);
            if (battleChar != null)
            {
                return battleChar;
            }

            if (originalMaster == "Lucy")
            {
                return BattleSystem.instance.AllyTeam.LucyAlly;
            }

            List<BattleChar> aliveChars = BattleSystem.instance.AllyTeam.AliveChars;
            if (!string.IsNullOrEmpty(originalMaster) && aliveChars != null && aliveChars.Count > 0)
            {
                GDECharacterData originalMasterData = new GDECharacterData(originalMaster);
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

    [Serializable]
    public class SkillNetworkDTO
    {
        // 只传输必要的最小数据
        public string SkillKey;              // GDESkillData.Key
        public string MasterKey;              // 角色唯一标识
        public int Seed;                       // CharInfoSkillData.Seed，用于优先匹配本地初始化出的原牌
    }
}
