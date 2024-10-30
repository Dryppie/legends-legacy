using Application.Interfaces.Services.LL;
using Application.UseCases.Characters.Events;
using MediatR;

namespace Application.UseCases.Attributes.EventHandlers;

//TODO: Create AttributeService
public class CharacterCreatedEventHandler : INotificationHandler<CharacterCreatedEvent>
{
    private readonly IAttributeService _attributesService;

    public CharacterCreatedEventHandler(IAttributeService attributesService)
    {
        _attributesService = attributesService;
    }

    public async Task Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _attributesService.CreateAttributesForNewCharacterAsync(notification.CharacterId, cancellationToken);
    }
}