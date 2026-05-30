using System.IO;

namespace MultiplayerDeck.Network.Messages
{
    /// <summary>版本握手消息，无负载。</summary>
    public class VersionMessage : NetworkMessage
    {
        public override void Serialize(BinaryWriter writer) { }
        public override void Deserialize(BinaryReader reader) { }
    }
}
