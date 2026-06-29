using Application.Interfaces.Services.LL.Colosseum;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Colosseum.Tournaments.Commands;

[NonTransactional]
public sealed record CreateTournamentTeamCommand(Guid CharacterId, Guid TournamentId, string Name)
    : ICommand<Response<CreateTournamentTeamResponseDto>>;

public sealed class CreateTournamentTeamCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<CreateTournamentTeamCommand, Response<CreateTournamentTeamResponseDto>>
{
    public async Task<Response<CreateTournamentTeamResponseDto>> Handle(
        CreateTournamentTeamCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateTeamAsync(request.CharacterId, request.TournamentId, request.Name, cancellationToken);
        return result is null
            ? Response<CreateTournamentTeamResponseDto>.Fail("Tournament team creation failed.")
            : Response<CreateTournamentTeamResponseDto>.Success(mapper.Map<CreateTournamentTeamResponseDto>(result));
    }
}

[NonTransactional]
public sealed record InviteTournamentTeamMemberCommand(
    Guid CharacterId,
    Guid TournamentId,
    Guid TeamId,
    Guid InvitedParticipantId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class InviteTournamentTeamMemberCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<InviteTournamentTeamMemberCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        InviteTournamentTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.InviteToTeamAsync(
            request.CharacterId,
            request.TournamentId,
            request.TeamId,
            request.InvitedParticipantId,
            cancellationToken);

        return result is null
            ? Response<TournamentTeamActionResponseDto>.Fail("Tournament team invite failed.")
            : Response<TournamentTeamActionResponseDto>.Success(mapper.Map<TournamentTeamActionResponseDto>(result));
    }
}

[NonTransactional]
public sealed record AcceptTournamentTeamInviteCommand(Guid CharacterId, Guid InviteId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class AcceptTournamentTeamInviteCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<AcceptTournamentTeamInviteCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        AcceptTournamentTeamInviteCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptTeamInviteAsync(request.CharacterId, request.InviteId, cancellationToken);
        return result is null
            ? Response<TournamentTeamActionResponseDto>.Fail("Tournament team invite acceptance failed.")
            : Response<TournamentTeamActionResponseDto>.Success(mapper.Map<TournamentTeamActionResponseDto>(result));
    }
}

[NonTransactional]
public sealed record ApplyToTournamentTeamCommand(Guid CharacterId, Guid TournamentId, Guid TeamId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class ApplyToTournamentTeamCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<ApplyToTournamentTeamCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        ApplyToTournamentTeamCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.ApplyToTeamAsync(request.CharacterId, request.TournamentId, request.TeamId, cancellationToken);
        return result is null
            ? Response<TournamentTeamActionResponseDto>.Fail("Tournament team application failed.")
            : Response<TournamentTeamActionResponseDto>.Success(mapper.Map<TournamentTeamActionResponseDto>(result));
    }
}

[NonTransactional]
public sealed record AcceptTournamentTeamApplicationCommand(Guid CharacterId, Guid ApplicationId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class AcceptTournamentTeamApplicationCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<AcceptTournamentTeamApplicationCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        AcceptTournamentTeamApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptTeamApplicationAsync(request.CharacterId, request.ApplicationId, cancellationToken);
        return result is null
            ? Response<TournamentTeamActionResponseDto>.Fail("Tournament team application acceptance failed.")
            : Response<TournamentTeamActionResponseDto>.Success(mapper.Map<TournamentTeamActionResponseDto>(result));
    }
}

[NonTransactional]
public sealed record KickTournamentTeamMemberCommand(
    Guid CharacterId,
    Guid TournamentId,
    Guid TeamId,
    Guid ParticipantId)
    : ICommand<Response<TournamentTeamActionResponseDto>>;

public sealed class KickTournamentTeamMemberCommandHandler(ITournamentGroundsService service, IMapper mapper)
    : IRequestHandler<KickTournamentTeamMemberCommand, Response<TournamentTeamActionResponseDto>>
{
    public async Task<Response<TournamentTeamActionResponseDto>> Handle(
        KickTournamentTeamMemberCommand request,
        CancellationToken cancellationToken)
    {
        var result = await service.KickTeamMemberAsync(
            request.CharacterId,
            request.TournamentId,
            request.TeamId,
            request.ParticipantId,
            cancellationToken);

        return result is null
            ? Response<TournamentTeamActionResponseDto>.Fail("Tournament team member kick failed.")
            : Response<TournamentTeamActionResponseDto>.Success(mapper.Map<TournamentTeamActionResponseDto>(result));
    }
}
