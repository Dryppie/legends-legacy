# Reviewed boss-pilot recipes

These five complete Tower scenarios are unchanged copies of frozen confirmation recipes from the [boss pilot](../Boss-Specific-Essence-Loadout-Pilot-Review.md). Each contains every required character, ordered Essences, equipment, level, preparation assumptions and the original 20 confirmation seeds. The copied files match their archived bytes; changing teammates or gear creates a different experiment.

| Scenario | Observed target result | Use and limits |
| --- | --- | --- |
| [Kodoku specialist](kodoku-four-slots-specialist.json) | 20/20; retained best 10/20 | Strong target result with substantial losses on other floors |
| [Kodoku prevention view](kodoku-four-slots-prevention-view.json) | 17/20 | Frozen measured-prevention representative; the label does not prove what caused survival |
| [Kodoku character-five change](kodoku-four-slots-character-five-change.json) | 14/20 | Only absolute character slot 5 differs from the retained reference; preserves more early-floor clears |
| [Kodoku retained reference](kodoku-four-slots-retained-reference.json) | 10/20 | Strongest observed historical control on this target schedule |
| [Eydis rare-clear alternative](eydis-five-slots-rare-clears.json) | 2/20; all controls 0/20 | Exploratory and unreliable; substantial transfer weaknesses |

Kodoku uses four Essences per character, level 30 and Uncommon Standard tier-1/rank-1 gear. Eydis uses five Essences, level 40 and Uncommon Standard tier-1/rank-2 gear. Essences are level 1, unascended and unevolved; ownership is hypothetical and Combat Styles are disabled. These are separate provisional budgets. The pilot review contains uncertainty, every retained-control comparison and the 15-floor transfer limitations. No successful seven-slot Nhalia recipe was found, and Morrowmaw received screening only.

To repeat a complete recipe using the captured executable and content, run from the repository root with a new output directory:

```powershell
$pilotPath = 'TestResults/balance/tower-boss-pilot-20260910'
dotnet "$pilotPath/executable/BalanceHarness.dll" tower `
  --content-root "$pilotPath/frozen-root" `
  --scenario 'Balance Harness/Boss-Pilot-Recipes-20260910/kodoku-four-slots-character-five-change.json' `
  --output 'TestResults/balance/kodoku-character-five-repeat'
```

This executes 20 additional fights on the recorded seeds. Repeating them checks reproducibility and does not add independent confirmation evidence. The original package remains sealed. A current rebuild or changed content can produce a different experiment; the captured assemblies, runtime and platform are recorded in the package's `scope.json`.

The package also exports all 96 frozen finalists under `recipe-library/`, including unsuccessful alternatives, and keeps every all-floor scenario and trial in the individual studies. The five files here are a compact set for reviewing observed tradeoffs, not a new confirmation selection or a universal loadout ranking.
