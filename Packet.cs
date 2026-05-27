namespace MultiplayerDeck
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
			clear();
		}

		public void clear()
		{
			data = null;
			player = null;
		}

		public void set(RemotePlayer player, byte[] data)
		{
			this.data = data;
			this.player = player;
		}

		public bool hasPacket()
		{
			if (data == null || player == null)
			{
				return false;
			}
			return true;
		}

		public RemotePlayer getplayer()
		{
			return player;
		}

		public byte[] getdata()
		{
			return data;
		}
	}
}
