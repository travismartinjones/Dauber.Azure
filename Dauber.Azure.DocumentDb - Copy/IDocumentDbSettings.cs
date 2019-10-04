﻿namespace Dauber.Azure.DocumentDb
{
    public interface IDocumentDbSettings
    {
        string DocumentDbRepositoryEndpointUrl { get; }
        string DocumentDbRepositoryAuthKey { get; }
        string DocumentDbRepositoryDatabaseId { get; }
        string DocumentDbRepositoryCollectionId { get; }
        bool IsPartitioned { get; }
    }
}