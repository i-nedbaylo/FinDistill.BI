namespace FinDistill.Domain.Common;

/// <summary>
/// Represents an error with a code and human-readable message.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
