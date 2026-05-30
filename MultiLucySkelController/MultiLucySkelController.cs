﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using MultiplayerDeck.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
namespace MultiplayerDeck
{


    /// <summary>
    /// 位置与跳跃状态同步补丁（基于 Harmony）
    /// 设计原则：本地保留完整输入/物理权限；远端屏蔽输入、覆盖Transform、插值渲染。
    /// </summary>
    [HarmonyPatch]
    public static class MultiLucySkelController
    {
        #region 权限与配置
        /// <summary>
        /// 渲染延迟（秒）。建议设为平均 RTT 的 50%~70%，通常 0.08~0.15s
        /// </summary>
        public const float InterpolationDelay = 0.12f;
        private const int MaxBufferSize = 48;
        private const float SendInterval = 0.05f;

        private static float _lastSendTime = 0f;
        #endregion

        #region 状态数据结构
        private struct SyncPacket
        {
            public float Timestamp;
            public Vector2 WorldPosition;
            public float JumpLocalY;
            public bool IsMoving;
            public bool FacingRight;
            public string SkinName;
        }

        private static readonly Dictionary<ulong, List<SyncPacket>> _syncBuffers = new Dictionary<ulong, List<SyncPacket>>();
        private static readonly Dictionary<ulong, PlayerController> _playerControllers = new Dictionary<ulong, PlayerController>();
        private static readonly Dictionary<ulong, string> _remotePlayerSkins = new Dictionary<ulong, string>();
        #endregion

        #region PlayerController 补丁
        [HarmonyPatch(typeof(PlayerController), "Update")]
        [HarmonyPrefix]
        private static bool PC_Update_Prefix(PlayerController __instance)
        {
            if (IsLocalPlayer(__instance)) return true;

            __instance.DonUpdate = true;
            return true;
        }

        [HarmonyPatch(typeof(PlayerController), "Update")]
        [HarmonyPostfix]
        private static void PC_Update_Postfix(PlayerController __instance)
        {
            if (IsLocalPlayer(__instance)) return;

            ulong steamId = GetRemotePlayerSteamId(__instance);
            if (steamId == 0) return;

            if (_syncBuffers.TryGetValue(steamId, out var buffer) && buffer.Count > 0)
            {
                var state = GetInterpolatedState(buffer, Time.time);
                if (state.HasValue)
                {
                    if (__instance.Spinedata != null)
                    {
                        if (state.Value.IsMoving)
                        {
                            __instance.Spinedata.AnimationName = "walking";
                            __instance.Spinedata.loop = true;
                            __instance.Spinedata.timeScale = 1f;
                        }
                        else
                        {
                            __instance.Spinedata.AnimationName = "standing";
                            __instance.Spinedata.loop = true;
                        }
                    }

                    __instance.Right = state.Value.FacingRight;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool PC_FixedUpdate_Prefix(PlayerController __instance)
        {
            if (IsLocalPlayer(__instance)) return true;

            ulong steamId = GetRemotePlayerSteamId(__instance);
            if (steamId == 0) return true;

            if (_syncBuffers.TryGetValue(steamId, out var buffer) && buffer.Count > 0)
            {
                var state = GetInterpolatedState(buffer, Time.time);
                if (state.HasValue)
                {
                    float dist = Vector2.Distance(__instance.transform.position, state.Value.WorldPosition);
                    if (dist > 1.5f)
                    {
                        __instance.transform.position = state.Value.WorldPosition;
                    }
                    else
                    {
                        __instance.transform.position = Vector2.Lerp(__instance.transform.position, state.Value.WorldPosition, 10f * Time.fixedDeltaTime);
                    }
                }
            }

            __instance.Movevec = Vector2.zero;
            if (__instance.rigiedbody != null) __instance.rigiedbody.velocity = Vector2.zero;
            return false;
        }

        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        [HarmonyPostfix]
        private static void PC_FixedUpdate_Postfix(PlayerController __instance)
        {
            if (!IsLocalPlayer(__instance)) return;
            if (!MultiplayerDeck_Plugin.IsMultiplayer) return;

            if (Time.time - _lastSendTime >= SendInterval)
            {
                _lastSendTime = Time.time;
                float jumpY = __instance.LucyCharMiantr?.localPosition.y ?? 0f;
                bool isMoving = __instance.Movevec != Vector2.zero;
                bool facingRight = __instance.Spinedata != null && __instance.Spinedata.transform.localScale.x > 0;
                string skinName = __instance.Spinedata != null ? __instance.Spinedata.initialSkinName : "skin_1";
                MessageSerializer.SendPosition(__instance.transform.position, jumpY, isMoving, facingRight, skinName);
            }
        }
        #endregion

        #region PlayerJump 补丁
        [HarmonyPatch(typeof(PlayerJump), "Update")]
        [HarmonyPrefix]
        private static bool PJ_Update_Prefix(PlayerJump __instance)
        {
            if (__instance.MainCont == null) return true;
            return IsLocalPlayer(__instance.MainCont);
        }

        [HarmonyPatch(typeof(PlayerJump), nameof(PlayerJump.FixedUpdate))]
        [HarmonyPrefix]
        private static bool PJ_FixedUpdate_Prefix(PlayerJump __instance)
        {
            if (__instance.MainCont == null || IsLocalPlayer(__instance.MainCont)) return true;

            ulong steamId = GetRemotePlayerSteamId(__instance.MainCont);
            if (steamId == 0) return true;

            var childTr = __instance.MainCont.LucyCharMiantr;
            if (childTr == null) return true;

            if (_syncBuffers.TryGetValue(steamId, out var buffer) && buffer.Count > 0)
            {
                var state = GetInterpolatedState(buffer, Time.time);
                if (state.HasValue)
                {
                    float targetY = state.Value.JumpLocalY;
                    float currentY = childTr.localPosition.y;
                    childTr.localPosition = new Vector3(0f, Mathf.Lerp(currentY, targetY, 8f * Time.fixedDeltaTime), 0f);
                }
            }

            __instance.JumpSpeed = 0f;
            return false;
        }
        #endregion

        #region 碰撞交互补丁
        [HarmonyPatch(typeof(PlayerController), "OnTriggerStay2D")]
        [HarmonyPrefix]
        private static bool PC_OnTriggerStay2D_Prefix(PlayerController __instance)
        {
            if (IsLocalPlayer(__instance)) return true;

            if (__instance.Coll != null)
            {
                __instance.Emoji.Off();
                __instance.Coll = null;
            }
            return false;
        }

        [HarmonyPatch(typeof(PlayerController), "OnTriggerExit2D")]
        [HarmonyPrefix]
        private static bool PC_OnTriggerExit2D_Prefix(PlayerController __instance)
        {
            if (IsLocalPlayer(__instance)) return true;

            if (__instance.Coll != null)
            {
                __instance.Emoji.Off();
                __instance.Coll = null;
            }
            return false;
        }

        [HarmonyPatch(typeof(EventObject), "OnTriggerExit2D")]
        [HarmonyPrefix]
        private static bool EO_OnTriggerExit2D_Prefix(EventObject __instance, Collider2D coll)
        {
            if (coll.gameObject.tag != "Player") return true;

            PlayerController collPlayer = coll.gameObject.GetComponent<PlayerController>();
            if (collPlayer != null && IsLocalPlayer(collPlayer)) return true;

            return false;
        }
        #endregion

        #region 远端玩家创建与管理
        private static GameObject _remotePlayerTemplate;
        private static readonly HashSet<ulong> _createdRemotePlayers = new HashSet<ulong>();
        private static readonly List<ulong> _pendingPlayers = new List<ulong>();
        private static bool _isRetrying = false;
        private static readonly float RetryInterval = 1f;
        private static float _lastRetryTime = 0f;

        private static void CapturePlayerTemplate()
        {
            if (_remotePlayerTemplate != null) return;
            if (FieldSystem.instance == null || FieldSystem.instance.Playercontrol == null) return;

            var original = FieldSystem.instance.Playercontrol.gameObject;
            _remotePlayerTemplate = GameObject.Instantiate(original);
            _remotePlayerTemplate.name = "_RemotePlayerTemplate";
            _remotePlayerTemplate.SetActive(false);
            GameObject.DontDestroyOnLoad(_remotePlayerTemplate);
            Debug.Log("[MultiLucySkelController] Captured hidden remote player template.");
        }

        public static void EnsureRemotePlayerController(ulong steamId)
        {
            if (_playerControllers.TryGetValue(steamId, out var existing) && existing != null && existing.gameObject != null)
                return;

            _playerControllers.Remove(steamId);

            if (_remotePlayerTemplate == null)
            {
                CapturePlayerTemplate();
                if (_remotePlayerTemplate == null)
                {
                    Debug.LogWarning("[MultiLucySkelController] Cannot create remote player for " + steamId + ": template not available.");
                    return;
                }
            }

            CreateRemotePlayerController(steamId);
        }

        public static void TryCreateRemotePlayer(ulong steamId)
        {
            if (_createdRemotePlayers.Contains(steamId))
                return;

            _createdRemotePlayers.Add(steamId);
            CapturePlayerTemplate();

            if (FieldSystem.instance == null || FieldSystem.instance.Playercontrol == null)
            {
                if (!_pendingPlayers.Contains(steamId))
                {
                    _pendingPlayers.Add(steamId);
                    Debug.Log("[MultiLucySkelController] FieldSystem not ready, queued SteamID: " + steamId + " for later creation.");
                }
                StartRetryLoop();
                return;
            }

            EnsureRemotePlayerController(steamId);
        }

        public static void InitializeRemotePlayers()
        {
            if (FieldSystem.instance == null || FieldSystem.instance.Playercontrol == null)
            {
                Debug.LogWarning("[MultiLucySkelController] Cannot initialize remote players yet: Playercontrol not found.");
                StartRetryLoop();
                return;
            }

            CapturePlayerTemplate();

            foreach (RemotePlayer player in TogetherManager.players)
            {
                if (player == null || TogetherManager.currentUser != null && player.IsUser(TogetherManager.currentUser.steamUser))
                    continue;

                TryCreateRemotePlayer(player.steamUser.m_SteamID);
                EnsureRemotePlayerController(player.steamUser.m_SteamID);
            }
        }

        private static void StartRetryLoop()
        {
            if (_isRetrying || _pendingPlayers.Count == 0)
                return;

            _isRetrying = true;
            _lastRetryTime = Time.time;

            SaveManager.savemanager?.StartCoroutine(RetryCreatePendingPlayers());

        }

        private static System.Collections.IEnumerator RetryCreatePendingPlayers()
        {
            while (_pendingPlayers.Count > 0)
            {
                if (Time.time - _lastRetryTime < RetryInterval)
                {
                    yield return null;
                    continue;
                }

                _lastRetryTime = Time.time;

                if (FieldSystem.instance == null || FieldSystem.instance.Playercontrol == null)
                {
                    Debug.Log("[MultiLucySkelController] Waiting for FieldSystem to be ready...");
                    yield return null;
                    continue;
                }

                CapturePlayerTemplate();

                var toCreate = new List<ulong>(_pendingPlayers);
                _pendingPlayers.Clear();

                foreach (ulong steamId in toCreate)
                    EnsureRemotePlayerController(steamId);
            }

            _isRetrying = false;
        }

        private static void CreateRemotePlayerController(ulong steamId)
        {
            if (_remotePlayerTemplate == null)
            {
                Debug.LogError("[MultiLucySkelController] Remote player template not available.");
                return;
            }

            GameObject remoteObj = GameObject.Instantiate(_remotePlayerTemplate);
            remoteObj.name = "RemotePlayer_" + steamId;
            remoteObj.SetActive(true);

            PlayerController remoteController = remoteObj.GetComponent<PlayerController>();
            if (remoteController == null)
            {
                Debug.LogError("[MultiLucySkelController] PlayerController component not found on instantiated prefab.");
                GameObject.Destroy(remoteObj);
                return;
            }

            if (remoteController.Spinedata != null)
            {
                remoteController.Spinedata.AnimationName = "standing";
                remoteController.Spinedata.loop = true;
                remoteController.Spinedata.timeScale = 1f;
            }

            if (remoteController.LucyCharMiantr != null)
                remoteController.LucyCharMiantr.localPosition = Vector3.zero;

            if (remoteController.rigiedbody != null)
                remoteController.rigiedbody.velocity = Vector2.zero;

            remoteController.Movevec = Vector2.zero;
            remoteController.DonUpdate = false;
            remoteController.enabled = true;

            RegisterPlayerController(steamId, remoteController);

            CreatePlayerNameTag(remoteObj, steamId);

            Debug.Log("[MultiLucySkelController] Created remote player controller for SteamID: " + steamId);
        }

        public static void CreatePlayerNameTagPublic(GameObject parent, ulong steamId)
        {
            CreatePlayerNameTag(parent, steamId);
        }

        private static void CreatePlayerNameTag(GameObject parent, ulong steamId)
        {
            GameObject tagObj = new GameObject("NameTag_" + steamId);
            tagObj.transform.SetParent(parent.transform);
            tagObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            tagObj.transform.localScale = Vector3.one;

            Debug.Log("[NameTag] Created NameTag for SteamID: " + steamId + ", parent: " + parent.name + ", localPos: " + tagObj.transform.localPosition);

            GameObject avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(tagObj.transform, false);
            avatarObj.transform.localPosition = Vector3.zero;
            avatarObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            SpriteRenderer avatarRenderer = avatarObj.AddComponent<SpriteRenderer>();

            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(tagObj.transform, false);
            nameObj.transform.localPosition = new Vector3(0.3f, 0f, 0f);
            TextMesh nameText = nameObj.AddComponent<TextMesh>();
            nameText.fontSize = 28;
            nameText.anchor = TextAnchor.MiddleCenter;
            nameText.alignment = TextAlignment.Center;
            nameText.color = Color.white;
            nameText.text = "Loading...";
            nameText.characterSize = 0.08f;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            PlayerNameTag tag = tagObj.AddComponent<PlayerNameTag>();
            tag.SteamId = steamId;

            Debug.Log("[NameTag] NameTag components added");
        }

        public static void CleanupAllRemotePlayers()
        {
            foreach (ulong steamId in _createdRemotePlayers)
            {
                if (_playerControllers.TryGetValue(steamId, out var controller))
                {
                    if (controller != null && controller.gameObject != null)
                    {
                        GameObject.Destroy(controller.gameObject);
                    }
                }
            }

            _playerControllers.Clear();
            _syncBuffers.Clear();
            _createdRemotePlayers.Clear();
            _pendingPlayers.Clear();
            if (_remotePlayerTemplate != null)
            {
                GameObject.Destroy(_remotePlayerTemplate);
                _remotePlayerTemplate = null;
            }
            _isRetrying = false;
        }

        public static void CleanupRemotePlayer(ulong steamId)
        {
            if (_playerControllers.TryGetValue(steamId, out var controller))
            {
                if (controller != null && controller.gameObject != null)
                {
                    GameObject.Destroy(controller.gameObject);
                }
            }

            UnregisterPlayerController(steamId);
            _createdRemotePlayers.Remove(steamId);
            _pendingPlayers.RemoveAll(id => id == steamId);
        }
        #endregion

        #region 核心同步工具
        private static bool IsLocalPlayer(PlayerController controller)
        {
            if (!MultiplayerDeck_Plugin.IsMultiplayer) return true;
            if (TogetherManager.currentUser == null) return true;

            foreach (var kvp in _playerControllers)
            {
                if (kvp.Value == controller)
                {
                    return false;
                }
            }
            return true;
        }

        private static ulong GetRemotePlayerSteamId(PlayerController controller)
        {
            foreach (var kvp in _playerControllers)
            {
                if (kvp.Value == controller)
                {
                    return kvp.Key;
                }
            }
            return 0;
        }

        private static SyncPacket? GetInterpolatedState(List<SyncPacket> buffer, float currentTime)
        {
            if (buffer == null || buffer.Count == 0) return null;
            if (buffer.Count < 2) return buffer[buffer.Count - 1];

            float targetTime = currentTime - InterpolationDelay;
            SyncPacket a = buffer[0], b = buffer[1];
            bool found = false;

            for (int i = 1; i < buffer.Count; i++)
            {
                if (buffer[i].Timestamp >= targetTime)
                {
                    a = buffer[i - 1];
                    b = buffer[i];
                    found = true;
                    break;
                }
            }

            if (!found) return buffer[buffer.Count - 1];

            float t = Mathf.Clamp01((targetTime - a.Timestamp) / Mathf.Max(0.0001f, b.Timestamp - a.Timestamp));
            return new SyncPacket
            {
                Timestamp = targetTime,
                WorldPosition = Vector2.Lerp(a.WorldPosition, b.WorldPosition, t),
                JumpLocalY = Mathf.Lerp(a.JumpLocalY, b.JumpLocalY, t),
                IsMoving = b.IsMoving,
                FacingRight = b.FacingRight
            };
        }

        /// <summary>
        /// 外部网络接收回调入口。由 NetworkHelper 调用。
        /// </summary>
        public static void OnReceiveRemoteState(ulong steamId, Vector2 pos, float jumpY, float timestamp, bool isMoving, bool facingRight, string skinName = null)
        {
            EnsureRemotePlayerController(steamId);

            if (!_syncBuffers.ContainsKey(steamId))
            {
                _syncBuffers[steamId] = new List<SyncPacket>();
            }
            var list = _syncBuffers[steamId];

            if (list.Count > 0 && timestamp <= list[list.Count - 1].Timestamp) return;

            list.Add(new SyncPacket { Timestamp = timestamp, WorldPosition = pos, JumpLocalY = jumpY, IsMoving = isMoving, FacingRight = facingRight, SkinName = skinName });
            if (list.Count > MaxBufferSize) list.RemoveRange(0, list.Count - MaxBufferSize);

            if (!string.IsNullOrEmpty(skinName))
            {
                ApplyRemotePlayerSkin(steamId, skinName);
            }
        }

        private static void ApplyRemotePlayerSkin(ulong steamId, string skinName)
        {
            if (_remotePlayerSkins.TryGetValue(steamId, out var currentSkin) && currentSkin == skinName)
            {
                return;
            }

            _remotePlayerSkins[steamId] = skinName;

            if (_playerControllers.TryGetValue(steamId, out var controller) && controller != null && controller.Spinedata != null)
            {
                controller.Spinedata.initialSkinName = skinName;
                controller.Spinedata.Initialize(overwrite: true);
            }
        }

        public static void RegisterPlayerController(ulong steamId, PlayerController controller)
        {
            _playerControllers[steamId] = controller;
        }

        public static void UnregisterPlayerController(ulong steamId)
        {
            _playerControllers.Remove(steamId);
            _syncBuffers.Remove(steamId);
        }

        public static bool GetPlayerController(ulong steamId, out PlayerController controller)
        {
            return _playerControllers.TryGetValue(steamId, out controller);
        }

        

        
        #endregion
    }
}
