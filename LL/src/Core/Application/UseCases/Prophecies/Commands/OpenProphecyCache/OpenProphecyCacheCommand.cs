using Application.Interfaces.Services.LL.Prophecies;
using Application.MediatR.Markers;
using Application.UseCases.Prophecies.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Prophecies.Commands.OpenProphecyCache;

public sealed record OpenProphecyCacheCommand(Guid CharacterId, string CacheItemId) : ICommand<Response<OpenProphecyCacheResponseDto>>;

public sealed class OpenProphecyCacheCommandHandler : IRequestHandler<OpenProphecyCacheCommand, Response<OpenProphecyCacheResponseDto>>
{
    private readonly IProphecyService _prophecyService;
    private readonly IMapper _mapper;

    public OpenProphecyCacheCommandHandler(IProphecyService prophecyService, IMapper mapper)
    {
        _prophecyService = prophecyService;
        _mapper = mapper;
    }

    public async Task<Response<OpenProphecyCacheResponseDto>> Handle(OpenProphecyCacheCommand request, CancellationToken cancellationToken)
    {
        var result = await _prophecyService.OpenCacheAsync(
            request.CharacterId,
            request.CacheItemId,
            cancellationToken);

        if (!result.Succeeded || result.Value is null)
        {
            return Response<OpenProphecyCacheResponseDto>.Fail(result.Error ?? "Could not open prophecy cache.");
        }

        return Response<OpenProphecyCacheResponseDto>.Success(_mapper.Map<OpenProphecyCacheResponseDto>(result.Value));
    }
}
