﻿namespace Dauber.Azure.Blob.Contracts
{
    public interface IBlobSettings
    {
        string ConnectionString { get; }
        bool IsContainerCreatedIfMissing { get; }
    }
}
