# Monetization Strategy

For alpha setup, start with the implementation report. For the benefit list, read the subscription specification. Older reports are preserved as research; their conflicting recommendations do not override the current Nobility model.

**Status — 11 September 2026:** game functionality, operator grants, redemption and trading are implemented. A follow-up migration gives one Signet to every existing character, including guests, without activating membership. Signet catalog loading now supports the `Misc` type in JSON and persistence. Cash purchasing is disabled behind a stub for future Stripe integration. The follow-up migration and redemption passed disposable PostgreSQL tests; no shared database update or deployment was performed. PostgreSQL concurrency and deployed alpha smoke testing remain outstanding.

**Current benefits exclude all subscription XP bonuses and daily Sigil Fragment/Soulstone grants.** Historical strategy proposals do not override this change.

**Nobility** is the membership, **Noble** is the active player status, and a **Signet** is the tradable item that grants one calendar month of Nobility when redeemed.

| Document | Purpose and status |
| --- | --- |
| [Nobility alpha implementation](nobility-alpha-implementation.md) | Implemented game behavior, granting Signets to testers, schema and verification limits; purchasing remains disabled. |
| [Initial product catalog](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/initial-product-catalog.md) | Alpha availability, future cash offers and prices, and proposed permanent cosmetic packs. |
| [Subscription specification](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/subscription-specification.md) | Source of truth for Signet ownership, trading, benefits and expiry rules. |
| [Subscription implementation plan](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/subscription-implementation-plan.md) | Completed alpha scope, implemented design decisions, remaining verification and deferred Stripe work. |
| [September monetization audit](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/monetization-strategy-audit-2026-09-10.md) | Historical repository audit and product recommendations; its earlier membership and spending rules have been superseded. |
| [August monetization strategy](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/monetization-strategy.md) | Historical strategy based on the 25 August inspection. |
| [August purchase overview](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/purchase-overview.md) | Historical summary of the August strategy's proposed offers. |

The alpha implementation report distinguishes delivered game functionality from future purchasing work. These documents do not establish that anything is deployed or available for purchase.
