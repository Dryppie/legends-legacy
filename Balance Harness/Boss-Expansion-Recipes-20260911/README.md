# Serevin and Serath expansion recipes

11 September 2026. These four JSON files are byte-for-byte exports of the discovery-frozen primary and probe-frozen anchor from each completed study. They retain the complete party, identities, equipment, training, ordered Essences and all 20 target confirmation seeds. See the [coverage review](../Boss-Coverage-Expansion-Review.md) for the fixed protocol, all-floor limitations and final verification.

**Progression clarification after sealing:** the intended floor-11 minimum is seven Essences per character, following approximately five to six at floor 10. The four-slot Serevin exports below are preserved diagnostic evidence of clears below that target, not the recommended progression budget. Their selection came from a search-headroom rule that omitted this requirement. The original recipe bytes and sealed observations remain unchanged.

| Exact recipe | Frozen role | Fresh target clears | Party and budget |
| --- | --- | ---: | --- |
| [Serevin primary](serevin-4-slots-primary.json) | Discovery primary `1c18bf10a62ca…` | 10/20 | 10 characters; 4 Essences each; level 30, tier 1, rank 1, Standard quality |
| [Serevin anchor](serevin-4-slots-anchor.json) | Probe anchor `d77927fe9757…` | 1/20 | Same fixed progression, gear and complete-party requirements |
| [Serath primary](serath-9-slots-primary.json) | Discovery primary `e72a8f4a8c4a…` | 7/20 | 15 characters; 9 Essences each; level 80, tier 2, rank 4, Fine quality |
| [Serath anchor](serath-9-slots-anchor.json) | Probe anchor `87cc6205ce50…` | 7/20 | Same fixed progression, gear and complete-party requirements |

The Serevin primary gained nine winning seeds and lost none against its anchor. Its observed success is still only 50%, with a pointwise Wilson 95% interval of 29.9–70.1%. It also cleared 0/20 on floor 8 where a retained control cleared 20/20. This below-target specialist has substantial transfer losses and must not be promoted as the intended floor-11 progression budget.

The Serath primary is itself a retained generalist control; all twelve method/restart arms selected that same discovery winner. It gained four target seeds and lost four against its anchor. Both cleared 7/20; the strongest retained control cleared 9/20. The comparison did not establish a new-method improvement. A separately observed 12/20 finalist remains exploratory and is not promoted into this primary/anchor publication.

All 76 frozen finalist target recipes remain available in the campaign's [complete recipe library](../../TestResults/balance/tower-boss-expansion-20260911/recipe-library/index.json). The [analysis](../../TestResults/balance/tower-boss-expansion-20260911/analysis/summary.md) retains every finalist's all-floor observations, every retained-control comparison, paired gained/lost counts and negative transfer. Context aliases, repeated method winners and joint/graph duplicate executions are not extra samples.

Essences are level 1, unascended and unevolved; styles are absent. Ownership is hypothetical and acquisition cost is not modeled. Preserve the whole recipe, including later party groups and ordered Essence slots. These experiments do not establish an optimum, a validated mechanic interaction or a reliable all-floor build.

Each of these exact exports reproduced all 20 archived target fights through the ordinary Tower path. One victory and one defeat per recipe also matched detailed replay after normalizing only the added event log. These 88 repeats are parity checks, not additional confirmation evidence. There were no observed draws in these four target vectors; no draw example was fabricated. Their seeds are now used and must be excluded from future fresh validation.

| File | SHA-256 of exact exported bytes |
| --- | --- |
| `serevin-4-slots-primary.json` | `cf74e56caa7128c75228a289212adc7fefd487a7b499dd3aeca1dc08da13f5ec` |
| `serevin-4-slots-anchor.json` | `aa6660f9178856bcf5529530be26c65065e5420d95fdcef2c09e175f2c3061e5` |
| `serath-9-slots-primary.json` | `3fc646fa4148b77d8a53bb6001c351cda61d7d0b613fc20175d38ee6619968ba` |
| `serath-9-slots-anchor.json` | `4f75928ce9e3dbb02d869f1c05e4911d59bdc4ccfbde0fa7e6889947c2914e8d` |

Git treats these four JSON exports as exact bytes, preserving their original CRLF line endings and hashes across checkouts. Publication here means reviewed local repository files. No production content, configuration, database or deployed service was changed.
