using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using Application.UseCases.Administration.Mappings;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Administration.Queries.GetAccountRiskDetails;

public sealed record GetAccountRiskDetailsQuery(Guid AccountId, int TransferLimit = 100)
    : IQuery<Response<AccountRiskDetailsDto>>;

public sealed class GetAccountRiskDetailsQueryHandler(ILiveOpsAccountRiskService service)
    : IRequestHandler<GetAccountRiskDetailsQuery, Response<AccountRiskDetailsDto>>
{
    public async Task<Response<AccountRiskDetailsDto>> Handle(GetAccountRiskDetailsQuery request, CancellationToken cancellationToken)
    {
        var details = await service.GetDetailsAsync(request.AccountId, request.TransferLimit, cancellationToken);
        return details is null
            ? Response<AccountRiskDetailsDto>.Fail("The account-risk snapshot was not found.")
            : Response<AccountRiskDetailsDto>.Success(AccountRiskDtoMapper.ToDto(details));
    }
}
