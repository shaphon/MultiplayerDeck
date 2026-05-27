using System.Collections.Generic;
using System.IO;
using GameDataEditor;
using MultiplayerDeck;
using UnityEngine;

namespace MultiplayerDeck
{
	public class NetworkHelper
	{
		public enum dataType
		{
			Version,
			Ready,
			Test,
			BossBattleStart,
			BossBattleHP,
			Soul,
			Gold,
			BossBattleReady,
			BattleStart,
			StageState,
			NextStageVote,
			NextStageCommit,
			DeckState,
			TurnActionNum,
			SkillPlayed,
			ExchangeSkill,
			DeckContribution
		}

		public static SteamIntegration steam;

		public static List<SteamLobby> lobbies = new List<SteamLobby>();

		public static bool embarked = false;

		public static Packet packet = new Packet();

		public static void initialize()
		{
			steam = new SteamIntegration();
			SteamCallbacks.callbackInit();
		}

		public static void update()
		{
			if (service() == null)
			{
				return;
			}
			service().getPacket(packet);
			while (packet.hasPacket() && TogetherManager.currentLobby != null)
			{
				parseData(packet.getdata(), packet.getplayer());
				if (service() != null)
				{
					service().getPacket(packet);
					continue;
				}
				break;
			}
		}

		public static void parseData(byte[] data, RemotePlayer playerInfo)
		{
			MemoryStream memoryStream = new MemoryStream(data);
			memoryStream.Position = 0L;
			using (BinaryReader binaryReader = new BinaryReader(memoryStream))
			{
				dataType dataType = (dataType)binaryReader.ReadInt32();
				switch (dataType)
				{
					case dataType.Test:
					{
						string text = binaryReader.ReadString();
						Debug.Log(dataType.ToString() + " " + text);
						if (FieldSystem.instance != null)
						{
							FieldSystem.instance.BattleStart(new GDEEnemyQueueData(text), StageSystem.instance.StageData.BattleMap.Key, true, false, "", "", false);
						}
						break;
					}
					case dataType.BossBattleStart:
					{
						string text2 = binaryReader.ReadString();
						Debug.Log(dataType.ToString() + " " + text2);
						MultiplayerDeck_Plugin.MyBossEnterFriend(text2);
						break;
					}
					case dataType.BattleStart:
					{
						string queueData = binaryReader.ReadString();
						bool normalBattle = binaryReader.ReadBoolean();
						bool cursed = binaryReader.ReadBoolean();
						string rewardKey = binaryReader.ReadString();
						string preset = binaryReader.ReadString();
						bool noGameover = binaryReader.ReadBoolean();
                        Debug.Log(dataType.ToString() + " " + queueData);
						if (playerInfo != TogetherManager.currentUser)
						{
							MultiplayerDeck_Plugin.StartBattleFromNetwork(queueData, normalBattle, cursed, rewardKey, preset, noGameover);
						}
						break;
					}
					case dataType.BossBattleHP:
					{
						if (BattleSystem.instance == null || playerInfo == TogetherManager.currentUser)
						{
							break;
						}
						Debug.Log(dataType);
						MultiplayerDeck_Plugin.BossBattleNet bossBattleNet = null;
						foreach (PassiveBase item in BattleSystem.instance.BattleExtended)
						{
							if (item is MultiplayerDeck_Plugin.BossBattleNet)
							{
								bossBattleNet = item as MultiplayerDeck_Plugin.BossBattleNet;
								break;
							}
						}
						if (bossBattleNet == null)
						{
							break;
						}
						int num = binaryReader.ReadInt32();
						Debug.Log("BossNum: " + num);
						if (num != bossBattleNet.enemyList.Count)
						{
							break;
						}
						Debug.Log("BossHp: ");
						{
							foreach (BattleEnemy enemy in bossBattleNet.enemyList)
							{
								int num2 = binaryReader.ReadInt32();
								Debug.Log(num2);
								if (enemy.HP != num2)
								{
									enemy.Info.Hp = num2;
								}
							}
							break;
						}
					}
					case dataType.Soul:
					{
						int soul = binaryReader.ReadInt32();
						Debug.Log(dataType.ToString() + " " + soul);
						PlayData.TSavedata._Soul = soul;
						if (PlayData.TSavedata._Soul <= 0)
						{
							PlayData.TSavedata._Soul = 0;
						}
						break;
					}
					case dataType.StageState:
					{
						string stageKey = binaryReader.ReadString();
						int stageNum = binaryReader.ReadInt32();
						float x = binaryReader.ReadSingle();
						float y = binaryReader.ReadSingle();
						Debug.Log(dataType.ToString() + " " + stageKey);
						if (playerInfo != TogetherManager.currentUser && !string.IsNullOrEmpty(stageKey) && FieldSystem.instance != null)
						{
							PlayData.TSavedata.StageNum = stageNum;
							PlayData.TSavedata.NowStageMapKey = stageKey;
							if (StageSystem.instance == null || StageSystem.instance.StageData == null || StageSystem.instance.StageData.Key != stageKey)
							{
								FieldSystem.instance.StageStart(stageKey);
							}
							if (StageSystem.instance != null)
							{
								StageSystem.instance.PlayerPos = new Vector2(x, y);
							}
						}
						break;
					}
					case dataType.NextStageVote:
					{
						if (TogetherManager.currentLobby != null && TogetherManager.currentLobby.isOwner())
						{
							AddNextStageVote(playerInfo);
						}
						break;
					}
					case dataType.NextStageCommit:
					{
						nextStageVotes.Clear();
						if (playerInfo != TogetherManager.currentUser && FieldSystem.instance != null)
						{
							FieldSystem.instance.NextStage();
						}
						break;
					}
					case dataType.DeckState:
					{
						List<string> deck = readStringList(binaryReader);
						List<string> usedDeck = readStringList(binaryReader);
						if (playerInfo != TogetherManager.currentUser)
						{
							MultiplayerBattleSync.ApplyDeckState(deck, usedDeck);
						}
						break;
					}
					case dataType.TurnActionNum:
					{
						int value = binaryReader.ReadInt32();
						if (playerInfo != TogetherManager.currentUser)
						{
							MultiplayerBattleSync.ApplyTurnActionNum(value);
						}
						break;
					}
					case dataType.SkillPlayed:
					{
						string skillName = binaryReader.ReadString();
						if (playerInfo != TogetherManager.currentUser)
						{
							MultiplayerBattleSync.ApplyRemoteSkillName(skillName);
						}
						break;
					}
					case dataType.ExchangeSkill:
					{
						string targetAccountId = binaryReader.ReadString();
						string skillKey = binaryReader.ReadString();
						if (playerInfo != TogetherManager.currentUser && IsCurrentUser(targetAccountId))
						{
							MultiplayerBattleSync.ReceiveExchangedSkill(skillKey);
						}
						break;
					}
					case dataType.DeckContribution:
					{
						List<string> deck = readStringList(binaryReader);
						if (TogetherManager.currentLobby != null && TogetherManager.currentLobby.isOwner())
						{
							MultiplayerBattleSync.ReceiveDeckContribution(playerInfo, deck);
						}
						break;
					}
					case dataType.Gold:
					{
						int gold = binaryReader.ReadInt32();
						Debug.Log(dataType.ToString() + " " + gold);
						PlayData.TSavedata._Gold = gold;
						if (PlayData.TSavedata._Gold <= 0)
						{
							PlayData.TSavedata._Gold = 0;
						}
						break;
					}
					default:
					Debug.Log(dataType);
					break;
				}
			}
		}

		public static void sendData(dataType type)
		{
			byte[] array = generateData(type);
			if (array != null)
			{
				SteamIntegration steamService = service();
				if (steamService != null)
				{
					steamService.sendPacket(array);
				}
			}
		}

		private static byte[] generateData(dataType type)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)type);
				switch (type)
				{
					case dataType.Test:
					binaryWriter.Write("Queue_S4_King");
					break;
					case dataType.BossBattleStart:
					binaryWriter.Write(MultiplayerDeck_Plugin.MyBossEnterMessage(StageSystem.instance));
					break;
					case dataType.BossBattleHP:
					{
						if (BattleSystem.instance == null)
						{
							break;
						}
						MultiplayerDeck_Plugin.BossBattleNet bossBattleNet = null;
						foreach (PassiveBase item in BattleSystem.instance.BattleExtended)
						{
							if (item is MultiplayerDeck_Plugin.BossBattleNet)
							{
								bossBattleNet = item as MultiplayerDeck_Plugin.BossBattleNet;
								break;
							}
						}
						if (bossBattleNet == null)
						{
							break;
						}
						binaryWriter.Write(bossBattleNet.enemyList.Count);
						foreach (BattleEnemy enemy in bossBattleNet.enemyList)
						{
							binaryWriter.Write(enemy.HP);
						}
						break;
					}
					case dataType.Soul:
					binaryWriter.Write(PlayData.TSavedata._Soul);
					break;
					case dataType.Gold:
					binaryWriter.Write(PlayData.TSavedata._Gold);
					break;
					default:
					binaryWriter.Write((int)type);
					break;
				}
			}
			return memoryStream.ToArray();
		}

		private static readonly HashSet<string> nextStageVotes = new HashSet<string>();

		public static void SubmitNextStageVote()
		{
			if (TogetherManager.currentUser != null && TogetherManager.currentLobby != null && TogetherManager.currentLobby.isOwner())
			{
				AddNextStageVote(TogetherManager.currentUser);
			}
			sendData(dataType.NextStageVote);
		}

		private static void AddNextStageVote(RemotePlayer player)
		{
			if (player != null)
			{
				nextStageVotes.Add(player.getAccountID().ToString());
			}
			if (TogetherManager.currentUser != null && TogetherManager.currentLobby != null && TogetherManager.currentLobby.isOwner())
			{
				nextStageVotes.Add(TogetherManager.currentUser.getAccountID().ToString());
			}
			if (TogetherManager.players.Count != 0 && nextStageVotes.Count >= TogetherManager.players.Count)
			{
				nextStageVotes.Clear();
				sendData(dataType.NextStageCommit);
				if (FieldSystem.instance != null)
				{
					FieldSystem.instance.NextStage();
				}
			}
		}

		public static void sendBattleStart(string queueData, bool bossBattle, bool cursed, string rewardKey, string preset, bool noGameover)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)dataType.BattleStart);
				binaryWriter.Write(queueData ?? string.Empty);
				binaryWriter.Write(bossBattle);
				binaryWriter.Write(cursed);
				binaryWriter.Write(rewardKey);
				binaryWriter.Write(preset);
				binaryWriter.Write(noGameover);
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

		public static void sendStageState(string stageKey, Vector2 playerPos)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
			binaryWriter.Write((int)dataType.StageState);
			binaryWriter.Write(stageKey ?? string.Empty);
			binaryWriter.Write(PlayData.TSavedata != null ? PlayData.TSavedata.StageNum : 0);
			binaryWriter.Write(playerPos.x);
			binaryWriter.Write(playerPos.y);
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

		public static void sendDeckState()
		{
			if (BattleSystem.instance == null)
			{
				return;
			}

			BattleTeam team = BattleSystem.instance.AllyTeam;
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)dataType.DeckState);
				writeStringList(binaryWriter, MultiplayerBattleSync.SkillKeys(team.Skills_Deck));
				writeStringList(binaryWriter, MultiplayerBattleSync.SkillKeys(team.Skills_UsedDeck));
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

		public static void sendDeckState(List<string> deck, List<string> usedDeck)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)dataType.DeckState);
				writeStringList(binaryWriter, deck);
				writeStringList(binaryWriter, usedDeck);
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

		public static void sendTurnActionNum(int value)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)dataType.TurnActionNum);
				binaryWriter.Write(value);
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

		public static void sendSkillPlayed(string skillName)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)dataType.SkillPlayed);
				binaryWriter.Write(skillName ?? string.Empty);
			}
			service()?.sendPacket(memoryStream.ToArray());
		}

	public static void sendDeckContribution(List<string> deck)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write((int)dataType.DeckContribution);
			writeStringList(binaryWriter, deck);
		}
		SteamIntegration steamService = service();
		if (steamService != null)
		{
			steamService.sendPacket(memoryStream.ToArray());
		}
	}

	public static void sendExchangeSkill(string targetAccountId, string skillKey)
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write((int)dataType.ExchangeSkill);
			binaryWriter.Write(targetAccountId ?? string.Empty);
			binaryWriter.Write(skillKey ?? string.Empty);
		}
		service()?.sendPacket(memoryStream.ToArray());
	}

	public static bool IsCurrentUser(string accountId)
	{
		return TogetherManager.currentUser != null
			&& TogetherManager.currentUser.getAccountID().ToString() == accountId;
	}

		private static void writeStringList(BinaryWriter writer, List<string> values)
		{
			writer.Write(values.Count);
			foreach (string value in values)
			{
				writer.Write(value ?? string.Empty);
			}
		}

		private static List<string> readStringList(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			List<string> values = new List<string>(count);
			for (int i = 0; i < count; i++)
			{
				values.Add(reader.ReadString());
			}
			return values;
		}

		public static SteamIntegration service()
		{
			if (TogetherManager.currentLobby == null)
			{
				return null;
			}
			if (TogetherManager.currentLobby.service == null)
			{
				return null;
			}
			return TogetherManager.currentLobby.service;
		}

		public static void updateLobbyData()
		{
			if (TogetherManager.currentLobby != null)
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("owner", TogetherManager.currentUser.userName);
				dictionary.Add("members", TogetherManager.currentLobby.getMemberNameList());
				TogetherManager.currentLobby.setMetadata(dictionary);
			}
		}

		public static void createLobby()
		{
			Debug.Log("Creating Lobby...");
			steam.createLobby();
		}

		public static void setLobbyPrivate(bool toggle)
		{
			TogetherManager.currentLobby.setPrivate(toggle);
		}

		public static void leaveLobby()
		{
			if (TogetherManager.currentLobby != null)
			{
				if (TogetherManager.currentLobby.isOwner())
				{
					TogetherManager.currentLobby.newOwner();
				}
				TogetherManager.currentLobby.leaveLobby();
				TogetherManager.clearMultiplayerData();
			}
		}

		public static void getLobbies()
		{
			lobbies.Clear();
			steam.getLobbies();
		}

		public static void addPlayer(RemotePlayer player)
		{
			foreach (RemotePlayer player2 in TogetherManager.players)
			{
				if (player2.isUser(player.steamUser))
				{
					return;
				}
			}
			TogetherManager.players.Add(player);
			Debug.Log("Member joined: " + player.userName);
		}

		public static void removePlayer(RemotePlayer player)
		{
			
		}
	}
}
