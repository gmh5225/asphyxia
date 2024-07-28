//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

using System.Net.Sockets;
using System.Text;

namespace asphyxia
{
    public sealed class Program
    {
        private static void Main() => TestConnection();

        private static void StartNatTravelService()
        {
            var service = new NatTravelService();
            service.Create(4096, 7778);
            Console.CancelKeyPress += (sender, args) =>
            {
                service.Dispose();
                Thread.Sleep(1000);
            };
            while (true)
            {
                service.Service();
                Thread.Sleep(1);
            }
        }

        private static void TestConnection()
        {
            var a = new Host();
            var b = new Host();
            a.Create(100, 7777, Socket.OSSupportsIPv6);
            b.Create(100);
            Thread.Sleep(100);
            b.Connect("127.0.0.1", 7777);
            Peer? peer = null;
            Peer? peer2 = null;
            var connected = false;
            var connected2 = false;
            Console.CancelKeyPress += (sender, args) =>
            {
                a.Dispose();
                b.Dispose();
            };
            var i = 0;
            var j = 0;
            while (true)
            {
                Thread.Sleep(100);
                a.Service();
                b.Service();
                while (a.CheckEvents(out var networkEvent))
                {
                    switch (networkEvent.EventType)
                    {
                        case NetworkEventType.Connect:
                            connected2 = true;
                            peer2 = networkEvent.Peer;
                            Console.WriteLine("Server Connect: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.Data:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"{networkEvent.Packet.Flag}: " + Encoding.UTF8.GetString(networkEvent.Packet.AsSpan()));
                            Console.ForegroundColor = ConsoleColor.White;
                            networkEvent.Packet.Dispose();
                            break;
                        case NetworkEventType.Disconnect:
                            Console.WriteLine("Server Disconnect: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.Timeout:
                            Console.WriteLine("Server Timeout: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.None:
                            break;
                    }
                }

                while (b.CheckEvents(out var networkEvent))
                {
                    switch (networkEvent.EventType)
                    {
                        case NetworkEventType.Connect:
                            connected = true;
                            peer = networkEvent.Peer;
                            Console.WriteLine("Connect: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.Data:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{networkEvent.Packet.Flag}: " + Encoding.UTF8.GetString(networkEvent.Packet.AsSpan()));
                            Console.ForegroundColor = ConsoleColor.White;
                            networkEvent.Packet.Dispose();
                            break;
                        case NetworkEventType.Disconnect:
                            Console.WriteLine("Disconnect: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.Timeout:
                            Console.WriteLine("Timeout: " + networkEvent.Peer.Id);
                            break;
                        case NetworkEventType.None:
                            break;
                    }
                }

                if (connected && connected2)
                {
                    i++;
                    if (i < 10)
                    {
                        for (var k = 0; k < 1; k++)
                        {
                            peer?.Send(DataPacket.Create(Encoding.UTF8.GetBytes($"server: {i}"), PacketFlag.Reliable | PacketFlag.NoAllocate));
                        }
                    }

                    j++;
                    if (j == 10)
                        peer?.DisconnectNow();
                    else if (j < 10)
                    {
                        if (peer2 != null)
                        {
                            for (var k = 0; k < 1; ++k)
                            {
                                peer2.Send(DataPacket.Create(Encoding.UTF8.GetBytes($"client: {j}"), PacketFlag.Sequenced | PacketFlag.NoAllocate));
                            }
                        }
                    }
                }

                a.Flush();
                b.Flush();
            }
        }
    }
}