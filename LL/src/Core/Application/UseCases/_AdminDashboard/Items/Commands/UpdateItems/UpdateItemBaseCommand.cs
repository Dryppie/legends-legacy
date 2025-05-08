using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Items.Dtos;
using MediatR;

namespace Application.UseCases._AdminDashboard.Items.Commands.UpdateItems;
public record UpdateItemBaseCommand(ItemBaseDto ItemBaseToUpdate) : IRequest<ItemBaseDto>;
public class UpdateItemBaseCommandHandler : IRequestHandler<UpdateItemBaseCommand, ItemBaseDto>
{
    private readonly IItemService _itemService;

    public UpdateItemBaseCommandHandler(IItemService itemService)
    {
        _itemService = itemService;
    }

    public async Task<ItemBaseDto> Handle(UpdateItemBaseCommand request, CancellationToken cancellationToken)
    {
        await _itemService.UpdateItemBaseAsync(request.ItemBaseToUpdate, cancellationToken);
        return request.ItemBaseToUpdate;
    }
}