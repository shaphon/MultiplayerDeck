using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>请求战斗开始时发送牌组。无负载。</summary>
    public class RequestForBattleStartDeckMessage : NetworkMessage
    {
        public override void Serialize(BinaryWriter writer) { }
        public override void Deserialize(BinaryReader reader) { }

        public override void Handle(RemotePlayer sender)
        {
            if (NetworkHelper.IsLobbyActive() && !NetworkHelper.IsLobbyOwner())
                BattleSyncManager.Instance.SendPersonalDeck();
        }
    }
}
