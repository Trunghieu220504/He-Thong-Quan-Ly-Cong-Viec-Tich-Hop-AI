using SharedKernel;

namespace Identity.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email không được để trống.");

        var normalized = email.Trim().ToLowerInvariant();

        if (!IsValid(normalized))
            throw new DomainException($"Email không hợp lệ: {normalized}");

        return new Email(normalized);
    }

    private static bool IsValid(string email)
    {
        if (email.Length > 254) return false;
        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@')) return false;
        var domain = email[(at + 1)..];
        if (domain.Length == 0 || !domain.Contains('.')) return false;
        if (email.Contains(' ')) return false;
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
