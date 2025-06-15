using Application.UseCases.Inventories.Dtos;
using Application.UseCases.Inventories.Events;
using Application.WebSockets.Contracts;
using AutoMapper;
using MediatR;

namespace Application.Common.Mappings;
public class DomainToClientMapper
{
    private readonly IMapper _mapper;

    public DomainToClientMapper(IMapper mapper)
    {
        _mapper = mapper;
    }

    public IMessage Map(INotification e) => e switch
    {
        LootGeneratedEvent l => new LootReceivedMsg(
            l.Loot.Select(i => _mapper.Map<InventoryItemDto>(i)).ToList()),

        // Example for another event:
        // AchievementUnlockedEvent a => _mapper.Map<AchievementUnlockedMsg>(a),

        _ => throw new NotSupportedException(e.GetType().Name)
    };
}