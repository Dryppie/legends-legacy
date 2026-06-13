using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Dungeons.Dtos;
using AutoMapper;
using Common.Primitives;
using Domain.Components.Attributes;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.StartDungeonRun;

public record StartDungeonRunCommand(Guid CharacterId, string DungeonId, DungeonTier DungeonTier) : ICommand<Response<DungeonRunDto>>;

public class StartDungeonRunCommandHandler : IRequestHandler<StartDungeonRunCommand, Response<DungeonRunDto>>
{
    private readonly IMapper _mapper;
    private readonly IDungeonRunService _dungeonRunService;
    private readonly IDungeonDefinitions _dungeonDefinitions;
    private readonly IDungeonRunRepository _dungeonRuns;
    private readonly ICharacterService _characters;

    public StartDungeonRunCommandHandler(
        IMapper mapper,
        IDungeonRunService dungeonRunService,
        IDungeonDefinitions dungeonDefinitions,
        IDungeonRunRepository dungeonRuns,
        ICharacterService characters)
    {
        _mapper = mapper;
        _dungeonRunService = dungeonRunService;
        _dungeonDefinitions = dungeonDefinitions;
        _dungeonRuns = dungeonRuns;
        _characters = characters;
    }

    public async Task<Response<DungeonRunDto>> Handle(StartDungeonRunCommand request, CancellationToken cancellationToken)
    {
        var dungeonDefinition = _dungeonDefinitions.GetByKey(request.DungeonId);
        var character = await _characters.GetMyCharacterOverviewAsync(request.CharacterId, cancellationToken);
        if (character is null)
            return Response<DungeonRunDto>.Fail("Character was not found.");

        var powerScore = PowerScoreCalculator.Calculate(character.BaseCombatAttributes, character.Level);
        if (powerScore < dungeonDefinition.MinimumPowerScore)
            return Response<DungeonRunDto>.Fail($"Requires {dungeonDefinition.MinimumPowerScore} Power.");

        if (!string.IsNullOrWhiteSpace(dungeonDefinition.RequiredPreviousDungeonId)
            && !await _dungeonRuns.HasCompletedDungeonAsync(
                request.CharacterId,
                dungeonDefinition.RequiredPreviousDungeonId,
                cancellationToken))
        {
            return Response<DungeonRunDto>.Fail("Complete the previous difficulty first.");
        }

        var dungeon = await _dungeonRunService.StartRunAsync(request.CharacterId, request.DungeonId, cancellationToken);
        
        if (dungeon == null)
            return Response<DungeonRunDto>.Fail("You already have an ongoing dungeon run.");

        var result = _mapper.Map<DungeonRunDto>(dungeon);
        return Response<DungeonRunDto>.Success(result);
    }
}
