//------------------------------------------------------------
// Onryo
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using NanoSockets;

namespace asphyxia
{
    public sealed unsafe class TravelerService
    {
        private readonly Host _host = new();
        private readonly ConcurrentDictionary<NanoIPEndPoint, Peer> _peers = new();
        private readonly ConcurrentQueue<NetworkEvent> _networkEvents = new();
        private readonly ConcurrentQueue<NetworkOutgoing> _outgoings = new();
        private int _state;

        public void Create(int maxPeers, ushort port)
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                throw new InvalidOperationException("Service has created.");
            _host.Create(maxPeers, port);
            new Thread(Polling) { IsBackground = true }.Start();
        }

        public void Dispose() => Interlocked.Exchange(ref _state, 0);

        private void Polling()
        {
            while (_state == 1)
            {
                _host.Service();
                while (_host.CheckEvents(out var networkEvent))
                    _networkEvents.Enqueue(networkEvent);
                while (_outgoings.TryDequeue(out var outgoing))
                    outgoing.Send();
                _host.Flush();
                Thread.Sleep(1);
            }

            foreach (var peer in _peers.Values)
                peer.DisconnectNow();
            _peers.Clear();
            _host.Flush();
            _host.Dispose();
            while (_networkEvents.TryDequeue(out var networkEvent))
            {
                if (networkEvent.EventType != NetworkEventType.Data)
                    continue;
                networkEvent.Packet.Dispose();
            }

            while (_outgoings.TryDequeue(out var outgoing))
                outgoing.Dispose();
        }

        public void Service()
        {
            while (_networkEvents.TryDequeue(out var networkEvent))
            {
                switch (networkEvent.EventType)
                {
                    case NetworkEventType.Connect:
                        Console.WriteLine($"Connected: {networkEvent.Peer.Id}");
                        _peers[networkEvent.Peer.IPEndPoint] = networkEvent.Peer;
                        continue;
                    case NetworkEventType.Data:
                        Console.WriteLine($"Data: {networkEvent.Peer.Id}");
                        var packet = networkEvent.Packet;
                        if (packet.Length != sizeof(NanoIPEndPoint))
                        {
                            packet.Dispose();
                            continue;
                        }

                        var ipEndPoint = Unsafe.Read<NanoIPEndPoint>((void*)packet.Data);
                        if (!_peers.TryGetValue(ipEndPoint, out var peer) || networkEvent.Peer == peer)
                        {
                            packet.Dispose();
                            continue;
                        }

                        Unsafe.Write((void*)packet.Data, peer.IPEndPoint);
                        var outgoing = new NetworkOutgoing(networkEvent.Peer, packet);
                        _outgoings.Enqueue(outgoing);
                        continue;
                    case NetworkEventType.Timeout:
                    case NetworkEventType.Disconnect:
                        Console.WriteLine($"Disconnected: {networkEvent.Peer.Id}");
                        _peers.TryRemove(networkEvent.Peer.IPEndPoint, out _);
                        continue;
                }
            }
        }
    }
}