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
                writer.Write(0);
                writer.Write(-1);
                return;
            }

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

            writer.Write(dto.Seed);
            writer.Write(dto.MasterIndex);
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
            dto.Seed = reader.ReadInt32();
            dto.MasterIndex = reader.ReadInt32();
            return dto;
        }

        // 转 DTO
        public static SkillNetworkDTO SkillToDTO(Skill skill)
        {
            if (skill == null || skill.MySkill == null)
            {
                return null;
            }
            return new SkillNetworkDTO
            {
                SkillKey = string.IsNullOrEmpty(skill.MySkill.KeyID) ? skill.MySkill.Key : skill.MySkill.KeyID,
                MasterKey = skill.Master?.Info?.KeyData ?? "",
                Seed = skill.CharinfoSkilldata?.Seed ?? 0,
                MasterIndex = GetMasterIndex(skill.Master),
                ExtendedData = (skill.AllExtendeds ?? new List<Skill_Extended>())
                    .Where(se => se.Data != null && !se.isDataExtended)
                    .Select(se => (se.Data.Key, se.BattleExtended)).ToList()
            };
        }

        // 从 DTO 重建（需要在游戏上下文环境中）
        public static Skill CreateSkillFromDTO(SkillNetworkDTO dto)
        {
            return CreateSkillFromDTO(dto, -1);
        }

        public static Skill CreateSkillFromDTO(SkillNetworkDTO dto, int packetIndex)
        {
            if (BattleSystem.instance == null || dto == null)
            {
                return null;
            }

            BattleChar master = FindBestLocalMaster(dto, packetIndex);
            if (master == null)
            {
                Debug.LogWarning("[SkillSerializer] Cannot find master for skill: " + dto.SkillKey + ", master=" + dto.MasterKey);
                return null;
            }

            Skill skill = CreateFromLocalInitializedSkill(dto, master);
            if (skill == null)
            {
                if (string.IsNullOrEmpty(dto.SkillKey))
                {
                    Debug.LogWarning("[SkillSerializer] Empty skill key. master=" + dto.MasterKey + ", seed=" + dto.Seed);
                    return null;
                }

                skill = Skill.TempSkill(dto.SkillKey, master, master.MyTeam);
                //AddNetworkExtensions(skill, dto);
            }

            return skill;
        }

        private static int GetMasterIndex(BattleChar master)
        {
            List<BattleChar> chars = BattleSystem.instance?.AllyTeam?.Chars;
            if (master == null || chars == null)
            {
                return -1;
            }

            return chars.FindIndex(bc => bc == master || bc?.Info == master.Info);
        }

        private static Skill CreateFromLocalInitializedSkill(SkillNetworkDTO dto, BattleChar master)
        {
            Skill original = FindLocalInitializedSkill(dto, master);
            if (original == null)
            {
                return null;
            }

            Skill skill = original.CloneSkill(false, master, original.AllExtendeds);
            skill.CharinfoSkilldata.CopyData(original);
            return skill;
        }

        private static Skill FindLocalInitializedSkill(SkillNetworkDTO dto, BattleChar master)
        {
            if (BattleSystem.instance?.AllyTeam == null)
            {
                return null;
            }

            IEnumerable<Skill> candidates = BattleSystem.instance.AllyTeam.Skills_Deck
                .Concat(BattleSystem.instance.AllyTeam.Skills_UsedDeck)
                .Where(skill => skill != null && skill.Master != null && skill.Master.Info != null);

            if (dto.Seed != 0)
            {
                Skill bySeed = candidates.FirstOrDefault(skill =>
                    skill.CharinfoSkilldata != null &&
                    skill.CharinfoSkilldata.Seed == dto.Seed &&
                    IsSameSkillKey(skill, dto.SkillKey));

                if (bySeed != null)
                {
                    return bySeed;
                }
            }

            return candidates.FirstOrDefault(skill =>
                skill.Master.Info.KeyData == master.Info.KeyData &&
                IsSameSkillKey(skill, dto.SkillKey));
        }

        private static bool IsSameSkillKey(Skill skill, string key)
        {
            if (skill == null || skill.MySkill == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            return skill.MySkill.Key == key || skill.MySkill.KeyID == key;
        }

        private static void AddNetworkExtensions(Skill skill, SkillNetworkDTO dto)
        {
            if (skill == null || dto?.ExtendedData == null)
            {
                return;
            }

            foreach ((string key, bool battle) seData in dto.ExtendedData)
            {
                if (string.IsNullOrEmpty(seData.key) ||
                    skill.AllExtendeds.Any(se => se.Data != null && se.Data.Key == seData.key))
                {
                    continue;
                }

                if (seData.battle)
                {
                    skill.ExtendedAdd_Battle(seData.key);
                }
                else
                {
                    skill.ExtendedAdd(seData.key);
                }
            }
        }

        public static BattleChar FindBestLocalMaster(string originalMaster)
        {
            return FindBestLocalMaster(new SkillNetworkDTO { MasterKey = originalMaster, MasterIndex = -1 }, -1);
        }

        public static BattleChar FindBestLocalMaster(SkillNetworkDTO dto, int packetIndex)
        {
            List<BattleChar> allChars = BattleSystem.instance?.AllyTeam?.Chars;
            if (allChars == null || allChars.Count == 0)
            {
                return null;
            }
  
            string originalMaster = dto?.MasterKey ?? "";

            BattleChar battleChar = allChars.FirstOrDefault(bc => bc?.Info != null && bc.Info.KeyData == originalMaster);
            if (battleChar != null)
            {
                return battleChar;
            }

            /*int mappedIndex = dto?.MasterIndex ?? -1;
            if (mappedIndex >= 0)
            {
                return allChars[mappedIndex % allChars.Count];
            }*/

            List<BattleChar> aliveChars = BattleSystem.instance.AllyTeam.AliveChars;
            if (!string.IsNullOrEmpty(originalMaster) && aliveChars != null && aliveChars.Count > 0)
            {
                GDECharacterData originalMasterData = new GDECharacterData(originalMaster);
                List<BattleChar> sameRole = aliveChars
                    .Where(bc => bc?.Info?.GetData?.Role != null && bc.Info.GetData.Role.Key == originalMasterData.Role.Key)
                    .ToList();

                if (sameRole.Count > 0)
                {
                    return sameRole.Random();
                }
            }

            /*if (packetIndex >= 0)
            {
                return allChars[packetIndex % allChars.Count];
            }*/

            if (aliveChars != null && aliveChars.Count > 0)
            {
                int keyHash = dto?.SkillKey == null ? 0 : Math.Abs(dto.SkillKey.GetHashCode());
                return aliveChars[keyHash % aliveChars.Count];
            }

            return allChars[0];
        }
    }

    [Serializable]
    public class SkillNetworkDTO
    {
        // 只传输必要的最小数据
        public string SkillKey;              // GDESkillData.Key
        public string MasterKey;              // 角色唯一标识
        public int Seed;                       // CharInfoSkillData.Seed，用于优先匹配本地初始化出的原牌
        public int MasterIndex;                // 原队伍中的角色槽位；远端角色不存在本地时按槽位映射

        // 扩展数据用键值对传输
        public List<(string, bool)> ExtendedData;
    }
}
