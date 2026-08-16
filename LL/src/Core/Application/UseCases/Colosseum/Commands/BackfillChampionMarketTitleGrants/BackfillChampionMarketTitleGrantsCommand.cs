using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Markers;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Commands.BackfillChampionMarketTitleGrants;

public sealed record BackfillChampionMarketTitleGrantsCommand
    : ICommand<Response<BackfillChampionMarketTitleGrantsResponseDto>>;

public sealed class BackfillChampionMarketTitleGrantsResponseDto
{
    public int GrantedCount { get; set; }
    public List<BackfillChampionMarketTitleGrantDto> Grants { get; set; } = [];
}

public sealed class BackfillChampionMarketTitleGrantDto
{
    public Guid CharacterId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;
    public DateTimeOffset PurchasedAt { get; set; }
}

public sealed class BackfillChampionMarketTitleGrantsCommandHandler
    : IRequestHandler<BackfillChampionMarketTitleGrantsCommand, Response<BackfillChampionMarketTitleGrantsResponseDto>>
{
    private readonly IColosseumService _colosseumService;

    public BackfillChampionMarketTitleGrantsCommandHandler(IColosseumService colosseumService)
    {
        _colosseumService = colosseumService;
    }

    public async Task<Response<BackfillChampionMarketTitleGrantsResponseDto>> Handle(
        BackfillChampionMarketTitleGrantsCommand request,
        CancellationToken cancellationToken)
    {
        var grants = await _colosseumService.BackfillMissingChampionMarketTitleGrantsAsync(cancellationToken);

        return Response<BackfillChampionMarketTitleGrantsResponseDto>.Success(
            new BackfillChampionMarketTitleGrantsResponseDto
            {
                GrantedCount = grants.Count,
                Grants = [.. grants.Select(grant => new BackfillChampionMarketTitleGrantDto
                {
                    CharacterId = grant.CharacterId,
                    ItemId = grant.ItemId,
                    TitleKey = grant.TitleKey,
                    PurchasedAt = grant.PurchasedAt
                })]
            });
    }
}
