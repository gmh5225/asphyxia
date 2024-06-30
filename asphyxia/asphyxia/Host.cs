//------------------------------------------------------------
// Onryo
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
using System.Collections.Generic;
using System.Threading;
#endif
using System.Net.Sockets;
using NanoSockets;
using static asphyxia.Settings;
using static System.Runtime.InteropServices.Marshal;
using static KCP.KCPBASIC;

#pragma warning disable CS8632

// ReSharper disable PossibleNullReferenceException

namespace asphyxia
{
    /// <summary>
    ///     Host
    /// </summary>
    public sealed unsafe class Host : IDisposable
    {
        /// <summary>
        ///     Socket
        /// </summary>
        private readonly NanoSocket _socket = new();

        /// <summary>
        ///     Buffer
        /// </summary>
        private byte* _receiveBuffer;

        /// <summary>
        ///     Buffer
        /// </summary>
        private byte* _sendBuffer;

        /// <summary>
        ///     Max peers
        /// </summary>
        private int _maxPeers;

        /// <summary>
        ///     Id
        /// </summary>
        private uint _id;

        /// <summary>
        ///     Id pool
        /// </summary>
        private readonly Queue<uint> _idPool = new(MAX_PEERS);

        /// <summary>
        ///     Peers
        /// </summary>
        private readonly Dictionary<int, Peer> _peers = new(MAX_PEERS);

        /// <summary>
        ///     Sentinel
        /// </summary>
        private Peer? _sentinel;

        /// <summary>
        ///     Outgoing commands
        /// </summary>
        private readonly Queue<OutgoingCommand> _outgoingCommands = new(MAX_EVENTS);

        /// <summary>
        ///     NetworkEvents
        /// </summary>
        private readonly Queue<NetworkEvent> _networkEvents = new(MAX_EVENTS);

        /// <summary>
        ///     Remote endPoint
        /// </summary>
        private NanoIPEndPoint _remoteEndPoint;

        /// <summary>
        ///     Peer
        /// </summary>
        private Peer? _peer;

        /// <summary>
        ///     Poll interval
        /// </summary>
        private int _pollInterval;

        /// <summary>
        ///     State lock
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsSet => _socket.IsSet;

        /// <summary>
        ///     Dispose
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (!IsSet)
                    return;
                _socket.Close();
                FreeHGlobal((nint)_receiveBuffer);
                FreeHGlobal((nint)_sendBuffer);
                _maxPeers = 0;
                _id = 0;
                _idPool.Clear();
                _peers.Clear();
                _sentinel = null;
                while (_outgoingCommands.TryDequeue(out var outgoingCommand))
                    outgoingCommand.Dispose();
                while (_networkEvents.TryDequeue(out var networkEvent))
                {
                    if (networkEvent.EventType != NetworkEventType.Data)
                        continue;
                    networkEvent.Packet.Dispose();
                }

                _peer = null;
                _pollInterval = 0;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///     Destructure
        /// </summary>
        ~Host() => Dispose();

        /// <summary>
        ///     Create
        /// </summary>
        /// <param name="maxPeers">Max peers</param>
        /// <param name="port">Port</param>
        public void Create(int maxPeers, ushort port = 0)
        {
            lock (_lock)
            {
                if (IsSet)
                    throw new InvalidOperationException("Host has created.");
                if (maxPeers < 0 || maxPeers > MAX_PEERS)
                    throw new ArgumentOutOfRangeException(nameof(maxPeers));
                _socket.Create(SOCKET_BUFFER_SIZE, SOCKET_BUFFER_SIZE);
                var localEndPoint = Socket.OSSupportsIPv6 ? NanoIPEndPoint.IPv6Any(port) : NanoIPEndPoint.Any(port);
                try
                {
                    _socket.Bind(ref localEndPoint);
                }
                catch
                {
                    _socket.Dispose();
                    throw;
                }

                _socket.DontFragment = true;
                _socket.Blocking = false;
                _receiveBuffer = (byte*)AllocHGlobal(BUFFER_SIZE);
                _sendBuffer = (byte*)AllocHGlobal(BUFFER_SIZE);
                _maxPeers = maxPeers;
                GC.ReRegisterForFinalize(this);
            }
        }

        /// <summary>
        ///     Check events
        /// </summary>
        /// <param name="networkEvent">NetworkEvent</param>
        /// <returns>Checked</returns>
        public bool CheckEvents(out NetworkEvent networkEvent) => _networkEvents.TryDequeue(out networkEvent);

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="ipAddress">IPAddress</param>
        /// <param name="port">Port</param>
        public Peer? Connect(string ipAddress, ushort port) => !IsSet ? null : ConnectInternal(NanoIPEndPoint.Create(ipAddress, port));

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="remoteEndPoint">Remote endPoint</param>
        public Peer? Connect(NanoIPEndPoint remoteEndPoint) => !IsSet ? null : ConnectInternal(remoteEndPoint);

        /// <summary>
        ///     Connect
        /// </summary>
        /// <param name="remoteEndPoint">Remote endPoint</param>
        private Peer? ConnectInternal(NanoIPEndPoint remoteEndPoint)
        {
            var hashCode = remoteEndPoint.GetHashCode();
            if (_peers.TryGetValue(hashCode, out var peer))
                return peer;
            if (_peers.Count >= _maxPeers)
                return null;
            peer = new Peer(this, _idPool.TryDequeue(out var id) ? id : _id++, remoteEndPoint, _sendBuffer, State.Connecting);
            _peers[hashCode] = peer;
            if (_sentinel == null)
            {
                _sentinel = peer;
            }
            else
            {
                _sentinel.Previous = peer;
                peer.Next = _sentinel;
                _sentinel = peer;
            }

            _sendBuffer[0] = (byte)Header.Connect;
            peer.RawSend(_sendBuffer, 1);
            return peer;
        }

        /// <summary>
        ///     Ping
        /// </summary>
        /// <param name="remoteEndPoint">Remote endPoint</param>
        public void Ping(NanoIPEndPoint remoteEndPoint)
        {
            if (!IsSet)
                return;
            _sendBuffer[0] = (byte)Header.Ping;
            _socket.Send(_sendBuffer, 1, &remoteEndPoint);
        }

        /// <summary>
        ///     Service
        /// </summary>
        public void Service()
        {
            if (_socket.Poll(_pollInterval))
            {
                _pollInterval = 0;
                var received = 0;
                var remoteEndPoint = _remoteEndPoint;
                while (received < MAX_EVENTS && _socket.Receive(_receiveBuffer, BUFFER_SIZE, out var count, ref _remoteEndPoint))
                {
                    try
                    {
                        if (count < OVERHEAD)
                        {
                            if (count == 4 && _receiveBuffer[0] == (byte)Header.Disconnect && _receiveBuffer[1] == (byte)Header.DisconnectAcknowledge && _receiveBuffer[2] == (byte)Header.Disconnect && _receiveBuffer[3] == (byte)Header.DisconnectAcknowledge)
                            {
                                if (_peer == null || _remoteEndPoint != remoteEndPoint)
                                {
                                    if (_peers.TryGetValue(_remoteEndPoint.GetHashCode(), out _peer))
                                        _peer.DisconnectInternal();
                                }
                                else
                                {
                                    _peer.DisconnectInternal();
                                }
                            }

                            continue;
                        }

                        if (_peer == null || _remoteEndPoint != remoteEndPoint)
                        {
                            var hashCode = _remoteEndPoint.GetHashCode();
                            if (!_peers.TryGetValue(hashCode, out _peer))
                            {
                                if (count != 25 || _receiveBuffer[24] != (byte)Header.Connect || _peers.Count >= _maxPeers)
                                    continue;
                                _peer = new Peer(this, _idPool.TryDequeue(out var id) ? id : _id++, _remoteEndPoint, _sendBuffer);
                                _peers[hashCode] = _peer;
                                if (_sentinel == null)
                                {
                                    _sentinel = _peer;
                                }
                                else
                                {
                                    _sentinel.Previous = _peer;
                                    _peer.Next = _sentinel;
                                    _sentinel = _peer;
                                }
                            }
                        }

                        _peer.Input(_receiveBuffer, count);
                    }
                    finally
                    {
                        received++;
                        remoteEndPoint = _remoteEndPoint;
                        Thread.SpinWait(1);
                    }
                }
            }
            else
            {
                _pollInterval = 1;
                Thread.SpinWait(100);
            }

            var node = _sentinel;
            while (node != null)
            {
                node.Service(_receiveBuffer);
                Thread.SpinWait(1);
                node = node.Next;
            }
        }

        /// <summary>
        ///     Flush
        /// </summary>
        public void Flush()
        {
            while (_outgoingCommands.TryDequeue(out var outgoingCommand))
            {
                _socket.Send(outgoingCommand.Data, outgoingCommand.Length, outgoingCommand.IPEndPoint);
                Thread.SpinWait(1);
                outgoingCommand.Dispose();
            }
        }

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="networkEvent">NetworkEvent</param>
        internal void Insert(in NetworkEvent networkEvent) => _networkEvents.Enqueue(networkEvent);

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="outgoingCommand">OutgoingCommand</param>
        internal void Insert(in OutgoingCommand outgoingCommand) => _outgoingCommands.Enqueue(outgoingCommand);

        /// <summary>
        ///     Remove
        /// </summary>
        /// <param name="hashCode">HashCode</param>
        /// <param name="peer">Peer</param>
        internal void Remove(int hashCode, Peer peer)
        {
            if (_peer == peer)
                _peer = null;
            _idPool.Enqueue(peer.Id);
            _peers.Remove(hashCode);
            if (peer.Previous != null)
                peer.Previous.Next = peer.Next;
            else
                _sentinel = peer.Next;
            if (peer.Next != null)
                peer.Next.Previous = peer.Previous;
        }
    }
}