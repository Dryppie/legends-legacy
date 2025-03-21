using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces.Services.AdminDashboard;
using Application.UseCases._AdminDashboard.Creatures.Queries.GetCreatures;
using Domain.Models.Entities.Creatures;
using MediatR;

namespace Application.UseCases._AdminDashboard.Creatures.Queries.UpdateCreatures
{
    public record UpdateCreatureQuery(): IRequest<Creature>;
    public class UpdateCreatureQueryHandler : IRequestHandler<UpdateCreatureQuery, Creature>
    {
        private readonly ICreatureService _creatureService;

        public UpdateCreatureQueryHandler(ICreatureService creatureService) 
        {
            _creatureService = creatureService;
        }

        public async Task<Creature> Handle(UpdateCreatureQuery request, CancellationToken cancellationToken)
        {
            return await _creatureService.UpdateCreatureAsync(cancellationToken);
        }
}
