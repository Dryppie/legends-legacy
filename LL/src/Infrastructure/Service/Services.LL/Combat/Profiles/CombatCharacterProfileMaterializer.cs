using Application.Interfaces.Services.LL.CombatProfiles;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.PowerRatings;

namespace Services.LL.Combat.Profiles;

public sealed record CombatCharacterProfileMaterializationRequest(
    string Id,
    string TeamId,
    int SlotIndex,
    string Name,
    string Family,
    string Role,
    CombatContentType ContentType,
    CanonicalPartyProfile EquipmentProfile,
    CanonicalEquipmentProgressionRung ProgressionRung,
    IReadOnlyList<string> EssenceIds,
    int PartyNumber = 1,
    int PartySlotIndex = 0,
    string? SourcePartyProfileId = null);

public sealed class CombatCharacterProfileMaterializer(
    CanonicalEquipmentBuildFactory canonicalBuilds,
    ICombatPreparationPipeline preparationPipeline,
    IEssenceDefinitionRepository essenceDefinitions)
{
    public SnapshotCombatantRequest CreateSnapshotRequest(
        CombatCharacterProfile profile,
        int? partyNumber = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var resolvedPartyNumber = partyNumber ?? profile.PartyNumber;
        if (resolvedPartyNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(partyNumber));
        if (!Enum.TryParse<CombatContentType>(profile.ContentType, true, out var contentType))
            throw new ArgumentException($"Unknown combat content type '{profile.ContentType}'.", nameof(profile));
        if (!Enum.TryParse<CanonicalPartyProfile>(profile.EquipmentProfile, true, out var equipmentProfile))
            throw new ArgumentException($"Unknown equipment profile '{profile.EquipmentProfile}'.", nameof(profile));
        if (!Enum.TryParse<Rarity>(profile.EquipmentRarity, true, out var rarity)
            || !Enum.TryParse<ItemQuality>(
                profile.EquipmentQuality,
                true,
                out var quality))
        {
            throw new ArgumentException("The profile equipment rarity or quality is invalid.", nameof(profile));
        }

        var rung = canonicalBuilds.GetProgressionLadder().SingleOrDefault(candidate =>
            candidate.Tier == profile.EquipmentTier
            && candidate.Rarity == rarity
            && candidate.Quality == quality)
            ?? throw new ArgumentException(
                $"No canonical progression rung matches profile '{profile.Id}'.",
                nameof(profile));
        var request = new CombatCharacterProfileMaterializationRequest(
            profile.Id,
            profile.TeamId,
            profile.SlotIndex,
            profile.Name,
            profile.Family,
            profile.Role,
            contentType,
            equipmentProfile,
            rung,
            profile.EssenceIds,
            resolvedPartyNumber,
            profile.PartySlotIndex,
            profile.SourcePartyProfileId);
        var context = CreateContext(request);
        return new SnapshotCombatantRequest(
            context.Snapshot,
            new CombatParticipantSlot(
                profile.Id,
                context.Snapshot.CharacterId,
                CombatSide.Friendly,
                resolvedPartyNumber));
    }

    public async Task<IReadOnlyList<CombatCharacterProfile>> MaterializeTeamAsync(
        IReadOnlyList<CombatCharacterProfileMaterializationRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
            throw new ArgumentException("A profile team must contain at least one character.", nameof(requests));
        if (requests.Select(request => request.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != requests.Count)
            throw new ArgumentException("Profile IDs must be unique within a team.", nameof(requests));
        if (requests.Select(request => request.SlotIndex).Distinct().Count() != requests.Count)
            throw new ArgumentException("Profile slot indices must be unique within a team.", nameof(requests));

        var contexts = requests.Select(CreateContext).ToArray();
        var contentType = requests[0].ContentType;
        if (requests.Any(request => request.ContentType != contentType))
            throw new ArgumentException("Every character in a profile team must use the same content type.", nameof(requests));

        var prepared = await preparationPipeline.PrepareAsync(
            contentType,
            contexts.Select(context => new CombatantPreparationRequest(
                new CombatParticipantSlot(
                    context.Request.Id,
                    context.Snapshot.CharacterId,
                    CombatSide.Friendly,
                    context.Request.PartyNumber),
                new SnapshotCombatantPreparationSource(context.Snapshot))).ToArray(),
            cancellationToken);

        return contexts.Zip(prepared, (context, participant) =>
            ToProfile(context, participant.Combatant)).ToArray();
    }

    private MaterializationContext CreateContext(CombatCharacterProfileMaterializationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.TeamId))
            throw new ArgumentException("Profile and team IDs are required.", nameof(request));
        if (request.SlotIndex < 0)
            throw new ArgumentException("Profile slot indices cannot be negative.", nameof(request));
        if (request.PartyNumber < 1 || request.PartySlotIndex is < 0 or >= 5)
            throw new ArgumentException("Profile party assignments are invalid.", nameof(request));

        var build = canonicalBuilds.CreateBuild(
            request.EquipmentProfile,
            request.ProgressionRung,
            request.EssenceIds);
        var snapshotId = CombatCharacterProfileIdentity.CreateDeterministicGuid($"{request.Id}:snapshot");
        var characterId = CombatCharacterProfileIdentity.CreateDeterministicGuid($"{request.Id}:character");
        var snapshot = new CharacterSnapshot
        {
            Id = snapshotId,
            CharacterId = characterId,
            Name = request.Name,
            Level = build.Character.Level,
            BaseAttributes = build.Character.BaseAttributes.Select(attribute =>
                new EntityAttributeSnapshot
                {
                    CharacterSnapshotId = snapshotId,
                    AttributeType = attribute.AttributeType,
                    Value = attribute.Value
                }).ToArray(),
            Equipment = build.Equipment.Select(equipment =>
                EquipmentSnapshot.From(ToSlot(equipment.EquipmentBase.EquipmentType), equipment)).ToArray(),
            EquippedEssences = build.EquippedEssences.Select((essence, essenceIndex) =>
                EquippedEssenceSnapshot.From(snapshotId, essenceIndex, essence)).ToArray()
        };

        return new MaterializationContext(request, build, snapshot);
    }

    private CombatCharacterProfile ToProfile(MaterializationContext context, CombatEntity combatant)
    {
        var request = context.Request;
        var abilityIds = combatant.NativeAbilityIds
            .Concat(combatant.EquippedEssences.SelectMany(essence =>
            {
                var definition = essenceDefinitions.GetById(essence.EssenceDefinitionId);
                return definition is null
                    ? []
                    : new[] { definition.ActiveAbility.Id, definition.PassiveAbility.Id };
            }))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var equipment = combatant.Equipment
            .OrderBy(item => ToSlot(item.EquipmentBase.EquipmentType))
            .Select(item => new CombatCharacterPreparedEquipment(
                item.ItemBaseId,
                ToSlot(item.EquipmentBase.EquipmentType).ToString(),
                item.Tier,
                item.Rarity.ToString(),
                item.Quality.ToString(),
                item.BaseRecipeId,
                item.BlueprintId,
                item.EquipmentSetId))
            .ToArray();
        var attributes = Enum.GetValues<AttributeType>()
            .ToDictionary(attribute => attribute.ToString(), combatant.GetAttributeValue);
        var preview = new CombatCharacterPreparedPreview(
            IsProductionReady: true,
            combatant.Level,
            combatant.GetCurrentHealthValue(),
            combatant.GetAttributeValue(AttributeType.MaxHealth),
            attributes,
            abilityIds,
            combatant.Tags.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            combatant.EquippedEssences.Select(essence => essence.EssenceDefinitionId).ToArray(),
            equipment);

        return new CombatCharacterProfile(
            request.Id,
            request.TeamId,
            request.SlotIndex,
            request.Name,
            request.Family,
            request.Role,
            request.ContentType.ToString(),
            request.ProgressionRung.Tier,
            request.ProgressionRung.Rarity.ToString(),
            request.ProgressionRung.Quality.ToString(),
            request.EquipmentProfile.ToString(),
            request.EssenceIds.ToArray(),
            context.Build.Rating.Overall,
            CombatRatingDisplay.FromRaw(context.Build.Rating.Overall),
            preview,
            request.PartyNumber,
            request.PartySlotIndex,
            request.SourcePartyProfileId);
    }

    private static EquipmentSlotType ToSlot(EquipmentType type) => type switch
    {
        EquipmentType.Head => EquipmentSlotType.Head,
        EquipmentType.Relic => EquipmentSlotType.Relic,
        EquipmentType.Chest => EquipmentSlotType.Chest,
        EquipmentType.Necklace => EquipmentSlotType.Necklace,
        EquipmentType.Legs => EquipmentSlotType.Legs,
        EquipmentType.Ring => EquipmentSlotType.Ring,
        EquipmentType.OneHanded or EquipmentType.TwoHanded => EquipmentSlotType.MainHand,
        EquipmentType.OffHand => EquipmentSlotType.OffHand,
        EquipmentType.Tool => EquipmentSlotType.Tool,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private sealed record MaterializationContext(
        CombatCharacterProfileMaterializationRequest Request,
        CanonicalEquipmentBuild Build,
        CharacterSnapshot Snapshot);
}
