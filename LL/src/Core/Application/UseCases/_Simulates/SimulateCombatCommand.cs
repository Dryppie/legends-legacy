using Application.Interfaces.Services.LL;
using Application.UseCases.CharacterActions.Commands.StartCharacterAction;
using Domain.Models.CharacterActions.CharacterActionDetails;
using Domain.Models.CharacterActions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases._Simulates;
public record SimulateCombatCommand() : IRequest;

public class SimulateCombatCommandHandler : IRequestHandler<SimulateCombatCommand>
{
    private readonly ISimulatorService _simulatorService;
    public SimulateCombatCommandHandler(ISimulatorService simulatorService)
    {
        _simulatorService = simulatorService;
    }
    public async Task Handle(SimulateCombatCommand request, CancellationToken cancellationToken)
    {
        await _simulatorService.SimulateCombat();
    }
}