namespace ATMCashForecasting.Application.Common.Models;

public class Result
{
    public bool Succeeded { get; protected init; }
    public IReadOnlyList<string> Errors { get; protected init; } = Array.Empty<string>();

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}

public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Success(T value) => new() { Succeeded = true, Value = value };
    public static new Result<T> Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}

public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedList<T> Create(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
