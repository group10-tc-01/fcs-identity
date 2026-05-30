using Bogus;

namespace Fcg.Identity.CommomTestsUtilities.Fakers.Shared;

public static class CpfFaker
{
    private static readonly string[] ValidCpfs =
    [
        "12345678909",
        "52998224725",
        "11144477735",
        "93541134780"
    ];

    public static string Generate()
    {
        return new Faker("pt_BR").PickRandom(ValidCpfs);
    }

    public static string GenerateMasked()
    {
        var cpf = Generate();
        return $"{cpf[..3]}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf[9..]}";
    }
}
