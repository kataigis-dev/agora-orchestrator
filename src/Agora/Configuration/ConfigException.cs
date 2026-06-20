namespace Agora.Configuration;

/// <summary>Thrown when a config file is missing, malformed, or fails reference validation.</summary>
public sealed class ConfigException : Exception
{
    /// <summary>Creates the exception with an error message.</summary>
    public ConfigException(string message) : base(message) { }

    /// <summary>Creates the exception wrapping an underlying cause.</summary>
    public ConfigException(string message, Exception inner) : base(message, inner) { }
}
