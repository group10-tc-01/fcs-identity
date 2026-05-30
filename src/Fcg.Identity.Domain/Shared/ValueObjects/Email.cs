using System.Net.Mail;
using Fcg.Identity.Domain.Shared.Results;

namespace Fcg.Identity.Domain.Shared.ValueObjects;

public readonly record struct Email
{
    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? email)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Error.Validation("Email.Required", "Email is required.");
        }

        if (normalizedEmail.Length > 320 || !IsValidEmail(normalizedEmail))
        {
            return Error.Validation("Email.Invalid", "Email is invalid.");
        }

        return new Email(normalizedEmail);
    }

    public override string ToString() => Value;

    private static bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
