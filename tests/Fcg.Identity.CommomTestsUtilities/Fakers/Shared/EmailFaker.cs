using Bogus;

namespace Fcg.Identity.CommomTestsUtilities.Fakers.Shared;

public static class EmailFaker
{
    public static string Generate()
    {
        return new Faker("pt_BR").Internet.Email().ToLowerInvariant();
    }
}
