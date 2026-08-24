using Application.Interfaces.Services.LL;
using Application.MediatR.Markers;
using Application.UseCases.Guilds.Dtos.Requests;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Guilds.Commands.UpdateGuildDescription;

public record UpdateGuildDescriptionCommand(Guid CharacterId, UpdateGuildDescriptionDto Request) : ICommand<Response<bool>>;

public class UpdateGuildDescriptionCommandHandler : IRequestHandler<UpdateGuildDescriptionCommand, Response<bool>>
{
    private readonly IGuildService _guild;
    public UpdateGuildDescriptionCommandHandler(IGuildService guild)
    {
        _guild = guild;
    }

    public async Task<Response<bool>> Handle(UpdateGuildDescriptionCommand request, CancellationToken cancellationToken)
    {
        var updated = await _guild.UpdateDescriptionAsync(
            request.CharacterId,
            request.Request.Description,
            cancellationToken);
        if (!updated)
            return Response<bool>.Fail("Only the guild leader and officers can change the guild description.");

        return Response<bool>.Success(true);
    }
}
