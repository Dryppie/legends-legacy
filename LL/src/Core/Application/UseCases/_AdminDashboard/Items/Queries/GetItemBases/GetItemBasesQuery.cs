using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Items.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases._AdminDashboard.Items.Queries.GetItemBases;
public record GetItemBasesQuery() : IRequest<List<ItemBaseDto>>;
public class GetItemBasesQueryHandler : IRequestHandler<GetItemBasesQuery, List<ItemBaseDto>>
{
    private readonly IItemService _itemService;
    private readonly IMapper _mapper;
    public GetItemBasesQueryHandler(IItemService itemService, IMapper mapper)
    {
        _itemService = itemService;
        _mapper = mapper;
    }
    public async Task<List<ItemBaseDto>> Handle(GetItemBasesQuery request, CancellationToken cancellationToken)
    {
        return await _itemService.GetItemBasesAsync(cancellationToken);
    }
}