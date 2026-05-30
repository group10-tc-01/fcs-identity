using Fcg.Identity.Domain.Shared.Results;

namespace Fcg.Identity.Domain.Shared.ValueObjects;

public readonly record struct Cpf
{
    private Cpf(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public string Masked => $"{Value[..3]}.{Value.Substring(3, 3)}.{Value.Substring(6, 3)}-{Value[9..]}";

    public static Result<Cpf> Create(string? cpf)
    {
        var normalizedCpf = Normalize(cpf);

        if (string.IsNullOrWhiteSpace(normalizedCpf))
        {
            return Error.Validation("Cpf.Required", "CPF is required.");
        }

        if (!IsValid(normalizedCpf))
        {
            return Error.Validation("Cpf.Invalid", "CPF is invalid.");
        }

        return new Cpf(normalizedCpf);
    }

    public override string ToString() => Value;

    private static string Normalize(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            return string.Empty;
        }

        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    private static bool IsValid(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var firstCheckDigit = CalculateCheckDigit(cpf, 9);
        var secondCheckDigit = CalculateCheckDigit(cpf, 10);

        return cpf[9] - '0' == firstCheckDigit && cpf[10] - '0' == secondCheckDigit;
    }

    private static int CalculateCheckDigit(string cpf, int length)
    {
        var sum = 0;

        for (var index = 0; index < length; index++)
        {
            sum += (cpf[index] - '0') * (length + 1 - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
