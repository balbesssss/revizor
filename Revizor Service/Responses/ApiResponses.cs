using Revizor_Service.Services;

namespace Revizor_Service.Responses;

public class ApiResponses<T> : ApiResponses
{
    public T? Data {get;set;}
    public static ApiResponses<T> Ok(T data) => new() { Success = true, Data = data};
}

public class ApiResponses
{
    public bool Success {get;set;}
    public string? Message { get; set; }
    public ExceptionCode? Code { get; set; }

    public static ApiResponses Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponses Fail(string message, ExceptionCode code) => new() {Success = false, Message=message, Code = code};
}
