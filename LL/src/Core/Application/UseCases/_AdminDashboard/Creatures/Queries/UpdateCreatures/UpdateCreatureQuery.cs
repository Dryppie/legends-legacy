using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Creatures.Dtos;
using Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
using Domain.Models.Entities.Creatures;
using MediatR;

namespace Application.UseCases._AdminDashboard.Creatures.Queries.UpdateCreatures
{
    public record UpdateCreatureQuery(CreatureDto CreatureToUpdate): IRequest;
    public class UpdateCreatureQueryHandler : IRequestHandler<UpdateCreatureQuery>
    {
        private readonly ICreatureService _creatureService;

        public UpdateCreatureQueryHandler(ICreatureService creatureService)
        {
            _creatureService = creatureService;
        }

        public async Task Handle(UpdateCreatureQuery request, CancellationToken cancellationToken)
        {
            await _creatureService.UpdateCreatureAsync(request.CreatureToUpdate, cancellationToken);
        }
    }
}
