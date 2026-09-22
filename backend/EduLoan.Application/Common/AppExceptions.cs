namespace EduLoan.Application.Common;

/// Thrown for any invalid-credentials / inactive-account scenario.
/// Deliberately generic message at the API boundary (don't leak which part failed).
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message = "Invalid email or password.")
        : base(message) { }
}

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}

public class InUseException : Exception
{
    public InUseException(string message) : base(message) { }
}

public class DuplicateNameException : Exception
{
    public DuplicateNameException(string entityName, string name)
        : base($"A {entityName} named '{name}' already exists.") { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}

public class InvalidFileException : Exception
{
    public InvalidFileException(string message) : base(message) { }
}

/// Thrown when one or more Blocking eligibility rules fail during submission.
/// Carries every failed rule's message so the API can return the full list at once
/// (rather than the person fixing one problem, resubmitting, and hitting the next).
public class EligibilityFailedException : Exception
{
    public List<string> FailureMessages { get; }

    public EligibilityFailedException(List<string> failureMessages)
        : base("Application does not meet eligibility requirements.")
    {
        FailureMessages = failureMessages;
    }
}
