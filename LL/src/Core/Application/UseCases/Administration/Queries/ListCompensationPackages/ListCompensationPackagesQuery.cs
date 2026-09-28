using Application.Interfaces.Services.LL.Administration;
using Application.MediatR.Markers;
using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;
namespace Application.UseCases.Administration.Queries.ListCompensationPackages;
public sealed record ListCompensationPackagesQuery : IQuery<Response<IReadOnlyList<CompensationPackageDto>>>;
public sealed class ListCompensationPackagesQueryHandler(ICompensationPackageService service, IMapper mapper) : IRequestHandler<ListCompensationPackagesQuery, Response<IReadOnlyList<CompensationPackageDto>>>
{
    public async Task<Response<IReadOnlyList<CompensationPackageDto>>> Handle(ListCompensationPackagesQuery request, CancellationToken ct) =>
        Response<IReadOnlyList<CompensationPackageDto>>.Success(mapper.Map<IReadOnlyList<CompensationPackageDto>>(await service.ListAsync(ct)));
}
