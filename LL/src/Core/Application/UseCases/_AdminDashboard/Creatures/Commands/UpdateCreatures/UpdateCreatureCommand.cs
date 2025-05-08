using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using MediatR;

namespace Application.UseCases._AdminDashboard.Creatures.Commands.UpdateCreatures
{
    public record UpdateCreatureCommand(CreatureDto CreatureToUpdate): IRequest;
    public class UpdateCreatureCommandHandler : IRequestHandler<UpdateCreatureCommand>
    {
        private readonly ICreatureService _creatureService;

        public UpdateCreatureCommandHandler(ICreatureService creatureService)
        {
            _creatureService = creatureService;
        }

        public async Task Handle(UpdateCreatureCommand request, CancellationToken cancellationToken)
        {
            await _creatureService.UpdateCreatureAsync(request.CreatureToUpdate, cancellationToken);
        }
    }
}
