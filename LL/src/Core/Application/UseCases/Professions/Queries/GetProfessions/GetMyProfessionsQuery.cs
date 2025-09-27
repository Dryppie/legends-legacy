using Application.Interfaces.Services.LL.Professions;
using Application.MediatR.Markers;
using Application.UseCases.Professions.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;

namespace Application.UseCases.Professions.Queries.GetProfessions;
public record GetMyProfessionsQuery(Guid CharacterId) : IQuery<Response<List<ProfessionDto>>>;
public class GetMyProfessionsQueryHandler : IRequestHandler<GetMyProfessionsQuery, Response<List<ProfessionDto>>>
{
    private readonly IProfessionService _professionService;
    private readonly IMapper _mapper;

    public GetMyProfessionsQueryHandler(IProfessionService professionService, IMapper mapper)
    {
        _professionService = professionService;
        _mapper = mapper;
    }

    public async Task<Response<List<ProfessionDto>>> Handle(GetMyProfessionsQuery request, CancellationToken cancellationToken)
    {
        var professions = await _professionService.GetProfessionsAsync(request.CharacterId, cancellationToken);
        if (professions == null || professions.Count == 0) return Response<List<ProfessionDto>>.Fail("No professions found for the character.");

        var professionDtos = _mapper.Map<List<ProfessionDto>>(professions);
        return Response<List<ProfessionDto>>.Success(professionDtos);
    }
}