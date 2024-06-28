//------------------------------------------------------------
// Onryo
// Copyright © 2024 Molth Nevin. All rights reserved.
//------------------------------------------------------------

namespace asphyxia
{
    /// <summary>
    ///     Settings
    /// </summary>
    public static class Settings
    {
        /// <summary>
        ///     Conversation
        /// </summary>
        public const uint CONVERSATION = 3847762548;

        /// <summary>
        ///     MaxPeers
        /// </summary>
        public const int MAX_PEERS = 4096;

        /// <summary>
        ///     MaxEvents
        /// </summary>
        public const int MAX_EVENTS = 4096;

        /// <summary>
        ///     BufferSize
        /// </summary>
        public const int BUFFER_SIZE = 2048;

        /// <summary>
        ///     WindowSize
        /// </summary>
        public const int WINDOW_SIZE = 1024;

        /// <summary>
        ///     Tick interval
        /// </summary>
        public const int TICK_INTERVAL = 1;

        /// <summary>
        ///     PingInterval
        /// </summary>
        public const int PING_INTERVAL = 500;

        /// <summary>
        ///     Timeout
        /// </summary>
        public const int TIMEOUT = 5000;

        /// <summary>
        ///     NoDelay
        /// </summary>
        public const int NO_DELAY = 1;

        /// <summary>
        ///     FastResend
        /// </summary>
        public const int FAST_RESEND = 2;

        /// <summary>
        ///     CongestionWindow
        /// </summary>
        public const int CONGESTION_WINDOW = 0;
    }
}