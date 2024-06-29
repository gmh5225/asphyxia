//------------------------------------------------------------
// Onryo
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

#if UNITY_2021_3_OR_NEWER || GODOT
using System;
#endif

namespace asphyxia
{
    /// <summary>
    ///     Network outgoing
    /// </summary>
    public struct NetworkOutgoing : IDisposable
    {
        /// <summary>
        ///     Peer
        /// </summary>
        public Peer Peer;

        /// <summary>
        ///     DataPacket
        /// </summary>
        public DataPacket Packet;

        /// <summary>
        ///     Structure
        /// </summary>
        /// <param name="peer">Peer</param>
        /// <param name="data">DataPacket</param>
        public NetworkOutgoing(Peer peer, Span<byte> data)
        {
            Peer = peer;
            Packet = DataPacket.Create(data);
        }

        /// <summary>
        ///     Send
        /// </summary>
        public void Send()
        {
            Peer.Send(Packet.AsSpan());
            Packet.Dispose();
        }

        /// <summary>
        ///     Dispose
        /// </summary>
        public void Dispose() => Packet.Dispose();
    }
}