using Application.Interfaces.Services.LL.Entities;
using Application.UseCases.Characters.Dtos;
using AutoMapper;
using Common.Primitives;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Characters.Queries.GetCharacterOverviewByName;
public record GetCharacterOverviewByNameQuery(string CharacterName) : IRequest<Response<CharacterOverviewDto>>;

public class GetCharacterOverviewByNameQueryHandler : IRequestHandler<GetCharacterOverviewByNameQuery, Response<CharacterOverviewDto>>
{
    private readonly ICharacterService _characterService;
    private readonly IMapper _mapper;


    public GetCharacterOverviewByNameQueryHandler(ICharacterService characterService, IMapper mapper)
    {
        _characterService = characterService;
        _mapper = mapper;
    }

    public async Task<Response<CharacterOverviewDto>> Handle(GetCharacterOverviewByNameQuery request, CancellationToken cancellationToken)
    {
        var character = await _characterService.GetCharacterOverviewByNameAsync(request.CharacterName, cancellationToken);

        return character != null
            ? Response<CharacterOverviewDto>.Success(_mapper.Map<CharacterOverviewDto>(character))
            : Response<CharacterOverviewDto>.Fail("Failed to get character overview.");
    }
}
