namespace RetroScan.Api.Errors;

public sealed class LanguageModelNotConfiguredException()
    : Exception("Groq:ApiKey is not configured, so questions cannot be answered.");
