namespace MultiplayerDeck
{
	public class GiantNet : Buff, IP_HPChange
	{
		public override void Init()
		{
			base.Init();
			this.PlusPerStat.MaxHP = (TogetherManager.players.Count - 1) * 100 + 25;
		}

		public void HPChange(BattleChar Char, bool Healed)
		{
			if (Char is BattleEnemy && ((BattleEnemy)Char).Boss)
			{
				NetworkHelper.sendData(NetworkHelper.dataType.BossBattleHP);
			}
		}
	}
}
