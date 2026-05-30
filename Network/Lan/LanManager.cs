using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Steamworks;
using UnityEngine;

namespace MultiplayerDeck.Network
{
    public class LanManager : ILobbyService
    {
        public const int DefaultPort = 24567;
        public const int MaxPlayers = 4;
        private const int MaxUdpPayload = 50000; // large payload for LAN (fits most maps in single packet)
        private const float FragmentTimeout = 10f; // seconds before stale fragments are cleaned

        private UdpClient _udpClient;
        private Thread _receiveThread;
        private volatile bool _running;
        private readonly object _queueLock = new object();
        private Queue<Packet> _packetQueue = new Queue<Packet>();

        // Fragmentation
        private static int _nextFragmentGroupId = 1;
        private readonly object _fragmentLock = new object();
        private Dictionary<ulong, Dictionary<int, PendingFragment>> _pendingFragments = new Dictionary<ulong, Dictionary<int, PendingFragment>>();
        private float _lastFragmentCleanupTime;

        private class PendingFragment
        {
            public int TotalFragments;
            public int ReceivedMask; // bitmask of received fragment indices
            public byte[][] Fragments;
            public float LastReceivedTime;
        }

        // Player management
        private Dictionary<ulong, IPEndPoint> _playerEndpoints = new Dictionary<ulong, IPEndPoint>();
        private Dictionary<ulong, RemotePlayer> _players = new Dictionary<ulong, RemotePlayer>();
        private ulong _localPlayerId;
        private string _localPlayerName;
        private int _nextPlayerId = 2; // 1 = host, 2+ = clients

        // State
        public bool IsHost { get; private set; }
        public bool IsActive { get; set; }
        public bool IsConnected
        {
            get { return _running && IsActive; }
        }

        // Lobby
        public LanLobby Lobby { get; private set; }

        // Discovery results
        public List<LanLobbyInfo> DiscoveredLobbies = new List<LanLobbyInfo>();

        // Public properties
        public ulong LocalPlayerId
        {
            get { return _localPlayerId; }
        }

        public string LocalPlayerName
        {
            get { return _localPlayerName; }
        }

        public int PlayerCount
        {
            get { return _players.Count; }
        }

        public List<RemotePlayer> GetPlayers()
        {
            return new List<RemotePlayer>(_players.Values);
        }

        public RemotePlayer GetPlayer(ulong id)
        {
            RemotePlayer player;
            if (_players.TryGetValue(id, out player))
            {
                return player;
            }
            return null;
        }

        // ==================== Transport API ====================

        public void GetPacket(Packet packet)
        {
            CleanupStaleFragments();

            lock (_queueLock)
            {
                if (_packetQueue.Count > 0)
                {
                    Packet p = _packetQueue.Dequeue();
                    packet.Set(p.GetPlayer(), p.GetData());
                }
                else
                {
                    packet.Clear();
                }
            }
        }

        public void SendPacket(byte[] data)
        {
            if (!_running)
            {
                return;
            }

            byte[] wrapped = WrapDataPacket(_localPlayerId, data);

            // Fragment large packets to avoid UDP MTU issues
            if (wrapped.Length > MaxUdpPayload)
            {
                SendFragmented(wrapped);
                return;
            }

            SendRaw(wrapped);
        }

        private void SendRaw(byte[] data)
        {
            if (IsHost)
            {
                foreach (KeyValuePair<ulong, IPEndPoint> kvp in _playerEndpoints)
                {
                    if (kvp.Key != _localPlayerId)
                    {
                        try { _udpClient.Send(data, data.Length, kvp.Value); }
                        catch (Exception ex) { Debug.LogError("[LanManager] Send error to " + kvp.Key + ": " + ex.Message); }
                    }
                }
            }
            else
            {
                IPEndPoint hostEp;
                if (_playerEndpoints.TryGetValue(1, out hostEp))
                {
                    try { _udpClient.Send(data, data.Length, hostEp); }
                    catch (Exception ex) { Debug.LogError("[LanManager] Send error: " + ex.Message); }
                }
            }
        }

        private void SendFragmented(byte[] data)
        {
            int groupId = Interlocked.Increment(ref _nextFragmentGroupId);
            int totalFragments = (data.Length + MaxUdpPayload - 1) / MaxUdpPayload;

            for (int i = 0; i < totalFragments; i++)
            {
                int offset = i * MaxUdpPayload;
                int chunkSize = Math.Min(MaxUdpPayload, data.Length - offset);

                // Fragment header: [type=7][senderId:8][groupId:4][totalFrags:4][fragIndex:4][chunkData...]
                byte[] fragment = new byte[1 + 8 + 4 + 4 + 4 + chunkSize];
                fragment[0] = 7; // MessageType.Fragment
                Buffer.BlockCopy(BitConverter.GetBytes(_localPlayerId), 0, fragment, 1, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(groupId), 0, fragment, 9, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(totalFragments), 0, fragment, 13, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(i), 0, fragment, 17, 4);
                Buffer.BlockCopy(data, offset, fragment, 21, chunkSize);

                SendRaw(fragment);

                // Small delay between fragments to avoid overwhelming receive buffer
                if (totalFragments > 1 && i < totalFragments - 1)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private static byte[] WrapDataPacket(ulong senderId, byte[] data)
        {
            byte[] wrapped = new byte[1 + 8 + data.Length];
            wrapped[0] = 0; // MessageType.Data
            byte[] idBytes = BitConverter.GetBytes(senderId);
            Buffer.BlockCopy(idBytes, 0, wrapped, 1, 8);
            Buffer.BlockCopy(data, 0, wrapped, 9, data.Length);
            return wrapped;
        }

        // ==================== Lobby Actions ====================

        public void HostLobby(int port, string playerName)
        {
            if (_running)
            {
                Disconnect();
            }

            _localPlayerId = 1;
            _localPlayerName = playerName;
            IsHost = true;
            IsActive = true;

            Lobby = new LanLobby(this);

            _udpClient = new UdpClient(port, AddressFamily.InterNetwork);
            _udpClient.EnableBroadcast = true;
            _running = true;

            // Add self
            RemotePlayer selfPlayer = new RemotePlayer(_localPlayerId, _localPlayerName);
            _players[_localPlayerId] = selfPlayer;

            // Setup global state
            TogetherManager.lanLobby = Lobby;
            TogetherManager.currentUser = selfPlayer;
            TogetherManager.players = new List<RemotePlayer> { selfPlayer };

            // Start receive thread
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            Debug.Log("[LanManager] Hosting on port " + port + " as '" + playerName + "'");
        }

        public void JoinLobby(string ip, int port, string playerName)
        {
            if (_running)
            {
                Disconnect();
            }

            _localPlayerName = playerName;
            IsHost = false;

            _udpClient = new UdpClient(0, AddressFamily.InterNetwork); // ephemeral port
            _udpClient.EnableBroadcast = true;
            _running = true;

            IPEndPoint hostEp = new IPEndPoint(IPAddress.Parse(ip), port);
            _playerEndpoints[1] = hostEp; // host is always ID 1

            // Start receive thread before sending connect
            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();

            // Send connect request
            SendConnectRequest();

            Debug.Log("[LanManager] Connecting to " + ip + ":" + port + " as '" + playerName + "'");
        }

        private void SendConnectRequest()
        {
            MemoryStream ms = new MemoryStream();
            byte[] sendData;
            using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write((byte)1); // MessageType.Connect
                byte[] nameBytes = Encoding.UTF8.GetBytes(_localPlayerName);
                bw.Write((ushort)nameBytes.Length);
                bw.Write(nameBytes);
                bw.Flush();
                sendData = ms.ToArray();
            }

            IPEndPoint hostEp;
            if (_playerEndpoints.TryGetValue(1, out hostEp))
            {
                try
                {
                    _udpClient.Send(sendData, sendData.Length, hostEp);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[LanManager] Failed to send connect: " + ex.Message);
                }
            }
        }

        public void DiscoverLobbies()
        {
            DiscoveredLobbies.Clear();

            try
            {
                using (UdpClient broadcast = new UdpClient(0, AddressFamily.InterNetwork))
                {
                    broadcast.EnableBroadcast = true;
                    broadcast.Client.ReceiveTimeout = 1500;

                    IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Broadcast, DefaultPort);
                    broadcast.Send(new byte[] { 5 }, 1, broadcastEp); // Discover

                    DateTime startTime = DateTime.Now;
                    while ((DateTime.Now - startTime).TotalSeconds < 2.0)
                    {
                        try
                        {
                            IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                            byte[] data = broadcast.Receive(ref remoteEp);

                            if (data.Length > 1 && data[0] == 6) // LobbyAnnounce
                            {
                                ParseLobbyAnnounce(data, remoteEp);
                            }
                        }
                        catch (SocketException)
                        {
                            break; // timeout
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[LanManager] Discovery error: " + ex.Message);
            }

            Debug.Log("[LanManager] Discovered " + DiscoveredLobbies.Count + " lobbies");
        }

        private void ParseLobbyAnnounce(byte[] data, IPEndPoint sender)
        {
            try
            {
                MemoryStream ms = new MemoryStream(data, 1, data.Length - 1);
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    ushort nameLen = br.ReadUInt16();
                    string name = Encoding.UTF8.GetString(br.ReadBytes(nameLen));
                    byte playerCount = br.ReadByte();
                    byte maxPlayers = br.ReadByte();

                    DiscoveredLobbies.Add(new LanLobbyInfo
                    {
                        Name = name,
                        IpAddress = sender.Address.ToString(),
                        PlayerCount = playerCount,
                        Port = DefaultPort
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LanManager] Failed to parse lobby announce: " + ex.Message);
            }
        }

        public void Disconnect()
        {
            if (!_running)
            {
                return;
            }

            // Send leave notification if client
            if (!IsHost && IsActive)
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    byte[] leaveData;
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)4); // MessageType.PlayerLeft
                        bw.Write(_localPlayerId);
                        bw.Flush();
                        leaveData = ms.ToArray();
                    }
                    IPEndPoint hostEp;
                    if (_playerEndpoints.TryGetValue(1, out hostEp))
                    {
                        _udpClient.Send(leaveData, leaveData.Length, hostEp);
                    }
                }
                catch
                {
                    // best effort
                }
            }

            // Send leave notifications to all clients if host
            if (IsHost && IsActive)
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    byte[] leaveData;
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write((byte)4); // PlayerLeft
                        bw.Write(_localPlayerId);
                        bw.Flush();
                        leaveData = ms.ToArray();
                    }
                    foreach (KeyValuePair<ulong, IPEndPoint> kvp in _playerEndpoints)
                    {
                        if (kvp.Key != _localPlayerId)
                        {
                            try { _udpClient.Send(leaveData, leaveData.Length, kvp.Value); }
                            catch { }
                        }
                    }
                }
                catch
                {
                    // best effort
                }
            }

            _running = false;

            // Wait for receive thread to exit
            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1500);
            }
            _receiveThread = null;

            // Close socket
            try
            {
                _udpClient?.Close();
            }
            catch { }
            _udpClient = null;

            // Clean up
            _players.Clear();
            _playerEndpoints.Clear();
            lock (_queueLock)
            {
                _packetQueue.Clear();
            }
            IsActive = false;
            IsHost = false;

            // Cleanup global state
            if (TogetherManager.lanLobby == Lobby)
            {
                TogetherManager.lanLobby = null;
                MultiLucySkelController.CleanupAllRemotePlayers();
                TogetherManager.players.Clear();
                TogetherManager.currentUser = null;
                VoteManager.Instance.AbortAllVotes();
            }

            Lobby = null;
            Debug.Log("[LanManager] Disconnected");
        }

        // ==================== Receive Loop ====================

        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref ep);

                    if (data == null || data.Length == 0)
                    {
                        continue;
                    }

                    byte msgType = data[0];

                    switch (msgType)
                    {
                        case 0: // Data
                            HandleDataPacket(data, ep);
                            break;
                        case 1: // Connect (host receives)
                            HandleConnect(data, ep);
                            break;
                        case 2: // Welcome (client receives)
                            HandleWelcome(data, ep);
                            break;
                        case 3: // PlayerJoined
                            HandlePlayerJoined(data);
                            break;
                        case 4: // PlayerLeft
                            HandlePlayerLeft(data);
                            break;
                        case 5: // Discover (host receives)
                            HandleDiscover(ep);
                            break;
                        case 7: // Fragment
                            HandleFragment(data, ep);
                            break;
                        // 6 = LobbyAnnounce, handled by DiscoverLobbies
                    }
                }
                catch (SocketException)
                {
                    if (_running)
                    {
                        Debug.LogWarning("[LanManager] Socket error in receive loop");
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        Debug.LogError("[LanManager] Receive error: " + ex.Message);
                    }
                }
            }
        }

        private void HandleFragment(byte[] data, IPEndPoint sender)
        {
            if (data.Length < 21) return;
            // Fragment format: [7][senderId:8][groupId:4][totalFrags:4][fragIndex:4][chunkData...]

            ulong senderId = BitConverter.ToUInt64(data, 1);
            int groupId = BitConverter.ToInt32(data, 9);
            int totalFrags = BitConverter.ToInt32(data, 13);
            int fragIndex = BitConverter.ToInt32(data, 17);

            if (fragIndex < 0 || fragIndex >= totalFrags || totalFrags > 256) return;

            int chunkSize = data.Length - 21;
            byte[] chunk = new byte[chunkSize];
            Buffer.BlockCopy(data, 21, chunk, 0, chunkSize);

            lock (_fragmentLock)
            {
                if (!_pendingFragments.ContainsKey(senderId))
                    _pendingFragments[senderId] = new Dictionary<int, PendingFragment>();

                var senderFrags = _pendingFragments[senderId];

                PendingFragment pending;
                if (!senderFrags.TryGetValue(groupId, out pending))
                {
                    pending = new PendingFragment
                    {
                        TotalFragments = totalFrags,
                        ReceivedMask = 0,
                        Fragments = new byte[totalFrags][],
                        LastReceivedTime = Time.time
                    };
                    senderFrags[groupId] = pending;
                }

                if (pending.TotalFragments != totalFrags) return; // mismatch
                pending.LastReceivedTime = Time.time;

                if ((pending.ReceivedMask & (1 << fragIndex)) != 0) return; // duplicate

                pending.Fragments[fragIndex] = chunk;
                pending.ReceivedMask |= (1 << fragIndex);

                // Check if all fragments received
                int expectedMask = (1 << totalFrags) - 1;
                if (pending.ReceivedMask == expectedMask)
                {
                    // Reassemble
                    int totalSize = 0;
                    for (int i = 0; i < totalFrags; i++)
                        totalSize += pending.Fragments[i].Length;

                    byte[] reassembled = new byte[totalSize];
                    int pos = 0;
                    for (int i = 0; i < totalFrags; i++)
                    {
                        Buffer.BlockCopy(pending.Fragments[i], 0, reassembled, pos, pending.Fragments[i].Length);
                        pos += pending.Fragments[i].Length;
                    }

                    senderFrags.Remove(groupId);

                    // Relay if host (the reassembled data starts with the original Data packet)
                    if (IsHost)
                    {
                        foreach (KeyValuePair<ulong, IPEndPoint> kvp in _playerEndpoints)
                        {
                            if (kvp.Key != _localPlayerId && kvp.Key != senderId)
                            {
                                try { _udpClient.Send(reassembled, reassembled.Length, kvp.Value); }
                                catch { }
                            }
                        }
                    }

                    // Process the reassembled data (same as HandleDataPacket)
                    if (reassembled.Length >= 9 && reassembled[0] == 0)
                    {
                        ulong originalSender = BitConverter.ToUInt64(reassembled, 1);
                        byte[] gameData = new byte[reassembled.Length - 9];
                        Buffer.BlockCopy(reassembled, 9, gameData, 0, gameData.Length);

                        RemotePlayer player;
                        if (!_players.TryGetValue(originalSender, out player))
                            player = new RemotePlayer(originalSender, "Player" + originalSender);

                        lock (_queueLock)
                        {
                            _packetQueue.Enqueue(new Packet(player, gameData));
                        }
                    }
                }
            }
        }

        private void CleanupStaleFragments()
        {
            float now = Time.time;
            if (now - _lastFragmentCleanupTime < 5f) return;
            _lastFragmentCleanupTime = now;

            lock (_fragmentLock)
            {
                List<ulong> emptySenders = new List<ulong>();
                foreach (var senderKvp in _pendingFragments)
                {
                    List<int> staleGroups = new List<int>();
                    foreach (var fragKvp in senderKvp.Value)
                    {
                        if (now - fragKvp.Value.LastReceivedTime > FragmentTimeout)
                            staleGroups.Add(fragKvp.Key);
                    }
                    foreach (int gid in staleGroups)
                        senderKvp.Value.Remove(gid);
                    if (senderKvp.Value.Count == 0)
                        emptySenders.Add(senderKvp.Key);
                }
                foreach (ulong sid in emptySenders)
                    _pendingFragments.Remove(sid);
            }
        }

        private void HandleDataPacket(byte[] data, IPEndPoint sender)
        {
            if (data.Length < 9)
            {
                return;
            }

            ulong senderId = BitConverter.ToUInt64(data, 1);
            byte[] gameData = new byte[data.Length - 9];
            Buffer.BlockCopy(data, 9, gameData, 0, gameData.Length);

            if (IsHost)
            {
                // Relay to all OTHER clients
                foreach (KeyValuePair<ulong, IPEndPoint> kvp in _playerEndpoints)
                {
                    if (kvp.Key != _localPlayerId && kvp.Key != senderId)
                    {
                        try
                        {
                            _udpClient.Send(data, data.Length, kvp.Value);
                        }
                        catch { }
                    }
                }
            }

            // Enqueue for local processing
            RemotePlayer player;
            if (!_players.TryGetValue(senderId, out player))
            {
                // Unknown sender - create a temporary player entry
                player = new RemotePlayer(senderId, "Player" + senderId);
            }

            lock (_queueLock)
            {
                _packetQueue.Enqueue(new Packet(player, gameData));
            }
        }

        private void HandleConnect(byte[] data, IPEndPoint sender)
        {
            if (!IsHost)
            {
                return;
            }

            if (PlayerCount >= MaxPlayers)
            {
                Debug.LogWarning("[LanManager] Connection rejected: lobby full");
                return;
            }

            try
            {
                MemoryStream ms = new MemoryStream(data, 1, data.Length - 1);
                string playerName;
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    ushort nameLen = br.ReadUInt16();
                    playerName = Encoding.UTF8.GetString(br.ReadBytes(nameLen));
                }

                ulong newId = (ulong)_nextPlayerId;
                _nextPlayerId++;

                _playerEndpoints[newId] = sender;
                RemotePlayer newPlayer = new RemotePlayer(newId, playerName);
                _players[newId] = newPlayer;

                // Send Welcome to new client
                MemoryStream welcomeMs = new MemoryStream();
                byte[] welcomeData;
                using (BinaryWriter welcomeBw = new BinaryWriter(welcomeMs, Encoding.UTF8))
                {
                    welcomeBw.Write((byte)2); // Welcome
                    welcomeBw.Write(newId);
                    byte[] hostNameBytes = Encoding.UTF8.GetBytes(_localPlayerName);
                    welcomeBw.Write((ushort)hostNameBytes.Length);
                    welcomeBw.Write(hostNameBytes);

                    // Write existing players (excluding the new player)
                    int otherCount = _players.Count - 1;
                    welcomeBw.Write((byte)otherCount);
                    foreach (KeyValuePair<ulong, RemotePlayer> kvp in _players)
                    {
                        if (kvp.Key != newId)
                        {
                            welcomeBw.Write(kvp.Key);
                            byte[] pNameBytes = Encoding.UTF8.GetBytes(kvp.Value.userName);
                            welcomeBw.Write((ushort)pNameBytes.Length);
                            welcomeBw.Write(pNameBytes);
                        }
                    }
                    welcomeBw.Flush();
                    welcomeData = welcomeMs.ToArray();
                }
                _udpClient.Send(welcomeData, welcomeData.Length, sender);

                // Broadcast PlayerJoined to all other clients
                MemoryStream joinMs = new MemoryStream();
                byte[] joinData;
                using (BinaryWriter joinBw = new BinaryWriter(joinMs, Encoding.UTF8))
                {
                    joinBw.Write((byte)3); // PlayerJoined
                    joinBw.Write(newId);
                    byte[] pNameBytes = Encoding.UTF8.GetBytes(playerName);
                    joinBw.Write((ushort)pNameBytes.Length);
                    joinBw.Write(pNameBytes);
                    joinBw.Flush();
                    joinData = joinMs.ToArray();
                }
                foreach (KeyValuePair<ulong, IPEndPoint> kvp in _playerEndpoints)
                {
                    if (kvp.Key != _localPlayerId && kvp.Key != newId)
                    {
                        try
                        {
                            _udpClient.Send(joinData, joinData.Length, kvp.Value);
                        }
                        catch { }
                    }
                }

                // Update global player list
                TogetherManager.players = GetPlayers();
                VoteManager.Instance.SyncPlayersWithLobby();

                Debug.Log("[LanManager] Player joined: '" + playerName + "' (ID: " + newId + ")");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LanManager] HandleConnect error: " + ex.Message);
            }
        }

        private void HandleWelcome(byte[] data, IPEndPoint hostEp)
        {
            try
            {
                MemoryStream ms = new MemoryStream(data, 1, data.Length - 1);
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    _localPlayerId = br.ReadUInt64();
                    ushort hostNameLen = br.ReadUInt16();
                    string hostName = Encoding.UTF8.GetString(br.ReadBytes(hostNameLen));

                    byte otherCount = br.ReadByte();

                    // Add host
                    RemotePlayer hostPlayer = new RemotePlayer(1, hostName);
                    _players[1] = hostPlayer;

                    // Add myself
                    RemotePlayer selfPlayer = new RemotePlayer(_localPlayerId, _localPlayerName);
                    _players[_localPlayerId] = selfPlayer;

                    // Add other existing players
                    for (int i = 0; i < otherCount; i++)
                    {
                        ulong pid = br.ReadUInt64();
                        ushort nLen = br.ReadUInt16();
                        string pname = Encoding.UTF8.GetString(br.ReadBytes(nLen));

                        if (pid != _localPlayerId && pid != 1)
                        {
                            _players[pid] = new RemotePlayer(pid, pname);
                            // Also store endpoint for potential direct communication
                            _playerEndpoints[pid] = hostEp; // relay via host
                        }
                    }
                }

                IsActive = true;
                Lobby = new LanLobby(this);

                // Setup global state
                TogetherManager.lanLobby = Lobby;
                TogetherManager.currentUser = _players[_localPlayerId];
                TogetherManager.players = GetPlayers();
                VoteManager.Instance.SyncPlayersWithLobby();

                Debug.Log("[LanManager] Connected! My ID: " + _localPlayerId + ", Players: " + _players.Count);
            }
            catch (Exception ex)
            {
                Debug.LogError("[LanManager] HandleWelcome error: " + ex.Message);
            }
        }

        private void HandlePlayerJoined(byte[] data)
        {
            try
            {
                MemoryStream ms = new MemoryStream(data, 1, data.Length - 1);
                ulong id;
                string name;
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
                {
                    id = br.ReadUInt64();
                    ushort nameLen = br.ReadUInt16();
                    name = Encoding.UTF8.GetString(br.ReadBytes(nameLen));
                }

                _players[id] = new RemotePlayer(id, name);
                TogetherManager.players = GetPlayers();
                VoteManager.Instance.SyncPlayersWithLobby();

                Debug.Log("[LanManager] Remote player joined: '" + name + "'");
            }
            catch (Exception ex)
            {
                Debug.LogError("[LanManager] HandlePlayerJoined error: " + ex.Message);
            }
        }

        private void HandlePlayerLeft(byte[] data)
        {
            if (data.Length < 9)
            {
                return;
            }

            ulong id = BitConverter.ToUInt64(data, 1);

            if (_players.ContainsKey(id))
            {
                string name = _players[id].userName;
                Debug.Log("[LanManager] Player left: '" + name + "' (ID: " + id + ")");

                MultiLucySkelController.CleanupRemotePlayer(id);
                _players.Remove(id);
                _playerEndpoints.Remove(id);
                TogetherManager.players = GetPlayers();
                VoteManager.Instance.SyncPlayersWithLobby();
            }
        }

        private void HandleDiscover(IPEndPoint sender)
        {
            if (!IsHost)
            {
                return;
            }

            try
            {
                MemoryStream ms = new MemoryStream();
                byte[] announceData;
                using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write((byte)6); // LobbyAnnounce
                    byte[] nameBytes = Encoding.UTF8.GetBytes(_localPlayerName);
                    bw.Write((ushort)nameBytes.Length);
                    bw.Write(nameBytes);
                    bw.Write((byte)PlayerCount);
                    bw.Write((byte)MaxPlayers);
                    bw.Flush();
                    announceData = ms.ToArray();
                }

                _udpClient.Send(announceData, announceData.Length, sender);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LanManager] HandleDiscover error: " + ex.Message);
            }
        }
    }
}
