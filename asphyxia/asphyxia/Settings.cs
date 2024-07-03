//------------------------------------------------------------
// あなたたちを許すことはできません
// Copyright © 2024 怨靈. All rights reserved.
//------------------------------------------------------------

using static KCP.KCPBASIC;

namespace asphyxia
{
    /// <summary>
    ///     Settings
    /// </summary>
    public static class Settings
    {
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
        public const int SOCKET_BUFFER_SIZE = BUFFER_SIZE << 1;

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
        public const int TICK_INTERVAL = (int)INTERVAL_MIN;

        /// <summary>
        ///     Socket send iterations
        /// </summary>
        public const int SOCKET_SEND_ITERATIONS = 1;

        /// <summary>
        ///     Socket receive iterations
        /// </summary>
        public const int SOCKET_RECEIVE_ITERATIONS = 1;

        /// <summary>
        ///     Service iterations
        /// </summary>
        public const int SERVICE_ITERATIONS = 1;

        /// <summary>
        ///     Socket non-blocking timeout
        /// </summary>
        public const int SOCKET_POLL_TIMEOUT_MIN = 0;

        /// <summary>
        ///     Socket poll timeout
        /// </summary>
        public const int SOCKET_POLL_TIMEOUT_MAX = 1;

        /// <summary>
        ///     Host bandwidth throttle iterations
        /// </summary>
        public const int HOST_BANDWIDTH_THROTTLE_ITERATIONS = 1000;

        /// <summary>
        ///     Service throttle interval
        /// </summary>
        public const int SERVICE_THROTTLE_INTERVAL = 5000;

        /// <summary>
        ///     Ping interval
        /// </summary>
        public const int PING_INTERVAL = 500;

        /// <summary>
        ///     Peer timeout
        /// </summary>
        public const int PEER_TIMEOUT = 5000;

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
        public const int NO_CONGESTION_WINDOW = 0;
    }
}