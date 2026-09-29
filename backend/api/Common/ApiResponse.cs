namespace Messenger.Api.Common;

public class ApiResponse<T> where T : class
{
    public ApiResponse(T data)
    {
        this.Data = data;
    }

    public ApiResponse(string errorMessage, int errorCode = 0)
    {
        this.ErrorMessage = errorMessage;
        this.ErrorCode = errorCode;
    }
    
    public T? Data { get; set; }

    public string? ErrorMessage { get; set; }
    public int ErrorCode { get; set; } = 0;
    
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}