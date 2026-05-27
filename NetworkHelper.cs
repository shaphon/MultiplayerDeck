using GameDataEditor;
using MultiplayerDeck;
using Spine;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using UnityEngine;
using UnityEngine.Analytics;

namespace MultiplayerDeck
{
	public enum NetDataType
	{
		Version,
		Ready,
		Test,
		BattleStart,
		BattleStartDeck,
		RequestForBattleStartDeck,
		DeckState,
		EnemyHP,
		TurnActionNum,
		ExchangeSkill,
		SkillPlayed,
		Vote,
		StageMap,
		MonsterClear,
		BossClear,
		LobbyClosed
	}

	public class NetworkHelper
	{
		public static SteamIntegration steam;

		public static List<SteamLobby> lobbies = new List<SteamLobby>();

		public static bool embarked = false;

		public static Packet packet = new Packet();

		public static void Initialize()
		{
			steam = new SteamIntegration();
			SteamCallbacks.callbackInit();
		}

		public static void Update()
		{
			if (Service() == null)
			{
				return;
			}
			Service().GetPacket(packet);
			while (packet.HasPacket() && TogetherManager.currentLobby != null)
			{
				ParseData(packet.GetData(), packet.GetPlayer());
				if (Service() != null)
				{
					Service().GetPacket(packet);
					continue;
				}
				break;
			}
		}

		public static void ParseData(byte[] data, RemotePlayer playerInfo)
		{
			if (playerInfo != null &&
				TogetherManager.currentUser != null &&
				playerInfo.IsUser(TogetherManager.currentUser.steamUser))
			{
				return;
			}
			try
			{
				MemoryStream memoryStream = new MemoryStream(data);
				memoryStream.Position = 0L;
				using (BinaryReader binaryReader = new BinaryReader(memoryStream))
				{
					NetDataType dataType = (NetDataType)binaryReader.ReadInt32();
					switch (dataType)
					{
						case NetDataType.Test:
						{
							string text = binaryReader.ReadString();
							Debug.Log(dataType.ToString() + " " + text);
							if (FieldSystem.instance != null)
							{
								FieldSystem.instance.BattleStart(new GDEEnemyQueueData(text), StageSystem.instance.StageData.BattleMap.Key, true, false, "", "", false);
							}
							break;
						}
						case NetDataType.BattleStart:
						{
							string queueData = binaryReader.ReadString();
							bool normalBattle = binaryReader.ReadBoolean();
							bool cursed = binaryReader.ReadBoolean();
							string rewardKey = binaryReader.ReadString();
							string preset = binaryReader.ReadString();
							bool noGameover = binaryReader.ReadBoolean();
							Debug.Log(dataType.ToString() + " " + queueData);

							StageSyncManager.Instance.StartBattleFromNetwork(queueData, normalBattle, cursed, rewardKey, preset, noGameover);
							break;
						}
						case NetDataType.RequestForBattleStartDeck:
						{
							if (TogetherManager.currentLobby != null && !TogetherManager.currentLobby.IsOwner())
							{
								BattleSyncManager.Instance.SendPersonalDeck();
							}
							break;
						}
						case NetDataType.BattleStartDeck:
						{
							if (TogetherManager.currentLobby.IsOwner())
							{
								List<SkillNetworkDTO> deck = SkillSerializer.SkillDTOListDeserialize(binaryReader);
								BattleSyncManager.Instance.ReceiveDeckContribution(playerInfo, deck);
							}
							else
							{
								List<Skill> deck = SkillSerializer.SkillListDeserialize(binaryReader);
								BattleSyncManager.Instance.ReceiveCombinedDeck(playerInfo, deck);
							}
							break;
						}
						case NetDataType.DeckState:
						{
							bool usedDeck = binaryReader.ReadBoolean();
							List<Skill> skills = SkillSerializer.SkillListDeserialize(binaryReader);
							BattleSyncManager.Instance.ApplyDeckState(usedDeck, skills);
							break;
						}
						case NetDataType.EnemyHP:
						{
							string enemyKey = binaryReader.ReadString();
							int position = binaryReader.ReadInt32();
							int hp = binaryReader.ReadInt32();
							BattleSyncManager.Instance.ApplyEnemyHp(enemyKey, position, hp);
							break;
						}
						case NetDataType.TurnActionNum:
						{
							int value = binaryReader.ReadInt32();
							BattleSyncManager.Instance.ApplyTurnActionNum(value);
							break;
						}
						case NetDataType.ExchangeSkill:
						{
							ulong targetAccountId = binaryReader.ReadUInt64();
							Skill skill = SkillSerializer.SkillDeserialize(binaryReader);
							if (skill == null)
							{
								break;
							}
							if (playerInfo != TogetherManager.currentUser && targetAccountId == TogetherManager.currentUser.steamUser.m_SteamID)
							{
								BattleSyncManager.Instance.ReceiveExchangedSkill(skill);
							}
							break;
						}
						case NetDataType.Vote:
						{
							VoteManager.VoteTheme voteTheme = (VoteManager.VoteTheme)binaryReader.ReadInt32();
							ulong playerId = binaryReader.ReadUInt64();
							bool cancel = binaryReader.ReadBoolean();
							VoteManager.Instance.VoteFromNetwork(voteTheme, playerId, cancel);
							break;
						}
						case NetDataType.StageMap:
						{
							if (!MultiplayerDeck_Plugin.IsLobbyOwner)
							{
								StageMapSyncHelper.mapPacket = StageMapSyncHelper.DeserializeMapPacket(data);
								StageSyncManager.Instance.GotoNextStage();
							}
							break;
						}
						case NetDataType.MonsterClear:
						{
							float x = binaryReader.ReadSingle();
							float y = binaryReader.ReadSingle();
							StageSyncManager.Instance.MonsterClear(new Vector2(x, y));
							break;
						}
						case NetDataType.BossClear:
						{
							StageSyncManager.Instance.bossClear = true;
							StageSyncManager.Instance.BossClear();
							break;
						}
						case NetDataType.LobbyClosed:
						{
							ulong lobbyId = binaryReader.ReadUInt64();
							string reason = binaryReader.ReadString();
							if (TogetherManager.currentLobby != null && TogetherManager.currentLobby.steamID.m_SteamID == lobbyId)
							{
								Debug.Log("[MultiplayerDeck] Lobby closed by owner: " + reason);
								TogetherManager.ClearMultiplayerData();
								VoteManager.Instance.AbortCurrentVote();
							}
							break;
						}

						case NetDataType.SkillPlayed:
						{
							string skillName = binaryReader.ReadString();
							if (playerInfo != TogetherManager.currentUser)
							{
								BattleSyncManager.Instance.ApplyRemoteSkillName(skillName);
							}
							break;
						}
						default:
						Debug.Log(dataType);
						break;
					}
				}
			}
			catch (Exception ex) { }
		}

		public static void SendData(NetDataType type)
		{
			byte[] array = GenerateData(type);
			if (array != null)
			{
				Service()?.SendPacket(array);
			}
		}

		private static byte[] GenerateData(NetDataType type)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)type);
				switch (type)
				{
					case NetDataType.Test:
					{
						binaryWriter.Write("Queue_S4_King");
						break;
					}
					default:
					binaryWriter.Write((int)type);
					break;
				}
			}
			return memoryStream.ToArray();
		}

		public static void SendBattleStart(string queueData, bool normalBattle, bool cursed, string rewardKey, string preset, bool noGameover)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.BattleStart);
				binaryWriter.Write(queueData ?? string.Empty);
				binaryWriter.Write(normalBattle);
				binaryWriter.Write(cursed);
				binaryWriter.Write(rewardKey);
				binaryWriter.Write(preset);
				binaryWriter.Write(noGameover);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendEnemyHpChange(string enemy, int position, int hp)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.EnemyHP);
				binaryWriter.Write(enemy);
				binaryWriter.Write(position);
				binaryWriter.Write(hp);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendVote(VoteManager.VoteTheme voteTheme, ulong playerId, bool cancel)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.Vote);
				binaryWriter.Write((int)voteTheme);
				binaryWriter.Write(playerId);
				binaryWriter.Write(cancel);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendDeckState(List<Skill> skills, bool usedDeck = false)
		{
			if (BattleSystem.instance == null)
			{
				return;
			}

            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.DeckState);
                binaryWriter.Write(usedDeck);
				SkillSerializer.SkillListSerialize(binaryWriter, skills);
            }
            Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendTurnActionNum(int value)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.TurnActionNum);
				binaryWriter.Write(value);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendSkillPlayed(string skillName)
		{
			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.SkillPlayed);
				binaryWriter.Write(skillName ?? string.Empty);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendExchangeSkill(ulong targetAccountId, Skill skill)
		{
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.ExchangeSkill);
				binaryWriter.Write(targetAccountId);
				SkillSerializer.SkillSerialize(binaryWriter, skill);
            }
            Service()?.SendPacket(memoryStream.ToArray());
		}

		public static void SendMonsterClear(Vector2 pos)
		{
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write((int)NetDataType.MonsterClear);
                binaryWriter.Write(pos.x);
				binaryWriter.Write(pos.y);
            }
            Service()?.SendPacket(memoryStream.ToArray());
        }

		public static void SendLobbyClosed(string reason)
		{
			if (TogetherManager.currentLobby == null)
			{
				return;
			}

			MemoryStream memoryStream = new MemoryStream();
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				binaryWriter.Write((int)NetDataType.LobbyClosed);
				binaryWriter.Write(TogetherManager.currentLobby.steamID.m_SteamID);
				binaryWriter.Write(reason ?? string.Empty);
			}
			Service()?.SendPacket(memoryStream.ToArray());
		}

		public static SteamIntegration Service()
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

		public static void UpdateLobbyData()
		{
			if (TogetherManager.currentLobby != null)
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("owner", TogetherManager.currentUser.userName);
				dictionary.Add("members", TogetherManager.currentLobby.GetMemberNameList());
				TogetherManager.currentLobby.SetMetadata(dictionary);
			}
		}

		public static void CreateLobby()
		{
			Debug.Log("Creating Lobby...");
			steam.CreateLobby();
		}

		public static void SetLobbyPrivate(bool toggle)
		{
			TogetherManager.currentLobby.SetPrivate(toggle);
		}

		public static void LeaveLobby()
		{
			if (TogetherManager.currentLobby != null)
			{
				bool wasOwner = TogetherManager.currentLobby.IsOwner();
				if (wasOwner && TogetherManager.players.Count > 1)
				{
					TogetherManager.currentLobby.NewOwner();
				}
				TogetherManager.currentLobby.LeaveLobby();
				TogetherManager.ClearMultiplayerData();
				VoteManager.Instance.AbortCurrentVote();
			}
		}

		public static void DisbandLobby()
		{
			if (TogetherManager.currentLobby == null)
			{
				return;
			}

			if (!TogetherManager.currentLobby.IsOwner())
			{
				LeaveLobby();
				return;
			}

			SendLobbyClosed("Owner disbanded lobby");
			//SendLobbyClosed("Owner disbanded lobby");
			//SendLobbyClosed("Owner disbanded lobby");
			TogetherManager.currentLobby.SetJoinable(false);
			TogetherManager.currentLobby.SetPrivate(true);
			TogetherManager.currentLobby.LeaveLobby();
			TogetherManager.ClearMultiplayerData();
			VoteManager.Instance.AbortCurrentVote();
		}

		public static void GetLobbies()
		{
			lobbies.Clear();
			steam.GetLobbies();
		}

		public static void AddPlayer(RemotePlayer player)
		{
			if (player == null)
			{
				return;
			}

			foreach (RemotePlayer player2 in TogetherManager.players)
			{
				if (player2.IsUser(player.steamUser))
				{
					return;
				}
			}
			TogetherManager.players.Add(player);
			Debug.Log("Member joined: " + player.userName);
			VoteManager.Instance.SyncPlayersWithLobby();
		}

		public static void RemovePlayer(RemotePlayer player)
		{
			if (player == null)
			{
				return;
			}

			for (int i = TogetherManager.players.Count - 1; i >= 0; i--)
			{
				if (TogetherManager.players[i].IsUser(player.steamUser))
				{
					Debug.Log("Member left: " + TogetherManager.players[i].userName);
					TogetherManager.players.RemoveAt(i);
				}
			}

			VoteManager.Instance.SyncPlayersWithLobby();
		}
	}
}
