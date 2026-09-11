# Nobility — Subscription Specification

Current product rules, updated 11 September 2026. Nobility gameplay, Signet grants, redemption and trading are implemented for alpha. Cash purchasing is disabled; prices, billing, refunds and permanent cash-support recognition below describe future commerce. See the [alpha implementation report](nobility-alpha-implementation.md) for setup and verification limits. Implementation does not mean deployment: only disposable local databases were used to verify the grant migration; no shared database was changed.

**Names:** **Nobility** is the membership; a player with active Nobility is a **Noble**; a **Signet** is the tradable item consumed to grant or extend Nobility. The item name is one word, with its one-calendar-month duration shown separately. Noble is a membership status, not an earned gameplay title.

## Obtaining Signets

**During alpha:** applying `20260911153816_GrantAlphaSignetToExistingCharacters` gives **one tradable Signet to every existing character**, including guests, once. It preserves earlier grants and does not automatically grant items to characters created afterward. Guests must register before redeeming or trading. Operators can grant additional Signets to registered players through **LiveOps → player → Alpha Signets**. Players redeem them in **Settings → Nobility** or buy and sell them through the market's **Signets** category. Grants do not activate membership or confer cash-support recognition.

**Future cash offers — unavailable during alpha:**

Buying a Signet grants a tradable item. Redeeming it grants one calendar month of Nobility, making the player a Noble while membership is active. Buying or holding a Signet does not activate benefits.

| Purchase | Price | What the buyer receives |
| --- | --- | --- |
| Signet | €4.99 / $4.99 | 1 Signet, redeemable for one calendar month of Nobility. |
| 12 Signets | €49.99 / $49.99 | 12 identical Signets. No automatic activation. |

EUR pricing includes applicable VAT; USD pricing excludes applicable sales tax, disclosed before payment. The bundle provides the same monthly benefits at a lower price per Signet.

## Redeeming and selling Signets

- Each Signet grants **one calendar month** of membership. Twelve Signets grant twelve calendar months; do not substitute twelve 30-day periods and advertise them as a full year.
- Enter a quantity and click **Redeem** once to consume those Signets and activate or extend Nobility immediately. There is no separate player-facing preview or confirmation. The client obtains exact units and expiry internally; the server validates ownership and membership version before consumption. An interrupted request offers a retry of the same operation without consuming extra Signets.
- Redemption consumes the selected Signets and extends active membership using its original calendar anchor and total redeemed months; expired membership starts a new anchor at redemption. Individual and bulk redemption use the same calendar handling. Time accumulates; benefit limits never multiply.
- Players can redeem some Signets and sell the rest. For example, redeem 1 from a 12-pack and sell the remaining 11.
- Unredeemed Signets can be sold and resold on the player market for **Cinders**, at player-set prices with the normal market fee. Listing reserves the item so it cannot also be redeemed.
- A free player buying a Signet for Cinders receives the **same membership benefits** when redeeming it. The seller keeps the sale proceeds, less the market fee.
- Unused Signets do not expire when membership ends. Once redeemed, membership time is account-bound and cannot be turned back into a tradable item.

## Included benefits

| Benefit | Exact contents | Availability |
| --- | --- | --- |
| Offline retention | **7 days (168 hours)** of offline combat retention, compared with 24 hours free. | While subscribed. |
| Extra Essence loadouts | **+3 saved loadouts: 6 total**, compared with 3 free. | While subscribed. |
| Extra equipment loadouts | **+3 saved loadouts: 6 total**, compared with 3 free. | While subscribed. |
| Increased Arena ticket cap | **+3 ticket capacity: 8 maximum**, compared with 5 free. No instant ticket grant or faster regeneration. | While subscribed. |
| Creature Focus cooldown | Change Focus every **2 hours**, compared with 8 hours free. | While subscribed. |
| Market capacity | **30 active sell listings and 30 active buy orders**, compared with 10 of each free. These are separate limits; normal market fees remain. | While subscribed. |
| Extra free Prophecy reroll | **+1 free reroll and +1 total reroll: 2 free, 4 total per day**, compared with 1 free and 3 total for free players. Subscriber costs are **0 / 0 / 40 / 80 Fate Echo**, compared with 0 / 40 / 80 for free players. | While subscribed. |
| Combat XP bonus | **+5% combat XP**. Equipped Essences receive the increased combat XP through normal progression; no additional subscription Essence XP multiplier. | Progress earned while subscribed. |
| Dungeon mastery XP bonus | **+5% dungeon mastery XP**, up to the existing mastery cap. | Progress earned while subscribed. |
| Combat Style XP bonus | **+5% Combat Style XP**, up to the existing mastery cap. | Progress earned while subscribed. |
| Daily Sigil Fragments | **2 Sigil Fragments per day**. | Accrue while subscribed, including offline. |
| Daily Soulstones | **10 Soulstones per day**. | Accrue while subscribed, including offline. |
| Noble badge | One optional **◆** icon before the character name, including for players who redeem market-bought Signets. No text badge, border or profile header. | While subscribed. |

**Future direct-support recognition, not implemented for alpha:** the permanent “Supported LegendsLegacy” history badge belongs to the account making a successfully settled real-money Signet purchase, even if it sells the Signets. It does not transfer with an item. Market buyers and recipients of alpha grants receive every membership benefit above; the history badge records direct financial support. No spending or loyalty tiers.

Benefits are available throughout active Nobility regardless of the Signet's source. Daily resources accrue **once per account per UTC date with any active coverage**, including partial dates, and are delivered automatically after that date closes. The first character to redeem a Signet remains that account's daily reward recipient. Offline days remain eligible without a claim or login requirement. Redeeming more Signets extends membership without duplicating daily grants. There are no additional monthly gifts or promises of new cosmetics every month.

Progression bonuses apply to eligible progress earned while membership is active, including offline combat. They do not retroactively boost progress earned before activation or raise progression caps. Combat XP and Combat Style XP are separate tracks; apply the stated subscription bonus once to each eligible track. The added XP rounds down per eligible encounter share or Mastery completion award; a base award below 20 gains no extra XP.

## Display rules

- **Display Nobility** controls the optional active icon on profiles, general/whisper chat, designated guild roster entries and supported Tavern leaderboard entries. Turning it off hides Nobility expiry, the Combat XP note, and the perks toggle/list from other players viewing Character Overview. On your own profile, expiry and **Show perks** remain available, including when opening yourself through search; the icon and XP note still follow the display preference. This changes presentation only; membership benefits remain active. Overview perks start collapsed.
- Display the permanent history badge principally on the profile. Show at most one membership mark beside a social name.
- Keep supporter recognition distinct from earned titles, achievements, competitive ranks and moderator status. Players retain their earned title display.
- Decorations must not change leaderboard placement, message prominence, rarity colors or gameplay readability.
- Verify badge ownership on the server. Cosmetic benefits must never contribute to title counts, achievements, renown, collection progress or rankings.

## Account ownership

- Require a recoverable authenticated account for redemption and alpha grants. Item ownership can change through the market; redeemed benefits belong to the redeeming account.
- Maintain one membership entitlement per account with an extendable expiry. No stronger tiers or multiplied benefits for redeeming more Signets.

## Future billing

Cash checkout currently returns an unavailable response through a small purchase gateway stub. Stripe checkout, fulfillment, payment history, refunds and recurring billing are not implemented.

- Treat the one-Signet and 12-Signet bundle as clearly labeled purchases; the 12-pack does not start an automatic annual renewal.
- If recurring monthly billing is offered, each successful payment delivers one Signet. Explain that delivery does not redeem it automatically; show the next charge and delivery date.
- Cancellation stops future recurring charges and deliveries. Existing Signets and redeemed membership time remain available.
- Require a recoverable authenticated account for cash purchases.
- Provide straightforward subscription management, payment history and clearly stated refund terms.

## Expiration

- On expiry, preserve the eligible unsettled Nobility combat window, capped at 168 hours, for later bounded resolution. Subsequent free combat uses the ordinary 24-hour retention window. Saved-loadout limits return to 3 Essence and 3 equipment loadouts, and the Arena ticket cap returns to 5.
- Preserve extra saved loadouts for viewing and copying into free slots. Do not delete their contents or change an already committed combat snapshot.
- Retain Arena tickets already accrued above the free cap. Regeneration resumes once the ticket balance is below that cap.
- Market limits return to 10 sell listings and 10 buy orders. Existing orders remain valid until filled, cancelled or expired; new orders require the corresponding active count to be below the free limit.
- Creature Focus returns to an 8-hour cooldown measured from the last Focus change. Prophecy rerolls return to 1 free and 3 total per day at 0 / 40 / 80 Fate Echo. Membership changes do not reset that day's used rerolls or charge for previously free rerolls.
- Stop earning subscription XP bonuses and daily resources after expiry. Preserve all XP, mastery progress, Sigil Fragments and Soulstones already earned, including pending automatic grants for eligible subscribed days.
- On expiry, remove the active badge and subscription-only decorations from display; restore the free appearance.
- Retain the player's cosmetic selections so they can be restored on resubscription.
- Preserve earned titles, items, Essences, builds, history and separately purchased permanent cosmetics.

## Future refunds and purchase recovery

These are requirements for enabling cash sales, not available alpha operations. Alpha issuance, market movements and redemption already retain individual Signet provenance.
- For refunds of untraded, unredeemed Signets, revoke the corresponding items once. Sold or redeemed Signets require a traced refund/chargeback review; do not automatically remove an innocent market buyer's membership or earned rewards because the original purchaser disputes payment.
- Track each purchased Signet from its original payment through market transfers and redemption. A Signet must never be both sold and redeemed, or redeemed twice. Define responsibility for traded-Signet refund losses before cash launch.
- Remove the permanent direct-support history badge only if the original cash purchaser has no remaining valid qualifying purchase.

## Market tradeoff

Once cash purchases are enabled, this allows **real money → Signets → Cinders → equipment and progression**. Market sales transfer existing Cinders rather than creating them, but spending can still buy additional wealth. The earlier claim that €500 cannot buy more gameplay advantage no longer applies to unrestricted Signet sales. A purchase or resale limit has not been decided; that policy needs to be settled before cash launch, including how it applies across accounts. Alpha uses operator-issued Signets and ordinary market access rules.

## Not included

- Drop-rate boosts, direct equipment or Essence grants, and currency or resource grants beyond the daily Sigil Fragments and Soulstones listed above. Cinders received from selling Signets are player-market proceeds.
- Additional equipped Essence slots, Soul Archive storage or inventory capacity. Extra loadouts store configurations only.
- Instant Arena ticket grants, faster ticket regeneration, direct dungeon entry grants or retries. Daily Sigil Fragments can be assembled into sigils through the normal system.
- Reward multipliers or cooldown reductions beyond the three XP bonuses and Creature Focus cooldown listed above.
- Exclusive gameplay, priority moderation, developer access, balance influence or early competitive access.

**Free players retain the complete core game and existing free conveniences, and can earn Cinders to buy Signets from other players. Every redeemed Signet grants the same Nobility benefits listed above, regardless of how the Signet was obtained.**
