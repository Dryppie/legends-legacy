using Application.UseCases.Users;

namespace EssenceSystem.Tests;

public sealed class GuestCharacterNameGeneratorTests
{
    [Fact]
    public void Generated_names_fit_the_public_character_name_rules()
    {
        for (var sample = 0; sample < 1000; sample++)
        {
            var name = GuestCharacterNameGenerator.Generate();

            Assert.Matches("^[A-Za-z]+_[0-9]{4}$", name);
            Assert.True(AuthInputValidator.TryValidateName(name, "Character name", out var validated, out var error), error);
            Assert.Equal(name, validated);
        }
    }
}
