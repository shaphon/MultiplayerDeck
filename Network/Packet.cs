namespace MultiplayerDeck.Network
{
	public class Packet
	{
		private byte[] data;

		private RemotePlayer player;

		public Packet(RemotePlayer player, byte[] data)
		{
			this.data = data;
			this.player = player;
		}

		public Packet()
		{
			Clear();
		}

		public void Clear()
		{
			data = null;
			player = null;
		}

		public void Set(RemotePlayer player, byte[] data)
		{
			this.data = data;
			this.player = player;
		}

		public bool HasPacket()
		{
			if (data == null || player == null)
			{
				return false;
			}
			return true;
		}

		public RemotePlayer GetPlayer()
		{
			return player;
		}

		public byte[] GetData()
		{
			return data;
		}
	}
}
