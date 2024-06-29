//------------------------------------------------------------
// Onryo
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif
using KCP;
using NanoSockets;
using static asphyxia.Settings;
using static asphyxia.Time;
using static asphyxia.State;
using static asphyxia.Header;
using static System.Runtime.CompilerServices.Unsafe;

#pragma warning disable CS8632

// ReSharper disable ConvertIfStatementToSwitchStatement

namespace asphyxia
{
    /// <summary>
    ///     Peer
    /// </summary>
    public sealed unsafe class Peer
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
        ///     State
        /// </summary>
        private State _state;

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
        public readonly NanoIPEndPoint IPEndPoint;

        /// <summary>
        ///     Kcp
        /// </summary>
        private readonly Kcp _kcp;

        /// <summary>
        ///     Buffer
        /// </summary>
        private readonly byte* _sendBuffer;

        /// <summary>
        ///     Last send timestamp
        /// </summary>
        private uint _lastSendTimestamp;

        /// <summary>
        ///     Last receive timestamp
        /// </summary>
        private uint _lastReceiveTimestamp;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="host">Host</param>
        /// <param name="id">Id</param>
        /// <param name="ipEndPoint">IPEndPoint</param>
        /// <param name="sendBuffer">Buffer</param>
        /// <param name="state">State</param>
        internal Peer(Host host, uint id, NanoIPEndPoint ipEndPoint, byte* sendBuffer, State state = State.None)
        {
            _host = host;
            Id = id;
            IPEndPoint = ipEndPoint;
            _sendBuffer = sendBuffer;
            _state = state;
            _kcp = new Kcp(CONVERSATION, Output);
            _kcp.SetNoDelay(NO_DELAY, TICK_INTERVAL, FAST_RESEND, NO_CONGESTION_WINDOW);
            _kcp.SetWindowSize(WINDOW_SIZE, WINDOW_SIZE);
            var current = Current;
            _lastSendTimestamp = current;
            _lastReceiveTimestamp = current;
        }

        /// <summary>
        ///     Is created
        /// </summary>
        public bool IsSet => _kcp.IsSet;

        /// <summary>
        ///     Input
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        internal void Input(byte* buffer, int length)
        {
            if (_state == Connected)
                _lastReceiveTimestamp = Current;
            _kcp.Input(buffer, length);
        }

        /// <summary>
        ///     Output
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        private void Output(byte* buffer, int length)
        {
            _lastSendTimestamp = Current;
            fixed (NanoIPEndPoint* ptr = &IPEndPoint)
            {
                _host.Insert(new OutgoingCommand(ptr, buffer, length));
            }
        }

        /// <summary>
        ///     Output
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        private void DisconnectingOutput(byte* buffer, int length)
        {
            fixed (NanoIPEndPoint* ptr = &IPEndPoint)
            {
                _host.Insert(new OutgoingCommand(ptr, buffer, length));
            }

            if (_kcp.SendQueueCount == 0)
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
        internal void RawSend(byte* buffer, int length) => _kcp.Send(buffer, length);

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <returns>Send bytes</returns>
        public void Send(byte[] buffer)
        {
            if (_state != Connected)
                return;
            var length = buffer.Length;
            _sendBuffer[0] = (byte)Data;
            fixed (byte* ptr = &buffer[0])
            {
                CopyBlock(_sendBuffer + 1, ptr, (uint)length);
            }

            RawSend(_sendBuffer, length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <returns>Send bytes</returns>
        public void Send(byte[] buffer, int length)
        {
            if (_state != Connected)
                return;
            _sendBuffer[0] = (byte)Data;
            fixed (byte* ptr = &buffer[0])
            {
                CopyBlock(_sendBuffer + 1, ptr, (uint)length);
            }

            RawSend(_sendBuffer, length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <returns>Send bytes</returns>
        public void Send(byte[] buffer, int offset, int length)
        {
            if (_state != Connected)
                return;
            _sendBuffer[0] = (byte)Data;
            fixed (byte* ptr = &buffer[offset])
            {
                CopyBlock(_sendBuffer + 1, ptr, (uint)length);
            }

            RawSend(_sendBuffer, length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <returns>Send bytes</returns>
        public void Send(ReadOnlySpan<byte> buffer)
        {
            if (_state != Connected)
                return;
            _sendBuffer[0] = (byte)Data;
            fixed (byte* ptr = &buffer[0])
            {
                CopyBlock(_sendBuffer + 1, ptr, (uint)buffer.Length);
            }

            RawSend(_sendBuffer, buffer.Length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="length">Length</param>
        /// <returns>Send bytes</returns>
        public void Send(byte* buffer, int length)
        {
            if (_state != Connected)
                return;
            _sendBuffer[0] = (byte)Data;
            CopyBlock(_sendBuffer + 1, buffer, (uint)length);
            RawSend(_sendBuffer, length + 1);
        }

        /// <summary>
        ///     Send
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="length">Length</param>
        /// <returns>Send bytes</returns>
        public void Send(byte* buffer, int offset, int length)
        {
            if (_state != Connected)
                return;
            _sendBuffer[0] = (byte)Data;
            CopyBlock(_sendBuffer + 1, buffer + offset, (uint)length);
            RawSend(_sendBuffer, length + 1);
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
        ///     Disconnect
        /// </summary>
        internal void DisconnectInternal()
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
        ///     Disconnect
        /// </summary>
        public void Disconnect()
        {
            if (_state == Connected)
            {
                _state = Disconnecting;
                _sendBuffer[0] = (byte)Header.Disconnect;
                RawSend(_sendBuffer, 1);
            }

            if (_state == Disconnecting || _state == Disconnected)
                return;
            _state = Disconnected;
            _kcp.Flush();
            _kcp.Dispose();
            _sendBuffer[0] = (byte)Header.Disconnect;
            _sendBuffer[1] = (byte)DisconnectAcknowledge;
            _sendBuffer[2] = (byte)Header.Disconnect;
            _sendBuffer[3] = (byte)DisconnectAcknowledge;
            Output(_sendBuffer, 4);
            _host.Remove(IPEndPoint.GetHashCode(), this);
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
            _state = Disconnected;
            _kcp.Flush();
            _kcp.Dispose();
            _sendBuffer[0] = (byte)Header.Disconnect;
            _sendBuffer[1] = (byte)DisconnectAcknowledge;
            _sendBuffer[2] = (byte)Header.Disconnect;
            _sendBuffer[3] = (byte)DisconnectAcknowledge;
            Output(_sendBuffer, 4);
            _host.Remove(IPEndPoint.GetHashCode(), this);
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
            RawSend(_sendBuffer, 1);
        }

        /// <summary>
        ///     Service
        /// </summary>
        /// <param name="buffer">Receive buffer</param>
        internal void Service(byte* buffer)
        {
            if (_lastReceiveTimestamp + TIMEOUT <= Current)
            {
                Timeout();
                return;
            }

            while (true)
            {
                var received = _kcp.Receive(buffer, BUFFER_SIZE);
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
                if (header != 0 && header <= 64 && (header & (header - 1)) == 0)
                {
                    switch (header)
                    {
                        case (byte)Ping:
                            if (_state != Connected)
                                goto error;
                            continue;
                        case (byte)Connect:
                            if (_state != State.None)
                                goto error;
                            _state = ConnectAcknowledging;
                            buffer[0] = (byte)ConnectAcknowledge;
                            RawSend(buffer, 1);
                            continue;
                        case (byte)ConnectAcknowledge:
                            if (_state != Connecting)
                                goto error;
                            _state = Connected;
                            _host.Insert(new NetworkEvent(NetworkEventType.Connect, this));
                            buffer[0] = (byte)ConnectEstablish;
                            RawSend(buffer, 1);
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
                            _host.Insert(new NetworkEvent(NetworkEventType.Data, this, DataPacket.Create(buffer + 1, received - 1)));
                            continue;
                        case (byte)Header.Disconnect:
                            if (_state != Connected)
                                goto error;
                            _state = Disconnected;
                            _kcp.SetOutput(DisconnectingOutput);
                            buffer[0] = (byte)DisconnectAcknowledge;
                            RawSend(buffer, 1);
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
            }

            var current = Current;
            if (_state == Connected && _lastSendTimestamp + PING_INTERVAL <= current)
            {
                _lastSendTimestamp = current;
                buffer[0] = (byte)Ping;
                RawSend(buffer, 1);
            }

            _kcp.Update(current);
        }
    }
}