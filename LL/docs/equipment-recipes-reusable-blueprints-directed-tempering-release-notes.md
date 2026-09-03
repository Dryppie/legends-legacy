# Reusable Equipment Blueprints

**Superseded 3 September 2026:** this document is historical. Player crafting, tempering, reusable Blueprint items, and the later equipment Forge have been removed. Current gear comes from authored grants and drops and cannot be upgraded. See the [Forge removal](../../docs/design/equipment-forge-removal.md) and [current specification](../../docs/design/equipment-specification.md).

Equipment crafting now starts from the item the player actually wants to make:
Dagger, Greatsword, Cloth Robe, Tower Shield, and the other concrete equipment
Recipes are listed directly.

Blueprints are permanent reusable designs rather than Recipe-specific variants.
Learning Venom, for example, makes Venom-Touched designs available on every
compatible weapon. Broad role-oriented Blueprints cover multiple item families,
while special identities such as Hivefang can remain tightly constrained.

The selected Recipe and optional Blueprint are composed into a full preview
before crafting. The screen shows the resulting name, readable attribute
ranges, weapon behavior, quality odds, starting Potential, tempering direction,
and total materials.

Crafted equipment stores its Recipe and optional Blueprint identities. Combat,
item tooltips, achievements, and Tempering resolve the same composed design, so
the preview and runtime item stay consistent.

Migration
`20260723161332_EquipmentRecipesReusableBlueprintsDirectedTempering` preserves
existing Blueprint provenance, maps old crafted items to concrete Recipes, and
collapses duplicate per-Recipe Blueprint unlocks into one reusable unlock.
