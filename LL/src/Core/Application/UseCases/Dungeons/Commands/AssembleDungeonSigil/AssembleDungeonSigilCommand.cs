using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Entities;
using Application.MediatR.Markers;
using Application.UseCases.Characters.Dtos;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Dungeons.Queries.GetAvailableDungeons;
using Application.UseCases.Inventories.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Dungeons.Commands.AssembleDungeonSigil;

public sealed record AssembleDungeonSigilCommand(
    Guid CharacterId,
    string DungeonId) : ICommand<Response<DungeonSigilAssemblyResponseDto>>;

public sealed class AssembleDungeonSigilCommandHandler(
    IDungeonSigilAssemblyService assemblyService,
    IInventoryService inventoryService,
    ICharacterService characterService,
    DungeonHubFactory dungeonHub,
    IMapper mapper)
    : IRequestHandler<AssembleDungeonSigilCommand, Response<DungeonSigilAssemblyResponseDto>>
{
    public async Task<Response<DungeonSigilAssemblyResponseDto>> Handle(
        AssembleDungeonSigilCommand request,
        CancellationToken cancellationToken)
    {
        var result = await assemblyService.AssembleAsync(
            request.CharacterId,
            request.DungeonId,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
            return Response<DungeonSigilAssemblyResponseDto>.Fail(
                result.Error ?? "Could not assemble the dungeon sigil.");

        var inventory = await inventoryService.GetInventoryByIdAsync(request.CharacterId, cancellationToken);
        var character = await characterService.GetCharacterByCharacterIdAsync(request.CharacterId, cancellationToken);
        if (inventory is null || character is null)
            return Response<DungeonSigilAssemblyResponseDto>.Fail(
                "Failed to load updated dungeon sigil state.");

        var response = mapper.Map<DungeonSigilAssemblyResponseDto>(result.Value);
        response = new DungeonSigilAssemblyResponseDto
        {
            DungeonId = response.DungeonId,
            SigilItemId = response.SigilItemId,
            SigilName = response.SigilName,
            InventoryQuantity = response.InventoryQuantity,
            SigilFragmentsRemaining = response.SigilFragmentsRemaining,
            InventoryItems = mapper.Map<List<InventoryItemDto>>(inventory.InventoryItems),
            Character = mapper.Map<CharacterDto>(character),
            // The inventory insert is committed after this handler returns, so use the
            // authoritative mutation result while rebuilding database-backed access state.
            Hub = await dungeonHub.CreateAsync(
                request.CharacterId,
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    [response.SigilItemId] = response.InventoryQuantity
                },
                cancellationToken)
        };

        return Response<DungeonSigilAssemblyResponseDto>.Success(response);
    }
}
