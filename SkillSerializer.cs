using GameDataEditor;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerDeck
{
    public static class SkillSerializer
    {
        public static void SkillListSerialize(BinaryWriter writer, List<Skill> list)
        {
            writer.Write(list.Count);
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
                list.Add(CreateSkillFromDTO(ReadDTO(reader)));
            }
            return list;
        }

        public static void SkillDTOListSerialize(BinaryWriter writer, List<SkillNetworkDTO> list)
        {
            writer.Write(list.Count);
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
            writer.Write(dto.SkillKey ?? "");
            writer.Write(dto.MasterKey ?? "");
            writer.Write(dto.ExtendedData?.Count ?? 0);

            if (dto.ExtendedData != null)
            {
                foreach ((string key, bool battle) seData in dto.ExtendedData)
                {
                    writer.Write(seData.key ?? "");
                    writer.Write(seData.battle);
                }
            }
        }

        public static SkillNetworkDTO ReadDTO(BinaryReader reader)
        {
            var dto = new SkillNetworkDTO
            {
                SkillKey = reader.ReadString(),
                MasterKey = reader.ReadString(),
            };

            int seCount = reader.ReadInt32();
            dto.ExtendedData = new List<(string, bool)>();
            for (int i = 0; i < seCount; i++)
            {
                dto.ExtendedData.Add((reader.ReadString(), reader.ReadBoolean()));
            }
            return dto;
        }

        // 转 DTO
        public static SkillNetworkDTO SkillToDTO(Skill skill)
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
        public static Skill CreateSkillFromDTO(SkillNetworkDTO dto)
        {
            if (BattleSystem.instance == null)
            {
                return null;
            }

            BattleChar master = FindBestLocalMaster(dto.MasterKey);
            Skill skill = Skill.TempSkill(dto.SkillKey, master, master.MyTeam);
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

        public static BattleChar FindBestLocalMaster(string originalMaster)
        {
            List<BattleChar> allChars = BattleSystem.instance.AllyTeam.Chars;
            List<BattleChar> aliveChars = BattleSystem.instance.AllyTeam.AliveChars;
            BattleChar battleChar = allChars.FirstOrDefault(bc => bc.Info.KeyData == originalMaster);
            if (battleChar != null)
            {
                return battleChar;
            }
            GDECharacterData originalMasterData = new GDECharacterData(originalMaster);
            battleChar = aliveChars.FindAll(bc => bc.Info.GetData.Role.Key == originalMasterData.Role.Key).Random();
            if (battleChar != null)
            {
                return battleChar;
            }
            return aliveChars.Random();
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
