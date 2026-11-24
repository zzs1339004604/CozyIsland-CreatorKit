using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CozyIsland.Modding;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CozyMods.Appearance
{
    internal struct AppearanceRoomMessage : NetworkMessage
    {
        public byte Version, Kind;
        public string Key, Value;
        public int Total, Offset;
        public ArraySegment<byte> Bytes;
    }

    internal sealed class RoomSync : IDisposable
    {
        internal const int MaxPackBytes = 8 * 1024 * 1024;
        internal const string EmptySelection = "|||||";
        private const int ChunkBytes = 8 * 1024;
        private const int MaxPending = 8;
        private const int MaxTransfers = 24;
        private const float RequestTimeout = 15f;
        private const float RetryDelay = 5f;
        private const byte ProtocolVersion = 1;

        private enum Kind : byte { Hello, Welcome, Select, Snapshot, Request, Chunk, Unavailable }

        private sealed class Relay
        {
            public readonly HashSet<NetworkConnectionToClient> Waiters = new HashSet<NetworkConnectionToClient>();
            public readonly HashSet<NetworkConnectionToClient> Tried = new HashSet<NetworkConnectionToClient>();
            public NetworkConnectionToClient Source;
            public float Expires;
        }

        private sealed class Transfer
        {
            public NetworkConnection Destination;
            public string Digest;
            public byte[] Bytes;
            public int Offset;
        }

        private sealed class Incoming
        {
            public byte[] Bytes;
            public int Offset;
        }

        private static RoomSync markerReceiver;
        private readonly CozyModContext context;
        private readonly Func<string, byte[]> readPack;
        private readonly Func<string, byte[], bool> acceptPack;
        private readonly Dictionary<string, string> players = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<NetworkConnectionToClient> announced = new HashSet<NetworkConnectionToClient>();
        private readonly Dictionary<NetworkConnectionToClient, string> peers = new Dictionary<NetworkConnectionToClient, string>();
        private readonly Dictionary<string, Relay> relays = new Dictionary<string, Relay>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> requests = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, Incoming> incoming = new Dictionary<string, Incoming>(StringComparer.Ordinal);
        private readonly Queue<Transfer> outgoing = new Queue<Transfer>();
        private NetworkConnection markerConnection, clientConnection;
        private string lastLocalSelection, lastLocalActor;
        private bool autoReceive, hosting, sentHello, welcomed;

        internal IReadOnlyDictionary<string, string> Players => players;
        internal bool Connected => hosting || welcomed;

        internal RoomSync(CozyModContext context, Func<string, byte[]> readPack, Func<string, byte[], bool> acceptPack, bool autoReceive)
        {
            this.context = context;
            this.readPack = readPack;
            this.acceptPack = acceptPack;
            this.autoReceive = autoReceive;
            Writer<AppearanceRoomMessage>.write = WriteMessage;
            Reader<AppearanceRoomMessage>.read = ReadMessage;

            // The host sends an empty native mod catalogue to advertise this optional protocol.
            // The unmodified game's handler treats an empty catalogue as a no-op.
            var compatibility = typeof(CozyModPrefabCatalogMessage).Assembly.GetType("CozyIsland.Modding.CozyModNetworkCompatibility");
            var receiver = AccessTools.Method(compatibility, "ReceiveCatalog", new[] { typeof(CozyModPrefabCatalogMessage) });
            context.Harmony.Patch(receiver, postfix: new HarmonyMethod(typeof(RoomSync), nameof(ObserveCatalog)));
            markerReceiver = this;
        }

        private static void ObserveCatalog(CozyModPrefabCatalogMessage __0)
        {
            if (markerReceiver != null && !NetworkServer.active && __0.assetIds?.Length == 0 && __0.prefabIds?.Length == 0)
                markerReceiver.markerConnection = NetworkClient.connection;
        }

        internal void SetAutoReceive(bool value)
        {
            autoReceive = value;
            if (value) return;
            foreach (var pair in relays)
                foreach (var waiter in pair.Value.Waiters.Where(IsLive))
                    waiter.Send(Packet(Kind.Unavailable, pair.Key));
            relays.Clear();
            requests.Clear();
            incoming.Clear();
        }

        internal void Tick(string selection)
        {
            UpdateConnection();
            if (hosting)
            {
                HostTick(selection);
            }
            else if (clientConnection != null)
            {
                if (!sentHello && ReferenceEquals(markerConnection, clientConnection) && clientConnection.isAuthenticated)
                {
                    sentHello = true;
                    clientConnection.Send(Packet(Kind.Hello));
                }
                var actor = ActorName(NetworkClient.localPlayer);
                if (welcomed && actor != null && (actor != lastLocalActor || selection != lastLocalSelection))
                {
                    clientConnection.Send(Packet(Kind.Select, value: selection));
                    lastLocalActor = actor;
                    lastLocalSelection = selection;
                }
            }

            var now = Time.unscaledTime;
            foreach (var digest in requests.Keys.ToArray())
            {
                if (Advertised(digest) || requests[digest] > now) continue;
                requests.Remove(digest);
                incoming.Remove(digest);
            }
            PumpTransfers();
        }

        private void UpdateConnection()
        {
            if (NetworkServer.active != hosting)
            {
                if (hosting) NetworkServer.UnregisterHandler<AppearanceRoomMessage>();
                hosting = NetworkServer.active;
                ResetRoom();
                if (hosting) NetworkServer.RegisterHandler<AppearanceRoomMessage>(ReceiveFromPeer);
            }

            var connection = !hosting && NetworkClient.isConnected ? NetworkClient.connection : null;
            if (ReferenceEquals(connection, clientConnection)) return;
            if (clientConnection != null) NetworkClient.UnregisterHandler<AppearanceRoomMessage>();
            clientConnection = connection;
            ResetRoom();
            if (clientConnection != null) NetworkClient.RegisterHandler<AppearanceRoomMessage>(ReceiveFromHost);
        }

        private void HostTick(string selection)
        {
            foreach (var dead in announced.Where(connection => !IsLive(connection)).ToArray())
                announced.Remove(dead);
            foreach (var dead in peers.Keys.Where(connection => !IsLive(connection)).ToArray())
            {
                var actor = peers[dead];
                peers.Remove(dead);
                if (actor != null) RemoveActor(actor);
            }
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection is LocalConnectionToClient || !connection.isAuthenticated || !connection.isReady
                    || !announced.Add(connection)) continue;
                connection.Send(new CozyModPrefabCatalogMessage { assetIds = Array.Empty<uint>(), prefabIds = Array.Empty<long>() });
            }

            var localActor = ActorName(NetworkClient.localPlayer);
            if (localActor != lastLocalActor && lastLocalActor != null) RemoveActor(lastLocalActor);
            if (localActor != null && (localActor != lastLocalActor || selection != lastLocalSelection))
            {
                lastLocalSelection = selection;
                SetActor(localActor, selection);
            }
            lastLocalActor = localActor;

            foreach (var pair in relays.ToArray())
                if (!IsLive(pair.Value.Source) || pair.Value.Expires <= Time.unscaledTime)
                    FetchNext(pair.Key, pair.Value);
        }

        // These are slot keys in Body, Hat, Cloth, Shoe, Glove, Knee order.
        internal static string[] KeysIn(string selection)
        {
            if (selection == null || selection.Length > 6 * 129 + 5) return null;
            var keys = selection.Split('|');
            if (keys.Length != 6) return null;
            foreach (var key in keys)
            {
                if (key.Length == 0) continue;
                if (key.Length < 66 || key.Length > 129 || key[64] != '/' || !IsDigest(key.Substring(0, 64))) return null;
                for (int i = 65; i < key.Length; i++)
                {
                    char c = key[i];
                    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                        || c == '-' || c == '_' || c == '.') continue;
                    return null;
                }
            }
            return keys;
        }

        private static bool IsDigest(string digest)
        {
            if (digest?.Length != 64) return false;
            foreach (var c in digest)
                if (!(c >= 'a' && c <= 'f') && !(c >= '0' && c <= '9')) return false;
            return true;
        }

        private static bool HasDigest(string selection, string digest)
        {
            var index = selection.IndexOf(digest + "/", StringComparison.Ordinal);
            return index == 0 || (index > 0 && selection[index - 1] == '|');
        }

        private bool Advertised(string digest) => players.Values.Any(selection => HasDigest(selection, digest));

        internal void RequestPack(string digest)
        {
            if (!Connected || !IsDigest(digest) || !Advertised(digest)) return;
            var now = Time.unscaledTime;
            if (requests.TryGetValue(digest, out var waitUntil) && waitUntil > now) return;
            if (requests.Values.Count(time => time > now) >= MaxPending) return;
            requests[digest] = now + RequestTimeout;
            incoming.Remove(digest);
            if (hosting)
            {
                if (relays.TryGetValue(digest, out var active))
                    requests[digest] = active.Expires;
                else if (relays.Count < MaxPending)
                    BeginRelay(digest, null);
                else
                    requests[digest] = now + RetryDelay;
            }
            else clientConnection.Send(Packet(Kind.Request, digest));
        }

        private void ReceiveFromPeer(NetworkConnectionToClient connection, AppearanceRoomMessage message)
        {
            if (message.Version != ProtocolVersion || !announced.Contains(connection)) return;
            var kind = (Kind)message.Kind;
            if (kind == Kind.Hello)
            {
                if (peers.ContainsKey(connection)) return;
                peers.Add(connection, null);
                connection.Send(Packet(Kind.Welcome));
                foreach (var pair in players) connection.Send(Packet(Kind.Snapshot, pair.Key, pair.Value));
                context.Log("Appearance peer connected.");
                return;
            }
            if (!peers.TryGetValue(connection, out var owner)) return;
            if (kind == Kind.Select)
            {
                var actor = ActorName(connection.identity);
                if (actor == null || actor == lastLocalActor || KeysIn(message.Value) == null ||
                    peers.Any(pair => pair.Key != connection && pair.Value == actor)) return;
                if (owner != null && owner != actor) RemoveActor(owner);
                peers[connection] = actor;
                SetActor(actor, message.Value);
                return;
            }
            if (!IsDigest(message.Key)) return;

            var digest = message.Key;
            if (kind == Kind.Request && Advertised(digest))
            {
                if (relays.TryGetValue(digest, out var pending)) pending.Waiters.Add(connection);
                else
                {
                    var bytes = readPack(digest);
                    if (bytes != null) QueueTransfer(connection, digest, bytes);
                    else if (autoReceive && relays.Count < MaxPending) BeginRelay(digest, connection);
                    else connection.Send(Packet(Kind.Unavailable, digest));
                }
                return;
            }

            if (!relays.TryGetValue(digest, out var relay) || relay.Source != connection) return;
            if (kind == Kind.Unavailable) FetchNext(digest, relay);
            else if (kind == Kind.Chunk)
            {
                if (!ReceiveChunk(message, out var bytes))
                {
                    FetchNext(digest, relay);
                    return;
                }
                relay.Expires = Time.unscaledTime + RequestTimeout;
                if (bytes == null) return;
                if (Accept(digest, bytes))
                {
                    relays.Remove(digest);
                    requests.Remove(digest);
                    foreach (var waiter in relay.Waiters.Where(IsLive)) QueueTransfer(waiter, digest, bytes);
                }
                else FetchNext(digest, relay);
            }
        }

        private void ReceiveFromHost(AppearanceRoomMessage message)
        {
            if (message.Version != ProtocolVersion || clientConnection == null) return;
            var kind = (Kind)message.Kind;
            if (kind == Kind.Welcome && sentHello)
            {
                welcomed = true;
                context.Log("Appearance room joined.");
                return;
            }
            if (!welcomed) return;
            if (kind == Kind.Snapshot && ValidActor(message.Key))
            {
                if (message.Value == null) players.Remove(message.Key);
                else if (KeysIn(message.Value) != null) players[message.Key] = message.Value;
                return;
            }
            if (!IsDigest(message.Key)) return;

            var digest = message.Key;
            if (kind == Kind.Request)
            {
                if (lastLocalSelection != null && HasDigest(lastLocalSelection, digest))
                    QueueTransfer(clientConnection, digest, readPack(digest));
                else clientConnection.Send(Packet(Kind.Unavailable, digest));
                return;
            }
            var now = Time.unscaledTime;
            if (!requests.TryGetValue(digest, out var timeout) || timeout <= now) return;
            if (kind == Kind.Unavailable)
            {
                incoming.Remove(digest);
                requests[digest] = now + RetryDelay;
            }
            else if (kind == Kind.Chunk)
            {
                if (!ReceiveChunk(message, out var bytes))
                {
                    incoming.Remove(digest);
                    requests[digest] = now + RetryDelay;
                    return;
                }
                if (bytes == null) requests[digest] = now + RequestTimeout;
                else if (Accept(digest, bytes)) requests.Remove(digest);
                else requests[digest] = now + RetryDelay;
            }
        }

        private void BeginRelay(string digest, NetworkConnectionToClient waiter)
        {
            var relay = new Relay();
            if (waiter != null) relay.Waiters.Add(waiter);
            relays.Add(digest, relay);
            FetchNext(digest, relay);
        }

        private void FetchNext(string digest, Relay relay)
        {
            incoming.Remove(digest);
            foreach (var pair in peers)
            {
                if (pair.Value == null || !IsLive(pair.Key) || !players.TryGetValue(pair.Value, out var selection) ||
                    !HasDigest(selection, digest) || !relay.Tried.Add(pair.Key)) continue;
                relay.Source = pair.Key;
                relay.Expires = Time.unscaledTime + RequestTimeout;
                pair.Key.Send(Packet(Kind.Request, digest));
                return;
            }
            relays.Remove(digest);
            requests[digest] = Time.unscaledTime + RetryDelay;
            foreach (var waiter in relay.Waiters.Where(IsLive)) waiter.Send(Packet(Kind.Unavailable, digest));
        }

        private bool ReceiveChunk(AppearanceRoomMessage message, out byte[] complete)
        {
            complete = null;
            if (message.Total < 1 || message.Total > MaxPackBytes || message.Offset < 0 ||
                message.Bytes.Count < 1 || message.Bytes.Count > ChunkBytes || message.Offset > message.Total - message.Bytes.Count) return false;

            if (!incoming.TryGetValue(message.Key, out var transfer))
            {
                if (message.Offset != 0 || incoming.Count >= MaxPending) return false;
                transfer = new Incoming { Bytes = new byte[message.Total] };
                incoming.Add(message.Key, transfer);
            }
            if (transfer.Bytes.Length != message.Total || transfer.Offset != message.Offset) return false;
            Buffer.BlockCopy(message.Bytes.Array, message.Bytes.Offset, transfer.Bytes, transfer.Offset, message.Bytes.Count);
            transfer.Offset += message.Bytes.Count;
            if (transfer.Offset == transfer.Bytes.Length)
            {
                incoming.Remove(message.Key);
                complete = transfer.Bytes;
            }
            return true;
        }

        private bool Accept(string digest, byte[] bytes)
        {
            if (!acceptPack(digest, bytes)) return false;
            context.Log($"Received appearance pack {digest.Substring(0, 12)} ({bytes.Length} bytes).");
            return true;
        }

        private void QueueTransfer(NetworkConnection destination, string digest, byte[] bytes)
        {
            if (outgoing.Any(transfer => transfer.Destination == destination && transfer.Digest == digest)) return;
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaxPackBytes || outgoing.Count >= MaxTransfers)
            {
                destination.Send(Packet(Kind.Unavailable, digest));
                return;
            }
            outgoing.Enqueue(new Transfer { Destination = destination, Digest = digest, Bytes = bytes });
        }

        private void PumpTransfers()
        {
            if (outgoing.Count == 0 || Transport.active == null) return;
            var packetBytes = Math.Min(ChunkBytes, NetworkMessages.MaxContentSize(Channels.Reliable) - 160);
            for (var i = 0; i < 6 && outgoing.Count > 0; i++)
            {
                var transfer = outgoing.Dequeue();
                if (transfer.Destination is NetworkConnectionToClient peer ? !IsLive(peer)
                    : !ReferenceEquals(transfer.Destination, clientConnection)) continue;
                if (packetBytes <= 0)
                {
                    transfer.Destination.Send(Packet(Kind.Unavailable, transfer.Digest));
                    continue;
                }
                var count = Math.Min(packetBytes, transfer.Bytes.Length - transfer.Offset);
                var message = Packet(Kind.Chunk, transfer.Digest);
                message.Total = transfer.Bytes.Length;
                message.Offset = transfer.Offset;
                message.Bytes = new ArraySegment<byte>(transfer.Bytes, transfer.Offset, count);
                transfer.Destination.Send(message);
                transfer.Offset += count;
                if (transfer.Offset < transfer.Bytes.Length) outgoing.Enqueue(transfer);
            }
        }

        private void SetActor(string actor, string selection)
        {
            if (players.TryGetValue(actor, out var old) && old == selection) return;
            players[actor] = selection;
            Broadcast(Packet(Kind.Snapshot, actor, selection));
        }

        private void RemoveActor(string actor)
        {
            if (players.Remove(actor)) Broadcast(Packet(Kind.Snapshot, actor));
        }

        private void Broadcast(AppearanceRoomMessage message)
        {
            foreach (var connection in peers.Keys)
                if (IsLive(connection)) connection.Send(message);
        }

        private static bool IsLive(NetworkConnectionToClient conn) =>
            conn != null && NetworkServer.connections.TryGetValue(conn.connectionId, out var live) && ReferenceEquals(conn, live);

        private static string ActorName(NetworkIdentity identity)
        {
            if (identity == null) return null;
            var actor = identity.GetComponent<NetworkPlayerHandler>()?.playerName;
            return ValidActor(actor) ? actor : null;
        }

        private static bool ValidActor(string name) => !string.IsNullOrEmpty(name) && name.Length <= 128;

        private static AppearanceRoomMessage Packet(Kind kind, string key = null, string value = null) =>
            new AppearanceRoomMessage { Version = ProtocolVersion, Kind = (byte)kind, Key = key, Value = value };

        private static void WriteMessage(NetworkWriter writer, AppearanceRoomMessage message)
        {
            writer.WriteByte(message.Version);
            writer.WriteByte(message.Kind);
            writer.WriteString(message.Key);
            writer.WriteString(message.Value);
            writer.WriteInt(message.Total);
            writer.WriteInt(message.Offset);
            writer.WriteBytesAndSize(message.Bytes.Array, message.Bytes.Offset, message.Bytes.Count);
        }

        private static AppearanceRoomMessage ReadMessage(NetworkReader reader) => new AppearanceRoomMessage
        {
            Version = reader.ReadByte(),
            Kind = reader.ReadByte(),
            Key = reader.ReadString(),
            Value = reader.ReadString(),
            Total = reader.ReadInt(),
            Offset = reader.ReadInt(),
            Bytes = reader.ReadArraySegmentAndSize()
        };

        private void ResetRoom()
        {
            players.Clear();
            announced.Clear();
            peers.Clear();
            relays.Clear();
            requests.Clear();
            incoming.Clear();
            outgoing.Clear();
            sentHello = welcomed = false;
            lastLocalSelection = lastLocalActor = null;
        }

        public void Dispose()
        {
            if (ReferenceEquals(markerReceiver, this)) markerReceiver = null;
            NetworkServer.UnregisterHandler<AppearanceRoomMessage>();
            NetworkClient.UnregisterHandler<AppearanceRoomMessage>();
            ResetRoom();
            hosting = false;
            clientConnection = markerConnection = null;
        }
    }
}
