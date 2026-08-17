using Application.UseCases.Administration.Dtos;
using AutoMapper;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Mappings;

public sealed class AdministrationMappingProfile : Profile
{
    public AdministrationMappingProfile()
    {
        CreateMap<PlayerAdministrationSnapshot, PlayerAdministrationDto>();
        CreateMap<AccountRestriction, AccountRestrictionDto>();
    }
}
