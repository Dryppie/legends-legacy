using Application.UseCases.Characters.Events;
using MediatR;

namespace Application.UseCases.Attributes.EventHandlers;

//TODO: Create AttributeService
public class CharacterCreatedEventHandler : INotificationHandler<CharacterCreatedEvent>
{
    //private readonly IAttributesService _attributesService;

    public CharacterCreatedEventHandler(/*IAttributesService attributesService*/)
    {
        //_attributesService = attributesService;
    }

    public async Task Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine("ATTRIBUTES");
        //await _attributesService.CreateAttributesForNewCharacterAsync(notification.CharacterId, cancellationToken);
    }
}