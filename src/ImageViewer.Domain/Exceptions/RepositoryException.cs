namespace ImageViewer.Domain.Exceptions;

/// <summary>
/// Base exception for repository operations
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

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class EntityNotFoundException : RepositoryException
{
    public EntityNotFoundException(string message) : base(message)
    {
    }

    public EntityNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when trying to create a duplicate entity
/// </summary>
public class DuplicateEntityException : RepositoryException
{
    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
