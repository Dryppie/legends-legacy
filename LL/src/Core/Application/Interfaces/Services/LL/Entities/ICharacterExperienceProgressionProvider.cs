namespace Application.Interfaces.Services.LL.Entities;

public interface ICharacterExperienceProgressionProvider
{
    long GetRequiredExperience(int level);
}
