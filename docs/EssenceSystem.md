# Essence System

The Essence system is split between immutable content definitions and player-owned runtime state.

## Lifecycle

1. Eligible monsters can drop an Unbound Essence item.
2. The Unbound Essence stays in inventory and can be traded by systems that support item trading.
3. Absorbing the item consumes one inventory copy and creates a Soul Archive entry.
4. A Soul Archive entry is character-bound and cannot be traded.
5. Archived Essences do not grant power until they are assigned to an active loadout slot.
6. Attuned Essences grant attribute bonuses, passive effects, and access to active ability definitions.
7. Attuned Essences gain XP from eligible combat rewards. Inactive archived Essences do not.
8. Essence Dust can be spent for XP, but XP cannot pass the current Ascension Tier cap.
9. At the tier cap, the Essence can ascend by consuming the matching Monster Core.
10. Each Essence can evolve once by meeting its required Ascension Tier and consuming its catalyst.

## Data Model

Content lives in `LL/src/API/API.LL/Data/essences.json`.

Runtime state lives in the database:

- `PlayerEssence`: one absorbed Soul Archive entry per character and Essence definition.
- `EssenceLoadout`: saved attunement preset.
- `EssenceLoadoutSlot`: slot assignment inside a loadout.
- `MonsterResonance`: per-character monster resonance progress.
- `PlayerEssence.ActiveAbilityReadyAt`: nullable active ability readiness timestamp; `null` means ready.
- `EssenceItemBase.EssenceDefinitionId`: links an Unbound Essence item to content.

Important constraints:

- `PlayerEssence` is unique by `CharacterId + EssenceDefinitionId`.
- `EssenceLoadout` names are unique per character.
- Loadout slots are unique per loadout and capped to indexes `0-9`.
- Resonance is unique by `CharacterId + MonsterId`.

## Authoring Definitions

Add new Essence content to `essences.json`.

Each definition must include:

- Stable `id`, such as `essence.goblin_ambusher`.
- Stable `sourceMonsterId`, such as `monster.goblin`.
- Name, description, rarity, tags, attribute bonuses.
- One active ability.
- One passive ability.
- One evolution.
- Drop and resonance values.

Also add an Unbound Essence item to `items.json` with:

```json
{
  "id": "item.essence.example",
  "itemType": "Essence",
  "essenceDefinitionId": "essence.example",
  "stackable": true,
  "isBound": false,
  "dismantleDustAmount": 1
}
```

## Tags

Tags are normalized stable strings and validated at startup. Unknown tags fail definition loading.

Current categories include:

- `Species.*`
- `Role.*`
- `Range.*`
- `Element.*`
- `Pattern.*`
- `Defense.*`
- `Effect.*`
- `Control.*`
- `Status.*`
- `Resource.*`
- `Trigger.*`
- `Target.*`
- `Mechanic.*`

Tags help filtering, balancing, synergies, and conditions. They do not replace effect definitions.

## Scaling

Attribute bonuses and ability effects use simple formulas:

```text
baseValue + perLevel * (EssenceLevel - 1) + perAscensionTier * AscensionTier
```

Progression templates define reusable XP curves and balancing metadata. Templates should reduce repetition, not hide the Essence identity.

## Attribute Modifiers

Essence attribute bonuses are converted to normal backend stat modifiers before combat. The UI displays calculated values, but the backend remains the source of truth for stat math.

`EssenceModifierKind.Flat` creates a flat modifier. `EssenceModifierKind.Percent` creates an additive percentage modifier.

The attribute system is defined by `AttributeType` and `AttributeCatalog`. Essence content, equipment modifiers, combat temporary modifiers, and backend calculations use the same stat names:

- Primary: `Power`, `Fortitude`, `Precision`, `Spirit`
- Base and derived inputs: `MaxHealth`, `WeaponDamage`, `Armor`, `Resistance`, `CritChance`, `CritDamage`, `ArmorPenetration`, `MagicPenetration`
- Defensive: `DodgeChance`, `BlockChance`, `BlockEffectiveness`, `DamageReduction`, `StatusResistance`, `CrowdControlResistance`
- Recovery: `HealingPowerPercent`, `HealthRegeneration`, `LifeSteal`
- Utility: `Cooldown`
- Summons: `SummonPower`, `SummonHealth`

These are the actual stats now; the old combat attributes such as attack power, separate physical/magical defenses, mana-specific stats, elemental resistance buckets, and current health/mana entries are no longer authored as `AttributeType` values.

`MaxHealth` is the single authored health stat. Percentage-style health increases should use an additive or multiplicative modifier on `MaxHealth` rather than a separate percent stat.

The shared attribute calculator applies modifiers in deterministic phases:

```text
floor((baseValue + flatSum) * (1 + additivePercentSum) * multiplicativeProduct)
```

Equipment modifiers, Essence modifiers, and combat temporary modifiers use the same `AttributeModifierBase` model and `ModifierType` phases. Only active loadout Essences emit Essence modifiers; inactive Soul Archive entries do not contribute stats.

## Ascension

Tier caps:

- Tier 0: level 10
- Tier 1: level 20
- Tier 2: level 30
- Tier 3: level 40

Ascension is manual. The Essence must be at the current tier cap and consume the next tier Monster Core.

Dungeon completions award the Monster Core matching dungeon tiers 1-3 through the pending reward flow.

## Evolution

Each Essence has exactly one evolution. Evolution consumes the required catalyst, requires the configured Ascension Tier, and can happen only once.

Evolution modifiers are data-driven and should enhance the original role rather than replace it.

## Resonance

Each eligible failed kill increases `MonsterResonance`. The next drop roll uses:

```text
baseDropChance + min(maxResonanceBonus, resonanceValue * dropChanceBonusPerResonance)
```

On drop, resonance resets. Resonance is capped and does not guarantee drops unless a definition explicitly configures a 100% effective chance.

## Combat Integration

Combat setup asks the Essence bonus provider for active loadout modifiers and applies only those modifiers to combat attributes.

Character XP reward writing also grants Essence XP to active loadout Essences. Archived but inactive Essences do not gain XP.

The reusable ability definitions are converted by `IEssenceAbilityProvider` into normal combat `AbilityInstance` objects. Active abilities are wired to `OnAbilityUsed`; passive abilities use their configured triggers and combat cooldowns. Combat consumes those generated instances without loading player state from EF inside pure resolution code.

When an attuned Essence active ability is used, outcome processing records `PlayerEssence.ActiveAbilityReadyAt`. Later combat setup initializes that active ability's remaining cooldown from the stored timestamp.

The Essence combat mapper supports the authored effect vocabulary (`Damage`, `Heal`, `ApplyStatus`, `RemoveStatus`, `Cleanse`, `GrantBarrier`, `ModifyAttribute`, `RestoreResource`, `Summon`, `Taunt`, `ReflectDamage`, `AbsorbDamage`, and `TriggerSecondaryEffect`) through reusable actions rather than one C# class per Essence.

Creature combat entities receive `SourceMonsterId` and normalized Essence tags from the matching Essence definition. `SourceHasTag`, `TargetHasTag`, `IsSpecies`, source/target health, source/target status, random chance, and cooldown-ready conditions are mapped through reusable condition objects.

## API

Main endpoints:

- `GET /api/v1/essence/catalog`
- `GET /api/v1/essence/archive`
- `GET /api/v1/essence/loadouts`
- `GET /api/v1/essence/loadouts/active`
- `POST /api/v1/essence/items/{inventoryItemId}/absorb`
- `POST /api/v1/essence/items/{inventoryItemId}/dismantle`
- `POST /api/v1/essence/{playerEssenceId}/spend-dust`
- `POST /api/v1/essence/{playerEssenceId}/ascend`
- `POST /api/v1/essence/{playerEssenceId}/evolve`
- `POST /api/v1/essence/loadouts`
- `PUT /api/v1/essence/loadouts/{loadoutId}`
- `POST /api/v1/essence/loadouts/{loadoutId}/activate`
- `DELETE /api/v1/essence/loadouts/{loadoutId}`

DTOs expose calculated values and validation state instead of EF entities.

## Validation

Definitions are loaded by `JsonEssenceDefinitionRepository` and validated by `EssenceDefinitionValidator`.

Validation checks include:

- Duplicate IDs.
- Missing active/passive/evolution definitions.
- Unknown tags.
- Unknown attributes in Essence bonuses and ModifyAttribute effects.
- Unknown effect, condition, trigger, or target selector IDs.
- Missing effect IDs and duplicate effect IDs within an ability.
- Invalid Ascension Tier setup.
- Invalid evolution tier requirements.

Run validation by starting or building the API path that constructs the service provider, or by exercising `IEssenceDefinitionRepository` in tests.

## Adding a New Essence

1. Add the Essence definition to `essences.json`.
2. Use only tags from `EssenceTagCatalog`.
3. Use `AttributeType` values from `AttributeCatalog`.
4. Add an Unbound Essence item to `items.json`.
5. Add monster drop integration by referencing the definition's `sourceMonsterId`.
6. Build the backend and frontend.
7. Add or update focused tests for any new mechanics.
