using Domain.Models.Prophecies;

namespace Application.Interfaces.Services.LL.Prophecies;

public interface IProphecyRewardResolver
{
    ProphecyRewardSnapshot Resolve(ProphecyDefinition definition, ProphecyRewardContext context);
}

public readonly record struct ProphecyRewardContext(
    int CharacterLevel,
    int ExperienceRequiredForNextLevel);
