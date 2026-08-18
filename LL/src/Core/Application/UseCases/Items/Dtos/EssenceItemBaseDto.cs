using Application.Common.Mappings;
using Application.Interfaces.Services.LL.Essences;
using Application.UseCases.Essences.Dtos;
using AutoMapper;
using Domain.Models.Items.EssenceItems;

namespace Application.UseCases.Items.Dtos;

public class EssenceItemBaseDto : ItemBaseDto, IMapFrom<EssenceItemBase>
{
    public string EssenceDefinitionId { get; set; } = string.Empty;
    public int DismantleDustAmount { get; set; }
    public EssenceDefinitionDto? Essence { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<EssenceItemBase, EssenceItemBaseDto>()
            .ConvertUsing<EssenceItemBaseConverter>();
    }
}

public sealed class EssenceItemBaseConverter : ITypeConverter<EssenceItemBase, EssenceItemBaseDto>
{
    private readonly IEssenceDefinitionRepository? _definitions;

    public EssenceItemBaseConverter()
    {
    }

    public EssenceItemBaseConverter(IEssenceDefinitionRepository definitions)
    {
        _definitions = definitions;
    }

    public EssenceItemBaseDto Convert(EssenceItemBase source, EssenceItemBaseDto destination, ResolutionContext context)
    {
        var essenceDefinitionId = string.IsNullOrWhiteSpace(source.EssenceDefinitionId)
            ? InferDefinitionIdFromItemBaseId(source.Id)
            : source.EssenceDefinitionId;
        var essence = _definitions is null || string.IsNullOrWhiteSpace(essenceDefinitionId)
            ? null
            : _definitions.GetById(essenceDefinitionId);

        return new EssenceItemBaseDto
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Stackable = source.Stackable,
            IsBound = source.IsBound,
            ItemType = source.ItemType,
            Rarity = source.Rarity,
            EssenceDefinitionId = essenceDefinitionId,
            DismantleDustAmount = source.DismantleDustAmount,
            Essence = essence is null ? null : context.Mapper.Map<EssenceDefinitionDto>(essence)
        };
    }

    private static string InferDefinitionIdFromItemBaseId(string itemBaseId)
    {
        const string itemPrefix = "item.";
        return itemBaseId.StartsWith(itemPrefix, StringComparison.OrdinalIgnoreCase)
            ? itemBaseId[itemPrefix.Length..]
            : string.Empty;
    }
}
