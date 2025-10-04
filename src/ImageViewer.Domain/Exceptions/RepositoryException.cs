using System;

namespace ImageViewer.Domain.Exceptions;

/// <summary>
/// Exception thrown when repository operations fail
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message)
    {
    }

    public RepositoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}