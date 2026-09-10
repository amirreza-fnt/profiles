namespace ProfileService.Application.Exceptions;

public class DomainValidationException : Exception
{
    public DomainValidationException(string message) : base(message) { }
}

public class NotAuthenticatedException : Exception
{
    public NotAuthenticatedException(string message) : base(message) { }
}

public class NotAuthorizedException : Exception
{
    public NotAuthorizedException(string message) : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class DependencyUnavailableException : Exception
{
    public DependencyUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
