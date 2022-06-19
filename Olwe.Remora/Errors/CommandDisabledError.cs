namespace Olwe.Remora.Errors;

public record CommandDisabledError(string Message = "Command is disabled") : IResultError;