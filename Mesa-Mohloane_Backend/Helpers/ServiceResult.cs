namespace Mesa_Mohloane_Backend.Helpers;

public class ServiceResult
{
    public bool Success { get; protected set; }
    public string? Error { get; protected set; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string error) => new() { Success = false, Error = error };
}
