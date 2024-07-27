//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif
using System.Net;
using KCP;
using static asphyxia.Settings;
using static asphyxia.Time;
using static asphyxia.PacketFlag;
using static asphyxia.PeerState;
using static asphyxia.Header;
using static System.Runtime.CompilerServices.Unsafe;

#pragma warning disable CS8602
#pragma warning disable CS8632

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable PossibleNullReferenceException

namespace asphyxia
{
    /// <summary>
    ///     Peer
    /// </summary>
    public sealed unsafe class Peer : IKcpCallback
    {
        /// <summary>
        ///     Previous
        /// </summary>
        internal Peer? Previous;

        /// <summary>
        ///     Next
        /// </summary>
        internal Peer? Next;

        /// <summary>
        ///     Host
        /// </summary>
        private readonly Host _host;

        /// <summary>
        ///     Id
        /// </summary>
        public readonly uint Id;

        /// <summary>
        ///     IPEndPoint
        /// </summary>
        public readonly IPEndPoint IPEndPoint;

        /// <summary>
        ///     Conversation id
        /// </summary>
        private readonly byte _conversationId;

        /// <summary>
        ///     Kcp
        /// </summary>
        private readonly Kcp _kcp;

        /// <summary>
        ///     Send buffer
        /// </summary>
        private readonly byte* _sendBuffer;

        /// <summary>
        ///     Flush buffer
        /// </summary>
        private readonly byte* _flushBuffer;

        /// <summary>
        ///     Last send sequence number
        /// </summary>
        private ushort _lastSendSequence;

        /// <summary>
        ///     Last receive sequence number
        /// </summary>
        private ushort _lastReceiveSequence = ushort.MaxValue;

        /// <summary>
        ///     Last send timestamp
        /// </summary>
        private uint _lastSendTimestamp;

        /// <summary>
        ///     Last receive timestamp
        /// </summary>
        private uint _lastReceiveTimestamp;

        /// <summary>
        ///     Peer state
        /// </summary>
        private PeerState _state;

        /// <summary>
        ///     Disconnecting
        /// </summary>
        private bool _disconnecting;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <param name="host">Host</param>
        /// <param name="id">Id</param>
        /// <param name="ipEndPoint">IPEndPoint</param>
        /// <param name="sendBuffer">Send buffer</param>
        /// <param name="flushBuffer">Flush buffer</param>
        /// <param name="state">State</param>
        internal Peer(byte conversationId, Host host, uint id, EndPoint ipEndPoint, byte* sendBuffer, byte* flushBuffer, PeerState state = PeerState.None)
        {
            _host = host;
            Id = id;
            _conversationId = conversationId;
            IPEndPoint = (IPEndPoint)ipEndPoint;
            _sendBuffer = sendBuffer;
            _flushBuffer = flushBuffer;
            _state = state;
            _kcp = new Kcp(conversationId, this);
            _kcp.SetNoDelay(KCP_NO_DELAY, KCP_FLUSH_INTERVAL, KCP_FAST_RESEND, KCP_NO_CONGESTION_WINDOW);
            _kcp.SetWindowSize(KCP_WINDOW_SIZE, KCP_WINDOW_SIZE);
            _kcp.SetMtu(KCP_MAXIMUM_TRANSMISSION_UNIT);
            var current = Current;
            _lastSendTimestamp = current;
            _lastReceiveTimestamp = current;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsSet => _kcp.IsSet;

        /// <summary>
        ///     Peer state
        /// </summary>
        public PeerState State => _state;

        /// <summary>
        ///     Session id
        /// </summary>
        public byte SessionId => _conversationId;

        /// <summary>
        ///     Smoothed round-trip time
        /// </summary>
        public uint RoundTripTime => (uint)_kcp.RxSrtt;

        /// <summary>
        ///     Output
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <param name="current">Timestamp</param>
        void IKcpCallback.Output(byte* buffer, int length, uint current) => Output(buffer, length, current);

        /// <summary>
        ///     Receive
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <param name="current">Timestamp</param>
        internal void ReceiveReliable(byte[] buffer, int length, uint current)
        {
            if (_kcp.Input(buffer, length) != 0)
                return;
            if (_state != Connected)
                return;
            _lastReceiveTimestamp = current;
        }

        /// <summary>
        ///     Receive
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        internal void ReceiveSequenced(byte[] buffer, int length)
        {
            if (_state != Connected)
                return;
            var conversationId = buffer[0];
            if (conversationId != _conversationId)
                return;
            var sendSequence = As<byte, ushort>(ref buffer[1]);
            if (sendSequence <= _lastReceiveSequence && _lastReceiveSequence - sendSequence <= 32767)
                return;
            _lastReceiveSequence = sendSequence;
            _host.Insert(new NetworkEvent(NetworkEventType.Data, this, DataPacket.Create(buffer, 3, length - 3, Sequenced)));
        }

        /// <summary>
        ///     Receive
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        internal void ReceiveUnreliable(byte[] buffer, int length)
        {
            if (_state != Connected)
                return;
            var conversationId = buffer[0];
            if (conversationId != _conversationId)
                return;
            _host.Insert(new NetworkEvent(NetworkEventType.Data, this, DataPacket.Create(buffer, 1, length - 1, Unreliable)));
        }

        /// <summary>
        ///     Output
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <param name="current">Timestamp</param>
        private void Output(byte* buffer, int length, uint current)
        {
            _lastSendTimestamp = current;
            buffer[length] = (byte)Reliable;
            _host.Insert(IPEndPoint, buffer, length + 1);
            if (_disconnecting && _kcp.SendQueueCount == 0)
            {
                _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
                _state = Disconnected;
                _kcp.Dispose();
                _host.Remove(IPEndPoint.GetHashCode(), this);
            }
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <returns>Sent bytes</returns>
        internal int KcpSend(byte* buffer, int length) => _kcp.Send(buffer, length);

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <returns>Send bytes</returns>
        internal int SendReliable(byte* buffer, int length)
        {
            _sendBuffer[0] = (byte)Data;
            CopyBlock(_sendBuffer + 1, buffer, (uint)length);
            return KcpSend(_sendBuffer, length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        internal int SendSequenced(byte* buffer, int length)
        {
            _sendBuffer[0] = _conversationId;
            *(ushort*)(_sendBuffer + 1) = _lastSendSequence++;
            CopyBlock(_sendBuffer + 3, buffer, (uint)length);
            length += 4;
            _sendBuffer[length - 1] = (byte)Sequenced;
            _host.Insert(IPEndPoint, _sendBuffer, length);
            return length;
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        internal int SendUnreliable(byte* buffer, int length)
        {
            _sendBuffer[0] = _conversationId;
            CopyBlock(_sendBuffer + 1, buffer, (uint)length);
            length += 2;
            _sendBuffer[length - 1] = (byte)Unreliable;
            _host.Insert(IPEndPoint, _sendBuffer, length);
            return length;
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="packet">DataPacket</param>
        /// <returns>Send bytes</returns>
        public int Send(DataPacket packet)
        {
            if (_state != Connected)
                return -1;
            var flag = (int)packet.Flag;
            if ((flag & (int)Reliable) != 0)
                return SendReliable((byte*)packet.Data, packet.Length);
            if ((flag & (int)Sequenced) != 0)
                return SendSequenced((byte*)packet.Data, packet.Length);
            if ((flag & (int)Unreliable) != 0)
                return SendUnreliable((byte*)packet.Data, packet.Length);
            return -1;
        }

        /// <summary>
        ///     Timeout
        /// </summary>
        private void Timeout()
        {
            if (_state == Connected || _state == Disconnecting)
                _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
            else if (_state == Connecting)
                _host.Insert(new NetworkEvent(NetworkEventType.Timeout, this));
            _state = Disconnected;
            _kcp.Dispose();
            _host.Remove(IPEndPoint.GetHashCode(), this);
        }

        /// <summary>
        ///     Try disconnect now
        /// </summary>
        internal void TryDisconnectNow(uint conversationId)
        {
            if (_conversationId != conversationId || _state == Disconnected)
                return;
            if (_state == Connected)
                _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
            _state = Disconnected;
            _kcp.Dispose();
            _host.Remove(IPEndPoint.GetHashCode(), this);
        }

        /// <summary>
        ///     Disconnect
        /// </summary>
        private void DisconnectInternal()
        {
            if (_state == Disconnected)
                return;
            if (_state == Connected)
                _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
            _state = Disconnected;
            _kcp.Dispose();
            _host.Remove(IPEndPoint.GetHashCode(), this);
        }

        /// <summary>
        ///     Send disconnect now
        /// </summary>
        private void SendDisconnectNow()
        {
            _state = Disconnected;
            var conv = _conversationId;
            _kcp.Flush(_flushBuffer);
            _kcp.Dispose();
            _sendBuffer[0] = (byte)Header.Disconnect;
            _sendBuffer[1] = (byte)DisconnectAcknowledge;
            _sendBuffer[2] = (byte)Header.Disconnect;
            _sendBuffer[3] = (byte)DisconnectAcknowledge;
            *(uint*)(_sendBuffer + 4) = conv;
            Output(_sendBuffer, 8, Current);
            _host.Remove(IPEndPoint.GetHashCode(), this);
        }

        /// <summary>
        ///     Disconnect
        /// </summary>
        public void Disconnect()
        {
            if (_state == Connected)
            {
                _state = Disconnecting;
                _sendBuffer[0] = (byte)Header.Disconnect;
                KcpSend(_sendBuffer, 1);
                return;
            }

            if (_state == Disconnecting || _state == Disconnected)
                return;
            SendDisconnectNow();
        }

        /// <summary>
        ///     Disconnect now
        /// </summary>
        public void DisconnectNow()
        {
            if (_state == Disconnecting || _state == Disconnected)
                return;
            if (_state == Connected)
                _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
            SendDisconnectNow();
        }

        /// <summary>
        ///     Disconnect later
        /// </summary>
        public void DisconnectLater()
        {
            if (_state != Connected)
                return;
            _state = Disconnecting;
            _sendBuffer[0] = (byte)Header.Disconnect;
            KcpSend(_sendBuffer, 1);
        }

        /// <summary>
        ///     Service
        /// </summary>
        /// <param name="current">Timestamp</param>
        /// <param name="buffer">Receive buffer</param>
        internal void Service(uint current, byte* buffer)
        {
            if (_kcp.State == -1)
            {
                DisconnectInternal();
                return;
            }

            if (_lastReceiveTimestamp + PEER_RECEIVE_TIMEOUT <= current)
            {
                Timeout();
                return;
            }

            while (true)
            {
                var received = _kcp.Receive(buffer, KCP_MESSAGE_SIZE);
                if (received < 0)
                {
                    if (received != -1)
                    {
                        DisconnectInternal();
                        return;
                    }

                    break;
                }

                var header = buffer[0];
                switch (header)
                {
                    case (byte)Ping:
                        if (_state != Connected)
                            goto error;
                        continue;
                    case (byte)Connect:
                        if (_state != PeerState.None)
                            goto error;
                        _state = ConnectAcknowledging;
                        buffer[0] = (byte)ConnectAcknowledge;
                        KcpSend(buffer, 1);
                        continue;
                    case (byte)ConnectAcknowledge:
                        if (_state != Connecting)
                            goto error;
                        _state = Connected;
                        _host.Insert(new NetworkEvent(NetworkEventType.Connect, this));
                        buffer[0] = (byte)ConnectEstablish;
                        KcpSend(buffer, 1);
                        continue;
                    case (byte)ConnectEstablish:
                        if (_state != ConnectAcknowledging)
                            goto error;
                        _state = Connected;
                        _host.Insert(new NetworkEvent(NetworkEventType.Connect, this));
                        continue;
                    case (byte)Data:
                        if (_state != Connected && _state != Disconnecting)
                            goto error;
                        _host.Insert(new NetworkEvent(NetworkEventType.Data, this, DataPacket.Create(buffer + 1, received - 1, Reliable)));
                        continue;
                    case (byte)Header.Disconnect:
                        if (_state != Connected)
                            goto error;
                        _state = Disconnected;
                        _disconnecting = true;
                        buffer[0] = (byte)DisconnectAcknowledge;
                        KcpSend(buffer, 1);
                        continue;
                    case (byte)DisconnectAcknowledge:
                        if (_state != Disconnecting)
                            goto error;
                        _host.Insert(new NetworkEvent(NetworkEventType.Disconnect, this));
                        _kcp.Dispose();
                        _host.Remove(IPEndPoint.GetHashCode(), this);
                        return;
                    default:
                        error:
                        DisconnectInternal();
                        return;
                }
            }

            if (_state == Connected && _lastSendTimestamp + PEER_PING_INTERVAL <= current)
            {
                _lastSendTimestamp = current;
                buffer[0] = (byte)Ping;
                KcpSend(buffer, 1);
            }
        }

        /// <summary>
        ///     Update
        /// </summary>
        internal void Update(uint current) => _kcp.Update(current, _flushBuffer);
    }
}