namespace ProductManagement.Web.Services;

public enum ServiceResultStatus
{
    Success,
    NotFound,
    Invalid
}

/// <summary>
/// Outcome of a service write. Validation failures are expected outcomes, not exceptions:
/// <see cref="Errors"/> maps each field name (Name, Category, Price, Stock) to its message,
/// so the controller can show it next to the matching form field.
/// </summary>
public sealed class ServiceResult<T>
{
    private static readonly IReadOnlyDictionary<string, string> NoErrors = new Dictionary<string, string>();

    private ServiceResult(ServiceResultStatus status, T? value, IReadOnlyDictionary<string, string> errors)
    {
        Status = status;
        Value = value;
        Errors = errors;
    }

    public ServiceResultStatus Status { get; }

    /// <summary>The result when <see cref="Status"/> is <see cref="ServiceResultStatus.Success"/>.</summary>
    public T? Value { get; }

    public IReadOnlyDictionary<string, string> Errors { get; }

    public bool Succeeded => Status == ServiceResultStatus.Success;

    public static ServiceResult<T> Success(T value) => new(ServiceResultStatus.Success, value, NoErrors);

    public static ServiceResult<T> NotFound() => new(ServiceResultStatus.NotFound, default, NoErrors);

    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string> errors) =>
        new(ServiceResultStatus.Invalid, default, errors);
}
