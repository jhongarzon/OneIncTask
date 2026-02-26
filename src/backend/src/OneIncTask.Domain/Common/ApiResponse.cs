using Microsoft.AspNetCore.Http;

namespace OneIncTask.Domain.Common;

public class ApiResponse<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public IResult? Error { get; }

    private ApiResponse(bool isSuccess, T? data, IResult? error)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
    }

    public static ApiResponse<T> Success(T data) =>
        new(true, data, null);

    public static ApiResponse<T> Failure(IResult error) =>
        new(false, default, error);
}

public static class ApiResponseExtensions
{
    public static IResult ToResult<T>(this ApiResponse<T> response)
    {
        return response.IsSuccess
            ? Results.Ok(response.Data)
            : response.Error ?? Results.Problem("Unknown error");
    }
}
