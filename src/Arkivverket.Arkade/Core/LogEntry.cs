﻿using System;

namespace Arkivverket.Arkade.Core
{
    public class LogEntry
    {
        public DateTime Timestamp { get; }

        public string Message { get; }

        public LogEntry(DateTime timestamp, string message)
        {
            Timestamp = timestamp;
            Message = message;
        }

    }
}
