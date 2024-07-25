//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

using static KCP.KCPBASIC;

// ReSharper disable HeuristicUnreachableCode

namespace asphyxia
{
    /// <summary>
    ///     Settings
    /// </summary>
    public static class Settings
    {
        /// <summary>
        ///     Socket batch receive/send
        ///     TODO: If the network bandwidth is relatively small, set it to false.
        /// </summary>
        public const bool SOCKET_BATCH_IO = false;

        /// <summary>
        ///     Max peers
        /// </summary>
        public const int MAX_PEERS = 4096;

        /// <summary>
        ///     Max send events
        /// </summary>
        public const int MAX_SEND_EVENTS = MAX_RECEIVE_EVENTS << 1;

        /// <summary>
        ///     Max receive events
        /// </summary>
        public const int MAX_RECEIVE_EVENTS = MAX_PEERS / TICK_INTERVAL;

        /// <summary>
        ///     Socket buffer size
        /// </summary>
        public const int SOCKET_BUFFER_SIZE = MAX_PEERS * BUFFER_SIZE;

        /// <summary>
        ///     Buffer size
        /// </summary>
        public const int BUFFER_SIZE = 2048;

        /// <summary>
        ///     Window size
        /// </summary>
        public const int WINDOW_SIZE = 1024;

        /// <summary>
        ///     Tick interval
        /// </summary>
        public const int TICK_INTERVAL = 1;

        /// <summary>
        ///     Ping interval
        /// </summary>
        public const int PING_INTERVAL = 500;

        /// <summary>
        ///     Receive timeout
        /// </summary>
        public const int RECEIVE_TIMEOUT = 5000;

        /// <summary>
        ///     Maximum transmission unit
        /// </summary>
        public const int MAXIMUM_TRANSMISSION_UNIT = 1400;

        /// <summary>
        ///     Output buffer size
        /// </summary>
        public const int OUTPUT_BUFFER_SIZE = (int)(REVERSED_HEAD + (MAXIMUM_TRANSMISSION_UNIT + OVERHEAD) * 3);

        /// <summary>
        ///     Reversed size
        /// </summary>
        public const int REVERSED_SIZE = (int)REVERSED_HEAD + sizeof(Header);

        /// <summary>
        ///     Max message size
        /// </summary>
        public const int MAX_MESSAGE_SIZE = WINDOW_SIZE < (int)FRG_LIMIT ? (MAXIMUM_TRANSMISSION_UNIT - (int)OVERHEAD - REVERSED_SIZE) * (WINDOW_SIZE - 1) - 1 : (MAXIMUM_TRANSMISSION_UNIT - (int)OVERHEAD - REVERSED_SIZE) * ((int)FRG_LIMIT - 1) - 1;

        /// <summary>
        ///     No delay
        /// </summary>
        public const int NO_DELAY = 1;

        /// <summary>
        ///     Fast resend
        /// </summary>
        public const int FAST_RESEND = 0;

        /// <summary>
        ///     No congestion window
        /// </summary>
        public const int NO_CONGESTION_WINDOW = 1;
    }
}