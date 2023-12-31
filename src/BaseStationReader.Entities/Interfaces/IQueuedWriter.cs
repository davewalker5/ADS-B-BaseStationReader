﻿using BaseStationReader.Entities.Events;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IQueuedWriter
    {
        event EventHandler<BatchWrittenEventArgs>? BatchWritten;
        void Push(object entity);
        void Start();
        void Stop();
    }
}
