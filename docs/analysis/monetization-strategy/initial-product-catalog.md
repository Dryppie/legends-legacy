# LegendsLegacy — Initial Product Catalog

Updated 11 September 2026. Nobility game functionality is implemented for alpha; cash checkout is disabled. Operators can grant Signets and players can trade or redeem them. The prices below are planned cash offers, not products currently available for purchase. See the [subscription specification](subscription-specification.md) for product rules and the [alpha implementation report](nobility-alpha-implementation.md) for setup and verification limits. The two permanent cosmetic packs remain proposals.

**Nobility** is the membership, **Noble** is the active player status, and **Signet** is the one-word item name. A Signet grants one calendar month of Nobility when redeemed; show that duration separately from its name.

## Alpha availability

The alpha grant migration gives **one Signet to each existing character**, including guests, exactly once when applied. Later-created characters receive no automatic gift; guests must register before redemption or trading. Use **LiveOps → player → Alpha Signets** for additional grants to registered players. Granting 12 produces 12 identical tradable Signets, with no automatic activation. Players redeem a quantity in **Settings → Nobility** or trade through **Marketplace → Signets**. No cash-support history badge is awarded for alpha grants or market purchases.

## Planned cash products

| Product | EUR price | USD price | What the buyer receives | Status |
| --- | ---: | ---: | --- | --- |
| **Signet** | **€4.99** | **$4.99** | **1 tradable Signet**. Redeeming it grants one calendar month of Nobility. Buying it does not activate membership. | Item/gameplay implemented; cash purchase deferred. |
| **12 Signets** | **€49.99** | **$49.99** | **12 identical tradable Signets**. Redeem any quantity or sell the Signets individually. No automatic activation or annual renewal. | Quantity grant/redemption implemented; cash bundle deferred. |
| **Legacy Supporter Pack** | **€19.99** | **$19.99** | Permanent Legacy Supporter badge and **1 profile header**. Designs are distinct from Nobility and Heraldry. | Proposed cosmetic addition; not yet selected for launch. |
| **Heraldry Collection** | **€7.99** | **$7.99** | **3 permanent profile headers**, fully previewed before purchase. | Proposed cosmetic addition; not yet selected for launch. |

Planned EUR prices follow the specification's tax-inclusive convention. Planned USD prices exclude applicable sales tax, which must be shown before payment. Stripe product/price identifiers and tax configuration remain future commerce work; no payment credentials or price mappings are required for alpha grants.

## What Nobility provides

Redeeming a Signet from any source grants the same Nobility benefits, including alpha grants and market purchases. The player is a Noble while membership is active:

| Benefit | Included amount |
| --- | --- |
| Offline combat retention | **7 days / 168 hours** |
| Saved Essence loadouts | **6 total**, including 3 subscription slots |
| Saved equipment loadouts | **6 total**, including 3 subscription slots |
| Arena ticket capacity | **8 maximum**; unchanged regeneration and no instant ticket grant |
| Creature Focus | Change every **2 hours** |
| Market capacity | **30 sell listings and 30 buy orders**, separately |
| Daily Prophecy rerolls | **4 total**, costing **0 / 0 / 40 / 80 Fate Echo** |
| Active cosmetics | Optional **◆ Noble icon before the character name** |

The subscription specification governs full eligibility, expiry and retention rules. Nobility grants no XP bonuses or daily resources, and promises no new cosmetics every month.

## Purchase, ownership and trading

- Both Nobility offers deliver the **same one-month item**. The twelve-Signet bundle is a quantity discount, not a stronger membership tier or a separate annual item.
- Select a quantity and click **Redeem** once; Nobility activates or extends immediately without a separate confirmation. Months accumulate; benefit limits do not multiply. Twelve Signets grant twelve calendar months, not twelve fixed 30-day periods.
- Unredeemed Signets can be sold and resold for **Cinders** through the player market at player-set prices and normal fees. Listed Signets are reserved and cannot also be redeemed. Unused Signets do not expire with membership.
- A player obtaining a Signet through the market receives every membership benefit. The future permanent **“Supported LegendsLegacy” history badge** belongs to the original qualifying cash Signet purchaser and does not transfer with the Signet. That badge is deferred with cash purchasing.
- If selected for launch, permanent cosmetic packs are **account-bound, non-tradable and purchasable once per account**. Their appearance entitlements last for the operating life of the service and remain available without Nobility. They do not grant Signets, XP bonuses or gameplay resources.
- The **Legacy Supporter Pack badge** is its own cosmetic entitlement, distinct from the cash-Signet purchaser's history badge and the active Noble badge. Cosmetics never count as earned titles, achievements or competitive ranks.
- Future cash checkout uses explicit purchases. Recurring monthly Signet delivery remains optional future billing behavior; it must not silently redeem Signets or enable annual renewal.

## Scope of this catalog

Older Wayfarer/Warden/Founder tiers, Patron's Oath, permanent loadout expansions, rename services, individual cosmetic price ranges, guild cosmetics and seasonal tracks are historical ideas, not current launch products. No fixed total-spending ceiling is claimed for repeatable Signet purchases; purchase/resale limits remain an unresolved policy in the subscription specification.

The [implementation plan](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/subscription-implementation-plan.md) covers Nobility. If the two permanent cosmetic packs are selected, add their exact artwork, permanent cosmetic ownership and fulfillment to the shared commerce foundation before listing them for sale.
