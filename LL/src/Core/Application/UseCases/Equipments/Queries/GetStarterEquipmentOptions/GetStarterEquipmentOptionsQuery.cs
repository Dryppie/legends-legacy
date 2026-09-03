using Application.Interfaces.Services.LL.Items;
using Application.MediatR.Markers;
using Application.UseCases.Equipments.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases.Equipments.Queries.GetStarterEquipmentOptions;

public sealed record GetStarterEquipmentOptionsQuery : IQuery<IReadOnlyList<StarterEquipmentOptionDto>>;

public sealed class GetStarterEquipmentOptionsQueryHandler(IStarterEquipmentService service, IMapper mapper)
    : IRequestHandler<GetStarterEquipmentOptionsQuery, IReadOnlyList<StarterEquipmentOptionDto>>
{
    public Task<IReadOnlyList<StarterEquipmentOptionDto>> Handle(GetStarterEquipmentOptionsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(mapper.Map<IReadOnlyList<StarterEquipmentOptionDto>>(service.GetOptions()));
}
