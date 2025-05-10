using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Items.Dtos;
using AutoMapper;
using MediatR;

namespace Application.UseCases._AdminDashboard.Items.Queries.GetItemBases;
public record GetRecipeQuery() : IRequest<List<ItemBaseDto>>;
public class GetItemBasesQueryHandler : IRequestHandler<GetRecipeQuery, List<ItemBaseDto>>
{
    private readonly IItemService _itemService;
    private readonly IMapper _mapper;
    public GetItemBasesQueryHandler(IItemService itemService, IMapper mapper)
    {
        _itemService = itemService;
        _mapper = mapper;
    }
    public async Task<List<ItemBaseDto>> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
    {
        return await _itemService.GetItemBasesAsync(cancellationToken);
    }
}