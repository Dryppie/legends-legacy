using AutoMapper;
using Domain.Models.Essences;

namespace Application.UseCases.Essences.Dtos;

public sealed class EssenceCatalogMappingProfile : Profile
{
    public EssenceCatalogMappingProfile()
    {
        CreateMap<EssenceCatalog, EssenceCatalogDto>().ConvertUsing<EssenceCatalogConverter>();
    }
}

public sealed class EssenceCatalogConverter : ITypeConverter<EssenceCatalog, EssenceCatalogDto>
{
    public EssenceCatalogDto Convert(EssenceCatalog source, EssenceCatalogDto destination, ResolutionContext context) =>
        new(source.Essences.Select(x => context.Mapper.Map<EssenceDefinitionDto>(x)).ToList(), source.TagsByCategory);
}
