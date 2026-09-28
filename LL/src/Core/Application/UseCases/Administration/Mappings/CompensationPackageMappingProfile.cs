using AutoMapper;
using Application.UseCases.Administration.Dtos;
using Domain.Models.Administration;
namespace Application.UseCases.Administration.Mappings;
public sealed class CompensationPackageMappingProfile : Profile
{
    public CompensationPackageMappingProfile()
    {
        CreateMap<CompensationPackageDefinition, CompensationPackageDto>();
        CreateMap<CompensationPackageOperation, CompensationPackageReceiptDto>()
            .ForCtorParam("OperationId", o => o.MapFrom(x => x.Action.Id))
            .ForCtorParam("CharacterId", o => o.MapFrom(x => x.Action.TargetCharacterId))
            .ForCtorParam("AccountId", o => o.MapFrom(x => x.Action.TargetAccountId));
    }
}
