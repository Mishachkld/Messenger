namespace Messenger.Api.Common.Base;

public class ApiResponse<T> where T : class
{
    public ApiResponse(T data)
    {
        this.Data = data;
    }

    public ApiResponse(string errorMessage)
    {
        this.ErrorMessage = errorMessage;
    }
    
    public T? Data { get; set; }

    public string? ErrorMessage { get; set; }
    
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}