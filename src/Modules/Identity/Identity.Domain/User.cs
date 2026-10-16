using SharedKernel;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain;

public sealed class User : AggregateRoot<Guid>
{
    public Email Email { get; private set; } = default!;
    public PasswordHash PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    private User(Guid id, Email email, PasswordHash passwordHash, string displayName, UserRole role)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(Email email, PasswordHash passwordHash, string displayName, UserRole role = UserRole.Member)
    {
        displayName = NormalizeDisplayName(displayName);
        var user = new User(Guid.NewGuid(), email, passwordHash, displayName, role);
        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email.Value));
        return user;
    }

    public void ChangeDisplayName(string displayName)
    {
        DisplayName = NormalizeDisplayName(displayName);
    }

    private static string NormalizeDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên hiển thị không được để trống.");
        var v = name.Trim();
        if (v.Length is < 2 or > 100)
            throw new DomainException("Tên hiển thị phải từ 2-100 ký tự.");
        return v;
    }

    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
    }

    public void MarkLogin() => LastLoginAt = DateTime.UtcNow;
}
