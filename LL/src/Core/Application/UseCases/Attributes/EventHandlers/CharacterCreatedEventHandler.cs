using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Events;
using MediatR;

namespace Application.UseCases.Attributes.EventHandlers;

public class CharacterCreatedEventHandler : INotificationHandler<CharacterCreatedEvent>
{
    private readonly IAttributeService _attributesService;

    public CharacterCreatedEventHandler(IAttributeService attributesService)
    {
        _attributesService = attributesService;
    }

    public async Task Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        _attributesService.CreateAttributesForNewCharacter(notification.CharacterId);
    }
}