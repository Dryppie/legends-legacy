using AutoMapper;
using Application.UseCases.Administration.Dtos;
using Domain.Models.Administration;

namespace Application.UseCases.Administration.Mappings;
public sealed class StateRefreshRecoveryMappingProfile : Profile
{
    public StateRefreshRecoveryMappingProfile() => CreateMap<StateRefreshRecoveryResult, StateRefreshRecoveryResultDto>();
}
