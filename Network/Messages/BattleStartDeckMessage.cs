using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>
    /// 战斗开始时的牌组同步消息。
    /// 客户端发送时 IsDto=false，携带 SkillList；房主发送合并后的牌组时 IsDto=true，携带 SkillDTOList。
    /// </summary>
    public class BattleStartDeckMessage : NetworkMessage
    {
        public bool IsDto;
        public List<Skill> Deck;
        public List<SkillNetworkDTO> DeckDto;

        public override void Serialize(BinaryWriter bw)
        {
            bw.Write(IsDto);
            if (IsDto)
                SkillSerializer.SkillDTOListSerialize(bw, DeckDto ?? new List<SkillNetworkDTO>());
            else
                SkillSerializer.SkillListSerialize(bw, Deck ?? new List<Skill>());
        }

        public override void Deserialize(BinaryReader br)
        {
            IsDto = br.ReadBoolean();
            if (IsDto)
                DeckDto = SkillSerializer.SkillDTOListDeserialize(br);
            else
                Deck = SkillSerializer.SkillListDeserialize(br);
        }

        public override void Handle(RemotePlayer sender)
        {
            Debug.Log("[DeckSync] Received BattleStartDeck. IsOwner=" + NetworkHelper.IsLobbyOwner());
            if (NetworkHelper.IsLobbyOwner())
            {
                BattleSyncManager.Instance.ReceiveDeckContribution(sender, DeckDto);
            }
            else
            {
                BattleSyncManager.Instance.ReceiveCombinedDeck(sender, Deck);
            }
        }
    }
}
