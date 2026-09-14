# LegendsLegacy Frontend UI/UX Rework Analysis

Analysis date: 13 September 2026. Repository snapshot: `d0f1a8a55756d737ad849a7403b75f3326b894ec`, plus the existing working tree. Target: the player application in `LL/src/Presentation/ll`.

This is an analysis and designer handoff, not an implementation or a proposed aesthetic. The only deliverable created is this document. Existing application files, configuration, and unrelated BalanceHarness work were left unchanged.

For a quick read, start with the [executive summary](#1-executive-summary), [opportunity map](#19-redesign-opportunity-map) and [copyable Claude handoff](#claude-ui-redesign-handoff). The [45-part inventory](#6-current-screen-inventory) records current behavior; the remaining sections explain the evidence and design tradeoffs.

| Read for | Sections |
|---|---|
| Game and visual references | [Actual game](#2-what-legendslegacys-frontend-currently-is) · [Ten reference analyses](#3-reference-image-analysis) · [Shared principles](#4-shared-principles-across-references) · [Different families](#5-major-differences-between-reference-styles) |
| Systems and player goals | [Screen inventory](#6-current-screen-inventory) · [Journeys](#7-major-player-journeys) · [Navigation](#8-information-architecture-and-navigation) |
| Critical audit | [Visual system](#9-visual-system-audit) · [Components](#10-component-architecture-from-a-design-perspective) · [AI-UI signals](#11-systematic-audit-of-ai-generated-ui-signals) · [Screen identity](#12-screen-identity-what-makes-each-system-itself) |
| Practical constraints | [Density](#13-data-density-and-disclosure-priorities) · [Art](#14-existing-artwork-and-art-dependency) · [Technical realities](#15-technical-and-interaction-constraints) |
| Design handoff | [Preserve](#16-what-must-not-be-lost) · [Challenge](#17-assumptions-the-redesign-should-challenge) · [Reference translation](#18-reference--legendslegacy-translation-matrix) · [Opportunity map](#19-redesign-opportunity-map) · [Questions/evidence](#20-open-questions-evidence-index-and-verification) · [Claude handoff](#claude-ui-redesign-handoff) |

## Scope, evidence, and limitations

The investigation traced the Angular route tree, all feature-template areas, the application shell, important shared components, their styles, state/services/models, and selected backend contracts. A source scan found 122 external HTML templates, including 57 under `features`, and 538 non-test HTML/TypeScript/CSS/SCSS files under `src/app`. These are file counts, not screen counts. Inline-template components such as help, tours, and character decorations were also examined where relevant.

**All ten reference images supplied during the investigation are included in this analysis**, labeled R1–R10 in the user's attachment order. Four repository artwork files were also visually inspected for the asset audit. Reference screenshots and existing game assets are distinguished throughout. Reference imagery is inspiration, not evidence of LegendsLegacy mechanics or an instruction to copy another interface.

This is a **source-based UI audit**, not a live, authenticated usability study. No game session was started, no gameplay data was changed, and no screenshots of rendered screens were captured. Layout, hierarchy, responsiveness, and accessibility findings describe implemented structure and likely consequences. Exact visual balance, clipping, contrast over artwork, screen-reader output, mobile ergonomics, and actual player frequency still require runtime validation. Information priorities, friction assessments, and redesign classifications below are design judgments grounded in code, not telemetry.

Source links point to the current local checkout. The evidence index in section 20 also identifies the relevant files for repository search. A designer can use the game descriptions and final handoff without opening those links.

## 1. Executive Summary

LegendsLegacy is a persistent browser RPG built around repeated automatic combat, collecting and developing monster Essences, equipment and Combat Style choices, permanent upgrades, progression through regions and dungeons, competitive combat, and cooperative/social systems. Most meaningful player input happens **before combat, between encounters, while choosing a route, or while claiming and reinvesting rewards**. A redesign that assumes real-time action-game controls would misrepresent the game.

The criticism of an undirected, dashboard-like presentation is **partly supported, with important exceptions**:

- **Supported:** large portions of the application use the same small gold heading, muted explanatory copy, dark bordered surface, repeated status chip, and inset content group. Achievements, Soulstones, the Essence Codex, and portions of Prophecies and Colosseum make different progression systems look structurally similar. The same accent carries entity names, selection, currency, progress, and primary actions, weakening distinctions between them.
- **Supported:** character and creature identity are mainly names and numbers. The current character overview does not build a composition around character artwork. World areas share the same background image despite different names and enemies. The broad game background is largely covered by the shell and feature surfaces.
- **Supported:** the shell, repeated page headings, local tabs, contextual panes, chat, and notifications can compete for limited space. Some journeys cross equipment, Essence, Combat Style, and activity pages without a unified account of which build will actually be used.
- **Not supported as a universal diagnosis:** this is not uniformly a rounded, glassy, gradient-heavy SaaS template. Radii are generally modest. Much of the palette is restrained. Inventory, equipment comparison, trading order books, dungeon maps, Tower progression, and tournament rounds already have specialized structures. Grids and tables often serve real comparison needs.
- **Not supported:** there is no evidence that a single reusable Angular “card component” caused all this. The strongest uniformity comes from global CSS classes, shared heading/navigation conventions, and independently authored templates repeating that visual grammar. A visual redesign need not discard the state and interaction architecture.

The best foundation for future concepts is the game's actual decision structure: **build composition, ownership and binding, opponent/party relationships, dungeon path and Vigor, collective Tower progress, and earned versus unclaimed rewards**. Those relationships can give screens identity without requiring a painted scene for every feature. This document identifies those opportunities without selecting a final style.

The largest risks in a radical redesign are hiding necessary comparison data, confusing live equipment with captured builds, flattening distinct reward states into generic progress, losing context on small screens, and discarding existing accessibility and recovery behavior.

## 2. What LegendsLegacy's Frontend Currently Is

### 2.1 The actual game loop

1. Sign in, create an account, or enter as a guest. Load character state and resolve stored/offline actions.
2. Follow the guided First Steps or choose an unlocked regional combat area.
3. Repeated combat produces character progress, loot, and Essence opportunities. The player can review encounters and stop the ongoing action.
4. Manage equipment; absorb new Essences; assign Essence loadouts; develop a Combat Style; spend permanent-upgrade currencies.
5. Use the resulting build in dungeon runs, Arena attacks, tournaments, Tower expeditions, raids, and recurring regional boss encounters, subject to availability and progression gates.
6. Claim rewards, compare records, trade, contribute to a guild, and prepare the next activity.

Evidence: [character actions API][api-actions], [character models][model-character], [Essence models][model-essence], [dashboard routes][route-dashboard], and the screen sources indexed below.

### 2.2 Vocabulary a designer must distinguish

| Concept | What it means in this frontend | Design consequence |
|---|---|---|
| Character level / Combat XP | General character progression; affects content and Essence-slot availability | Do not merge it with Essence level, equipment rank, or style mastery |
| Combat Rating | Summary of permanent combat attributes and equipped build; may be unavailable | A useful summary, not a complete predictor of matchup or party effectiveness |
| Gear Power | Equipment comparison value displayed in inventory | A different measure from character Combat Rating and Arena Rating |
| Equipment | Eight equipped slots in the inventory interface, with compatible weapon/slot choices | Preserve compatibility and comparison, including two-slot alternatives where applicable |
| Equipment tier, rarity, quality, rank, variant | Separate item properties; reinforcement changes rank, blueprint conversion changes variant/set membership | These must not become interchangeable color badges |
| Unbound Essence item | An inventory object that can be absorbed or, subject to restrictions, shattered/traded/transferred | Owning a drop is different from owning its archived progression |
| Archived Essence | Persistent learned entity with active and passive abilities, level, XP, Ascension tier, favorite state, and possible attunement | A collection/build system, not simply a consumable bag |
| Attuned Essence / loadout | A selected Essence occupying a slot in a saved preset; activities can auto-use presets | Slot occupancy and active assignment must remain legible |
| Essence Ascension | Raises tier and level cap and changes abilities; costs/requirements are exposed by the server | Show current → next effects and requirements together; avoid hardcoding economics in a concept |
| Creature Focus | Targets a recorded creature to improve Essence farming, with a change cooldown | Connect creature, source location, drop opportunity, and cooldown |
| Essence Codex | Collections of Essences grant bonuses; discovery, absorption, collection completion, and collection Ascension differ | A single “collected” status is insufficient |
| Combat Style | One equipped mechanic, with mastery, a refinement, upgrade slots, and upgrade mastery | A separate build axis from equipment variants and Essence presets |
| Dungeon Vigor / Pending Loot | Expedition resource and unsecured rewards; failure loses Pending Loot, retreat secures it | Risk is a first-class decision, not background metadata |
| Captured build / snapshot | A build submitted or captured for a particular competitive/cooperative activity | Changing the ordinary loadout does not necessarily change the submitted build |
| First Clear / Echo | Distinct World Tower modes and reward eligibility | “Replay” and “repeat reward” cannot substitute for these meanings |

Backend confirmation: Essence level caps are 10, 30, 60, and 100 across tiers 0–3 in [Essence progression rules][rule-essence]. Combat Style validation enforces refinement availability, distinct upgrades, and mastered-upgrade eligibility in [Combat Style rules][rule-style]. Conduit uses an eligible Essence in the **first occupied Essence slot**; ordering is mechanically meaningful. Dungeon DTO mapping deliberately hides unrevealed rooms in [DungeonRunDto][dto-dungeon]; a designer must not assume all future room information can be shown.

### 2.3 Resources are system-specific

The persistent header exposes **Cinders** and **Soulstones**. Other resources become relevant in context: Essence Dust and Monster Cores, equipment Parts and blueprints, Sigil Fragments and dungeon Sigils, Arena Glory and tickets, Tower Tokens, raid Trophies, Guild Favor/XP/Supplies, Fate Echo and Prophetic Favor, and Nobility Signets. These names describe different economies or progress measures. They do not all warrant permanent placement in the shell. Evidence: [header][screen-header], [equipment management][component-upgrade], [dungeon preview][component-dungeon], [Prophecies][screen-prophecies], [Nobility][screen-nobility], and the respective activity screens.

### 2.4 Available code is not identical to deployed availability

The current environment code sets raids from `env.environment !== 'prod'`; focused Beta guidance defaults to enabled unless explicitly disabled. The Beta journey gradually exposes navigation based on tutorial progress and level. Constants identify social level 10, economy level 20, and full-game level 30, but individual visibility also depends on journey stage and objective destination. Route guards and sidebar filtering are separate mechanisms. Do not turn these thresholds into a universal release promise. See [environment feature flags][config-environment], [journey rules][journey], and [Beta guards][guard-beta].

There are player routes for Tower, regional bosses, guilds, trading, Prophecies, and tournaments even when a new character cannot reach them. Runtime feature flags, account eligibility, membership permissions, server data, and progress determine the actual experience. Development-only team-generation/spawn controls exist in some templates and are not ordinary player actions.

The repository instructions name the API location as `LL/src/API/LL`; the actual API files inspected are under **`LL/src/API/API.LL/Controllers/V1`**. Admin CRUD and LiveOps frontends are separate applications and are outside this player redesign.

## 3. Reference Image Analysis

These are visual references, several with concept-like content and controller prompts. Static images demonstrate composition, not tested interaction. Exact font families, hover states, responsive behavior, and the meanings of unexplained symbols cannot be reliably determined from them. Names, numbers, labels, weapons, currencies, parties and factions in the images are not LegendsLegacy requirements.

### R1 — White editorial character dossier: Ellenai Kesia

**Composition and hierarchy:** an oversized character cutout crosses a dark vertical strip and the surrounding pale canvas. The diagonal weapon breaks the column boundary and balances the long name on the left. The eye is drawn to the figure/name before supporting stats. Left-side combat information and right-side biography/loadout information have different density; neither is forced into an equal card grid. The dark resource strip stays shallow at the top, while contextual commands sit along the bottom.

**Typography and information:** the name mixes bold italic and much lighter lettering, turning identity into a graphic element. Small italic stat labels, aligned values and thin rules keep detail subordinate. HP/SP bars provide quick magnitude alongside exact values; equipment thumbnails sit directly beneath the stats they plausibly affect. Red identifies a title/warning/accent, while resource colors are localized. Most information rests directly on the canvas. Small beveled equipment tiles and elemental mini-cards provide object-specific framing.

**Why it feels like a game:** the selected entity is the subject of the entire screen; data and commands orbit that entity. **Useful translation:** character/build identity, typographic scale, compact persistent resources, grouped numeric information without enclosing every group. **Risks:** exceptionally expensive full-body art; large empty areas are practical only with a narrow set of stats; red mixes decorative and status roles; tiny technical labels and controller prompts cannot be copied as browser controls. LL has no verified equivalent to this screen's loyalty, wanted level, origin or SP systems. [R1 image][ref1]

### R2 — Tactical personnel management

**Composition and hierarchy:** a compact, tab-filtered personnel table sits on the left, a full-body character creates a strong central silhouette, and the selected person's portrait/name/vitals and assignment details fill the right. The selected table row is continuous and high-contrast; the small portrait in that row connects visually to the larger profile. Pale map contours and small crosshair marks suggest an organizational overview without enclosing every section.

**Typography and information:** “Personnel” is much larger than the roster labels, while the selected person's name is a second anchor. Job, level, rank and selected state have consistent aligned positions. Thin dividers group status, tasks, equipment and schedule. Red section bars and military icons establish a functional category; rank has a compact bright strip; schedule colors separate time allocations. There are pills here, but they encode schedule entries rather than surround every fact.

**Why it feels like a game:** selection has a visible subject, role and assignment, rather than just a record detail panel. **Useful translation:** guild rosters, Tower/raid party assignment and selection-to-detail continuity; dense tables can be expressive. **Risks:** art occupies space needed for LL party comparisons; schedules/workers/scientists are not LL systems; task icons need labels and keyboard/touch access. The bold central figure should not replace the evidence needed to evaluate several human players' builds. [R2 image][ref2]

### R3 — Dark illustrated party menu

**Composition and hierarchy:** three tall, slanted portrait frames occupy most of the screen; a small left menu and bottom prompts support them. Faces, names and oversized level diamonds lead; health/stamina and slim XP lines form a consistent comparison baseline below the art. A dark teal landscape and contour texture are continuous across the screen, so the interface reads as one composition.

**Typography and shape:** large white italic names combine with angular numerals, colored class labels and very thin ornamental frame corners. The party entries repeat, but repetition directly expresses comparable party members. Skewed frames create direction and energy without using a thick box around each numerical subsection. Red/blue bars remain readable against dark ground.

**Why it feels like a game:** the player's immediate subject is the party, and the selected menu item is visibly a mode of that party view. **Useful translation:** limited-size tournament/raid party overviews, clear member identity and consistent live-state baselines. **Risks:** LL is not a three-companion RPG; Tower/raid rosters can exceed three; huge portraits cannot replace required role/snapshot data. Save/Load menu entries and stamina must not be imported. Slant should affect framing rather than distort readable text or hit regions. [R3 image][ref3]

### R4 — Dark character, inventory and equipment composition

**Composition and hierarchy:** a large character and architectural illustration anchor the right; a compact inventory table occupies the lower left, while stats and equipment bands run down the center. Environmental texture links the open sections. The asymmetry is useful: the list remains dense, the character establishes identity, and equipment is visually adjacent to the character and stats.

**Typography and information:** thin separators and small labels differentiate information, status and item list without heavy cards. Selected inventory row, colored equipment-category edges, oversized level and exact resource figures have separate jobs. A green stat increment is visible near the affected attribute. The lower command row spans the screen rather than appearing as repeated per-card buttons.

**Why it feels like a game:** equipment and inventory are presented as parts of the same character context. **Useful translation:** relationships among LL's equipment selection, stat differences and current build. **Risks:** already very dense; text over terrain/illustration can lose clarity; a large figure leaves less room for LL's full comparison data. The image alone does not establish whether the shown increment is a preview or committed change—LL must explicitly distinguish these. The depicted item/food/magic categories do not establish LL features. [R4 image][ref4]

### R5 — Illustrated character-led play hub

**Composition and hierarchy:** a panoramic environment is the ground plane. A large character on the right, irregularly sized illustrated navigation tiles on the left, and a journey tile overlapping the character create several depth planes. A shallow top bar carries mode navigation and account resources; a narrow right edge holds secondary destinations. Tile size and shape differ by function rather than filling a symmetrical grid.

**Typography and information:** oversized character name and level establish personal identity. Navigation uses bold condensed/wide display lettering, brief labels, object symbols and bespoke imagery. The active Play mode, small notifications and progress are clearly separate scales. Bright cyan and warm landscape lighting create emphasis, but the art supplies much of the personality.

**Why it feels like a game:** navigation is composed around a place and a represented character, with progression embedded in the scene. **Useful translation:** reconsidering LL's entry/return-to-game hierarchy and contextual continuation. **Risks:** strong promotional competition—Community, Cosmetics, Rewards Pass and Store can overshadow play; these are not all LL systems. Requires multiple tile illustrations, environment art and a cutout. Do not transplant a storefront hub into a frequent-action PBBG solely for atmosphere. [R5 image][ref5]

### R6 — Illustrated collection/deck catalog

**Composition and hierarchy:** the left navigation is stable while a regular four-column grid carries collectible units. Card art dominates; names, large level numerals, letter grades, stars and a small currency-value strip occupy consistent positions. The cropped lower row signals continuation. A subdued blurred background keeps focus on objects.

**Typography, color and shape:** compact card names have strong display weight; big numerals allow rapid comparison. Distinct colored edge strips and grade shapes create repeated recognition cues; corners and ornament suggest collectible objects. This reference is unapologetically card-based. Its success comes from making each card represent a collectible, rather than turning every unrelated system into a card.

**Useful translation:** Essence collection inspection if each unit retains clear functional identity; compact collection-state encoding. **Risks:** art inventory cost grows with collection size; LL needs active/passive abilities and synergies, not only rarity/level. The currency strip may imply value or price, but the screenshot does not establish transaction behavior. Do not import purchasable characters, stars or letter grades. A catalog still needs dense filtering and accessible names. [R6 image][ref6]

### R7 — Dark selected-character detail with portrait navigation

**Composition and hierarchy:** the same left menu family as R6 remains, but the catalog becomes a large illustrated character plane and a right-hand detail composition. The cutout breaks the upper frame. A row of diamond portraits, with a visibly enlarged selected portrait and shoulder-button hints, lets selection persist while viewing detail. Level, name and HP/SP precede the compact stat grid and skill slots.

**Typography and shape:** serif/italic character name contrasts with oversized block numerals and simpler small labels. Delicate stat boxes are subordinate to the art. Gold/cream text and cyan/green action prompts sit over a dark, textured environment. The frame, diamonds and hexagonal skill slots are different shapes with different object roles.

**Useful translation:** continuity between browsing and inspecting an Essence or party member; prominent selected identity; adjacent-item navigation without returning to a catalog. **Risks:** LL has no character wardrobe workflow established here; question-mark skill slots are incomplete reference content, not a design feature to preserve. Large art, detached skill symbols and decorative glow can hide actual build reasoning. [R7 image][ref7]

### R8 — Light illustrated system menu with party anchors

**Composition and hierarchy:** broad, differently patterned destination bands on the left balance two large characters on the right. Compact dark stat placards overlap the figures, tying information to subjects rather than occupying a detached dashboard grid. A light global nav and currency bar remain at the top; a dark bottom strip supplies contextual help and prompts.

**Typography and shape:** the selected Factions band changes both color and visual weight. Big level numerals and mixed-weight names give the floating placards clear hierarchy. Notched bands and patterned fills create visual identity; pale negative space lets the figures dominate without a landscape scene.

**Useful translation:** strong destination selection, action-specific help and data visibly attached to its subject; simpler neutral backgrounds can carry personality. **Risks:** repeated destinations in top navigation and large left buttons consume space; several labels are explicitly placeholder text; LL has no confirmed faction system or pair of owned protagonist characters. This composition depends on two strong cutouts and is much less suited to dense routine comparison than R2. [R8 image][ref8]

### R9 — Light character roster and equipment detail

**Composition and hierarchy:** a plain text roster with a small selection marker occupies the left; a large cutout fills the center; name, level, vitals and stats align on the right. Biography sits beneath the roster. A wide equipment plaque overlaps the lower artwork and detail area, making the equipped object part of the character composition.

**Typography and information:** oversized level and mixed-weight name contrast with restrained list typography. The selected roster entry does not need its own card. Dark stat strips support scanning; the equipment object has one large dedicated graphic and short identifying text. Pale patterns and fine rules maintain continuity with R8 while changing the screen structure substantially.

**Useful translation:** typographic selection lists, clear selected-subject continuity, and equipment linked to identity. **Risks:** a long biography receives substantial space despite low operational value; the depicted single weapon is far simpler than LL's eight equipment slots and set/variant comparison. Off-screen fading names may look disabled. No character biographies or full-body image field have been established in LL contracts. [R9 image][ref9]

### R10 — Illustrated campaign operation map

**Composition and hierarchy:** the right and background are an illustrated relief map with a connected mission path. Selecting a node has a clear counterpart in a left briefing area: chapter/mission title, objective and enemy cards, then a large Start Operation action. The selected path/node and red action color connect location to commitment. The persistent top nav and bottom help strip are retained from R8/R9, but the page structure is entirely different.

**Typography, shape and information:** mission numbers are prominent geographic markers; enemy cards are compact previews; one headline band dominates the briefing. Fine contour detail and muted map colors allow route lines and the selected node to stand out. The selected node connects to a special illustrated encounter marker, suggesting contextual identity rather than a generic list position.

**Useful translation:** LL dungeon topology and region/activity selection, pairing an activity's prerequisites/enemies/rewards with its position in progression. **Risks:** geographic detail can imply travel, branching or adjacency rules the game does not have. LL's dungeon graph is server-authoritative and contains hidden information; region data is currently an authored activity catalog, not a geographic coordinate model. Map art is medium/high effort, and the briefing's small dense paragraph should not hide costs or risk. The Store tile and its notification are a competing prompt, not a principle to inherit. [R10 image][ref10]

## 4. Shared Principles Across References

1. **Each screen has a subject.** Character, party, collection or location establishes the first visual anchor. The page title is rarely the only identity. R1/R7/R9 center a selected person, R3 a party, R6 collectibles, R10 an operation location.
2. **Identity and detail occupy different scales.** Large names/levels and artwork coexist with tightly aligned small data. “Game-like” does not mean every label gets large. LL can adopt clearer scale contrast without concealing important numbers.
3. **Container use follows objects.** R2 uses a table; R6 uses literal collectible cards; R10 uses a map and briefing. Thin rules, baseline alignment and whitespace often replace a border around every fact. The shared principle is not “remove cards.”
4. **Selected state connects multiple areas.** R2's roster row relates to a profile; R7's portrait strip relates to the focal figure; R10's node relates to the briefing/action. LL already has selection state to support such continuity.
5. **Persistent controls are shallow or peripheral.** Most examples retain a top/left navigation zone and bottom contextual commands. They leave central space to the current subject rather than surrounding it with equally loud utilities.
6. **Art participates in the layout.** Figures overlap frames and weapons cross columns; maps establish relationships. This is more than placing a background behind opaque panels. It is also the largest asset dependency.
7. **Consistency does not require one screen template.** R8/R9/R10 share type, notches, patterns and navigation but use menu, roster/detail and campaign-map arrangements. R6/R7 share identity while switching collection to selected-object detail.
8. **Color and shape provide roles, with imperfections.** Resource bars, rank strips, selected tabs and action prompts have recurring roles. Some references also overload red or use excessive grade/star/value badges; these should be critiqued, not sanctified.

Shared weaknesses matter: nearly all references assume a wide screen and strong illustrations; many use small text over texture, unexplained symbols, repeated controller hints, and art-dominated layouts. None proves mobile usability or an adequate information hierarchy for LL's largest rosters. The transferable lesson is deliberate composition with appropriate density, not imitation of fonts, colors or ornamental geometry.

## 5. Major Differences Between Reference Styles

| Observed family | References | Structural character | What can translate with limited art | What depends on substantial art |
|---|---|---|---|---|
| Light editorial character dossier | R1 | Extreme type/art scale; open canvas; information flanks a figure | Type hierarchy, aligned stat groups, shallow resource strip | Signature full-body figure and overlapping weapon silhouette |
| Tactical personnel management | R2 | Dense roster → selected subject → role/assignment information | Selection continuity, list columns, role grouping, assignment visibility | Central cutout and portrait coverage for roster members |
| Dark landscape/party interface | R3–R4 | Environmental plane, angled portraits, dense stats integrated with art | Distinct party rows, thin dividers, grouped equipment/stat changes | Landscape plus coherent character illustrations |
| Illustrated fantasy hub and collection | R5–R7 | Scene-led hub, collectible-object catalog, selected-character scene | Catalog/detail continuity, contextual commands, recognizable collection states | Many character cards, cutouts, scenes and illustrated destination tiles |
| Light illustrated campaign suite | R8–R10 | Pale patterned ground, notched controls, broad menus, roster detail and relief-map operations | A coherent language across genuinely different compositions; selected nodes and briefings | Character cutouts, equipment illustration and illustrated geographical map |

These families overlap; R2 uses editorial typography, and R8/R9 inherit some of R1's name/number techniques. They differ in **what carries identity and where density lives**, not simply in light versus dark colors. R2 preserves many visible records, R3 gives three members most of the viewport, R6 repeats comparable objects, and R10 devotes space to geography. They cannot all be applied to LL's inventory or combat view without different tradeoffs.

The future concepts should preserve these differences in emphasis. A typography/structure concept can be realistic for a solo developer; an illustrated concept must budget art explicitly; a tactical concept must remain recognizably the same RPG across social and progression screens. These are comparison axes, not proposed finished directions.

## 6. Current Screen Inventory

**Priority key:** Critical = required for the immediate decision; Important = supports routine decisions; Contextual = useful in the current subtask; Rare = infrequent/history/diagnostics. Frequency is an analytical assessment, not measured usage.

**Freedom key:** **Must** preserve functional meaning and recognizable states; **Reorganize** may move/recombine content; **Radical** presentation/composition may change completely; **Legacy** is an existing assumption or element to challenge, not an instruction to delete working behavior. The current visual style is not a requirement for the future redesign.

### 6.1 Login, guest entry, and maintenance

**Entry:** `/login`; `/` redirects there. **Purpose / primary goal:** enter the game. **Secondary goals:** select a sign-in method, reach signup, understand an outage.

**Information:** Critical—credentials, validation, availability; Important—guest and Google alternatives; Contextual—maintenance message and expected return; Rare—brand decoration. **Primary actions:** sign in with credentials. **Secondary actions:** use Google/guest entry or navigate to signup.

**Current structure:** full-screen login artwork, central logo and divider, labeled form rows, decorative fixed-width login button, alternate entry controls. A maintenance state replaces the form. **Strengths:** low-friction guest entry and explicit failure/maintenance messaging. **Weaknesses:** password recovery is explicitly unavailable; different button languages compete; the visual center depends on fixed control sizes and needs narrow-screen verification. `data-1p-ignore` on inputs also warrants checking password-manager behavior.

**Freedom:** Must preserve entry/validation/maintenance; Reorganize sign-in choices; Radical composition and decoration; Legacy fixed ornamental button sizing. Evidence: [public routes][route-public], [login][screen-login].

### 6.2 Signup and guest account binding

**Entry:** `/signup`; the same signup component is embedded in Settings for conversion. **Purpose / primary goal:** create or secure an account. **Secondary goal:** preserve the existing character during conversion.

**Information:** Critical—character name, email, password/confirmation, validity, conversion context; Important—whether this is signup or account binding; Contextual—name help; Rare—decoration. **Primary actions:** submit signup or account binding. **Secondary actions:** cancel binding or return to login.

**Current structure:** logo/divider, stacked input rows, submit control; embedded form changes labels and context. **Strengths:** behavioral reuse fits both flows. **Weaknesses:** a full signup presentation inside a modal is not necessarily the right composition for securing an established character; recovery limitations make this journey especially consequential.

**Freedom:** Must preserve identity and validation; Reorganize fields and recovery explanation; Radical presentation; Legacy treating new-player signup and established-player binding as visually identical. Evidence: [signup][screen-signup], [Settings][screen-settings].

### 6.3 Persistent shell and First Steps onboarding

**Entry:** every `/game/**` route. `/game` opens Character → Overview. **Purpose / primary goal:** stay oriented and continue the current activity. **Secondary goals:** see currencies, pinned quest, ongoing dungeon/raid, social activity, and recovery states.

**Information:** Critical—current action, next objective, bootstrap/offline errors; Important—Cinders, Soulstones, character identity, destination; Contextual—quest chain and active runs; Rare—full navigation descriptions. **Primary actions:** follow the next objective, resume activity or recover bootstrap state. **Secondary actions:** navigate, inspect currencies, and expand/collapse chat or sidebar.

**Current structure:** sidebar left, header over a constrained game surface, docked or floating chat; mobile moves header above the navigation and puts chat at the bottom. A welcome modal directs First Hunt. **Strengths:** persistent resumption and quest guidance; device preferences; explicit offline catch-up. **Weaknesses:** shell width/height costs affect every feature; mobile title plus local page heading can repeat; quest, notification, and action emphasis compete.

**Freedom:** Must preserve orientation, recovery, and ongoing-action access; Reorganize persistent versus contextual data; Radical shell; Legacy assuming every feature needs the same enclosing surface and margins. Evidence: [shell][screen-shell], [header][screen-header], [sidebar][screen-sidebar], [quest tracker][screen-tracker], [journey][journey].

### 6.4 Character overview and other-player inspection

**Entry:** `/game/character/character-overview`; `?characterName=...` opens a searched profile. **Purpose / primary goal:** understand a build's current strength. **Secondary goals:** inspect other players, identify guild/title/presence, review attuned Essences.

**Information:** Critical—whose profile is shown, level, rating availability, combat attributes, loadout; Important—equipment-derived ratings and ability meaning; Contextual—guild, achievements, Nobility perks/presence; Rare—membership expiry during routine build review. **Primary actions:** inspect the displayed character's capabilities and search for another character. **Secondary actions:** refresh, inspect Essence details, and follow guild or journey context.

**Current structure:** guidance panel for the owner, profile/level/rating blocks, grouped attribute rows, Combat Style summary, Essence-loadout side column. No focal character portrait is used. **Strengths:** meaningful scale contrast already exists in level and Combat Rating; grouped attributes are useful; autocomplete and shareable query state. **Weaknesses:** visual identity is numerical; third-party lookup shares space with self-management; profile metadata competes with build interpretation.

**Freedom:** Must preserve self/other distinction and stat semantics; Reorganize metadata and build grouping; Radical identity composition; Legacy displaying every profile subsection as a bordered panel. Evidence: [overview][screen-overview], [overview styles][style-overview], [character model][model-character].

### 6.5 Inventory: equipment selection and comparison

**Entry:** `/game/character/inventory`, Equipment view. **Purpose / primary goal:** find and equip the right item. **Secondary goals:** compare, favorite, dismantle, donate, link in chat, manage upgrades.

**Information:** Critical—selected item/slot, compatibility, equipped comparison, ownership/binding, available action; Important—quality, rank, variant, Gear Power/delta, favorite/new state; Contextual—set bonuses and detailed modifiers; Rare—full item description during repeated sorting. **Primary actions:** select/compare gear and equip/unequip. **Secondary actions:** sort/filter/search, favorite, manage, bulk dismantle, donate or link to chat.

**Current structure:** equipped-slot rail, compact sortable catalog, item inspector, loadout area; container queries adapt to actual available width. Narrow views replace panes with the inspector and a back/close action. **Strengths:** efficient comparison, direct slot filtering, inline differences, protected favorites/equipped items in bulk dismantling. **Weaknesses:** several equipment measures require interpretation; fixed column/rail widths can squeeze names; primary-colored bulk dismantle sits near ordinary selection controls before its confirmation stage.

**Freedom:** Must preserve comparison and protection; Reorganize control priority; Radical item/equipment presentation; Legacy treating the current panel geometry as inseparable from comparison behavior. Evidence: [inventory][screen-inventory], [inventory styles][style-inventory].

### 6.6 Equipment management and saved equipment loadouts

**Entry:** Inventory → “Reinforce & manage”; equipment modals; loadout section in inventory. **Purpose / primary goal:** deliberately improve or switch equipment. **Secondary goals:** rename/delete presets, assign automatic activity use, copy a locked preset into a usable one.

**Information:** Critical—before/after stats, cost/held currency, binding/ownership restrictions, active preset and unsaved state; Important—rank and variant/set consequences; Contextual—blueprint sources and activity ownership; Rare—locked-preset recovery until needed. **Primary actions:** review and commit reinforcement/variant change, or apply an equipment preset. **Secondary actions:** rename/delete/copy presets, assign auto-use and retry a failed save.

**Current structure:** modal/embedded management with Reinforce and Change variant sections, paired stat comparisons, and a separate preset control block. **Strengths:** server quotes, explicit resource costs, no silent loss of unsaved changes when switching presets. **Weaknesses:** equipment auto-save and Essence/Style save conventions differ; upgrading and inspecting can span multiple surfaces.

**Freedom:** Must preserve quotes, destructive consequences, save failure and preset semantics; Reorganize editing flow; Radical presentation; Legacy duplicate detail framing around the same equipment object. Evidence: [upgrade panel][component-upgrade], [loadouts][component-loadouts], [equipment API][api-equipment].

### 6.7 Inventory: stock, selection containers, and direct transfer

**Entry:** Inventory → Stock; generic item modal; transfer controls on eligible items. **Purpose / primary goal:** use or move owned non-equipment stock. **Secondary goals:** inspect quantities, choose container rewards, favorite items.

**Information:** Critical—item, held quantity, chosen reward or selected recipient, amount consumed/transferred, restriction; Important—category and description; Contextual—reward ability preview; Rare—full metadata for familiar stock. **Primary actions:** select/use a container or complete a chosen transfer. **Secondary actions:** search/sort/filter stock, favorite and inspect reward details.

**Current structure:** category rail, two-column item/quantity list, inspector; transfer expands a recipient autocomplete and quantity form. **Strengths:** explicit recipient selection, refreshed stock and max quantity, container consequence text. **Weaknesses:** the same reward can be encountered in a modal and inspector; transfer is a consequential secondary operation that can become visually mixed with inspection.

**Freedom:** Must preserve recipient/quantity validation and irreversible consumption meaning; Reorganize contextual actions; Radical framing; Legacy needing a second full modal for already-inspected information. Evidence: [inventory][screen-inventory], [item modal][component-item-modal], [transfer][component-transfer].

### 6.8 Essences: absorb and shatter

**Entry:** `/game/character/essences?view=absorb`, or the Absorb tab. **Purpose / primary goal:** turn an unbound Essence drop into archived capability. **Secondary goal:** convert spare copies into Essence Dust.

**Information:** Critical—unabsorbed versus already archived, copy count, selected Essence ability pair, absorb/shatter consequence; Important—spare-copy count and dust yield; Contextual—bulk selection and recipient/trade options; Rare—extended effect descriptions once familiar. **Primary actions:** absorb a new Essence or confirm shattering surplus copies. **Secondary actions:** select/filter, adjust quantity, inspect abilities and use bulk selection.

**Current structure:** inventory-Essence catalog and selection/detail workspace, mobile selection mode, shatter confirmation modal. **Strengths:** separates acquisition from archived progression; previews ability value and shatter yield. **Weaknesses:** the player must understand both inventory ownership and archive ownership; “shatter” in the UI maps to dismantle in the service, so code names should not dictate user vocabulary.

**Freedom:** Must preserve acquisition/ownership distinction and confirmation; Reorganize reward-to-absorb flow; Radical composition; Legacy generic catalog framing where a first acquisition deserves a different hierarchy. Evidence: [absorb][screen-absorb], [Essence API][api-essence].

### 6.9 Essences: Soul Archive, progression, and attunement

**Entry:** `/game/character/essences`, optionally `/:essenceId`. **Purpose / primary goal:** assemble a useful Essence build. **Secondary goals:** level/Ascend, favorite, manage named presets and activity auto-use.

**Information:** Critical—selected Essence, active/passive mechanics, occupied slots, selected preset, slot ordering for Conduit; Important—level/cap/XP/tier, Ascension requirements and current → next grants, dust budget; Contextual—threat, tags, favorite/ready filters and other presets; Rare—long effect explanations once a build is settled. **Primary actions:** select/attune/remove/channel an Essence and develop it through Ascension or dust. **Secondary actions:** name/delete/copy presets, assign activity auto-use and filter/sort the Archive.

**Current structure:** virtualized archive rows, central ability/progression detail, loadout pane; mobile has back-to-list, bottom action controls and an expandable loadout. Default archive sorting is threat descending. **Strengths:** coordinated list/detail/loadout workspace, resource requirements near actions, virtual scrolling, direct Essence links. **Weaknesses:** three dense panes still require cross-reading; “Soul Archive,” “Essences,” and “Loadout” overlap as identity labels; default threat ordering prioritizes a specialist metric; saving names and automatic slot/activity saves need clear distinctions.

**Freedom:** Must preserve mechanics, slot identity, ownership and saving; Reorganize progression/build hierarchy; Radical composition; Legacy nested active/passive/progression boxes as the only way to express relationships. Evolution exists in the API/state model but no current evolution action was found in this page template; do not invent an active evolution journey. Evidence: [Essences][screen-essences], [Essence state][state-essence], [models][model-essence].

### 6.10 Creature Archive and Creature Focus

**Entry:** Essences → Creatures (`?view=creatures` supported). **Purpose / primary goal:** choose what creature to farm for an Essence. **Secondary goals:** review defeated creatures and source locations.

**Information:** Critical—creature, desired Essence variant, known location, focus eligibility/cooldown; Important—absorbed state and kill count; Contextual—tags; Rare—first/last defeat dates and accumulated focus duration. **Primary actions:** set Creature Focus. **Secondary actions:** filter sources/locations/collection state and inspect variants.

**Current structure:** five filters, focus explanation block, repeated creature cards with status chips, chronology, locations, variant sub-boxes, tags and full-width focus button. **Strengths:** useful multidimensional filtering; exact focus-effect explanation. **Weaknesses:** historical timestamps occupy routine card space while creature-to-location-to-desired-Essence relationships are fragmented; truncating locations to three can conceal context.

**Freedom:** Must preserve focus effects/cooldown and source truth; Reorganize chronology; Radical bestiary/farming presentation; Legacy mandatory miniature biography/dashboard for every creature. Evidence: [Essences template][screen-essences], [Essence models][model-essence].

### 6.11 Essence Codex

**Entry:** Essences → Codex (`?view=codex` supported). **Purpose / primary goal:** identify collection completion opportunities and bonuses. **Secondary goal:** understand collection Ascension.

**Information:** Critical—required/missing members, completion state, reward bonus; Important—total bonuses and member Ascension tiers; Contextual—category and description; Rare—full descriptions of completed collections. **Primary actions:** inspect missing members, requirements and collection bonuses. **Secondary actions:** inspect member mechanics; acquiring or absorbing them happens elsewhere.

**Current structure:** bonus-summary grid, two-column collection cards, inner bonus box, bordered member rows with status badges, progress bar. **Strengths:** distinguishes unknown, missing, absorbed and unlocked states; aggregate bonuses help explain cumulative value. **Weaknesses:** one collection becomes a panel containing a card containing groups and rows; collection-to-source journey is not directly expressed by the displayed member rows.

**Freedom:** Must preserve missingness, bonuses and Ascension meaning; Reorganize collection detail; Radical collection composition; Legacy repeating all completed members at equal visual weight. Evidence: [Essences template][screen-essences], [Codex contracts][model-essence].

### 6.12 Combat Styles

**Entry:** `/game/character/combat-styles`. **Purpose / primary goal:** select and configure a combat mechanic. **Secondary goals:** preview alternatives and understand mastery milestones.

**Information:** Critical—previewed versus equipped style, refinement/upgrades, validity and unsaved state; Important—mechanic examples and mastery; Contextual—opening technique and future unlocks; Rare—full mastery history. **Primary actions:** preview/configure and save/equip a Style. **Secondary actions:** discard changes and inspect mastery/refinement/upgrade explanations.

**Current structure:** style navigation, mastery/slot summary, explicit battle-state banner, core mechanic panel, milestone panel, refinement and upgrade selection; native dialogs provide mobile editors. **Strengths:** clear preview-versus-equipped copy, actionable mechanic examples, meaningful progress milestones. **Weaknesses:** detailed conditional choices can overwhelm; Conduit's chosen Essence lives in another system; label overlap with equipment “style/variant” needs care.

**Freedom:** Must preserve preview/equip, validation and milestones; Reorganize explanatory depth; Radical mechanic expression; Legacy assuming all styles need identical boxes despite different mechanics. Evidence: [Combat Styles][screen-styles], [style rules][rule-style].

### 6.13 Achievements and titles

**Entry:** `/game/character/achievements`; collection switches between Achievements and Titles. **Purpose / primary goal:** inspect accomplishments and choose displayed identity. **Secondary goals:** pursue unfinished chains and compare Renown.

**Information:** Critical—achievement requirement/progress or title eligibility/current title; Important—points, Renown and chain; Contextual—rarity/category/scope; Rare—completed descriptions. **Primary actions:** inspect achievement progress and equip/unequip a title. **Secondary actions:** filter/search/sort, change collection and choose title position.

**Current structure:** four equal summary stat cards, segmented collection/category controls, card list, title side panel/collection. **Strengths:** searchable, filters separate status, title preview and positioning are explicit. **Weaknesses:** the same header/stat-grid/card formula as Soulstones, despite different motives; dense metadata badges compete with accomplishment and next target; four summary columns persist on small layouts.

**Freedom:** Must preserve unlock and identity states; Reorganize totals and metadata; Radical accomplishment presentation; Legacy equal emphasis on every statistic and completed record. Evidence: [Achievements][screen-achievements].

### 6.14 Soulstone permanent upgrades

**Entry:** `/game/character/soulstone-archive`, labeled Soulstones. **Purpose / primary goal:** spend Soulstones on permanent branch upgrades. **Secondary goal:** assess/reset allocation.

**Information:** Critical—held currency, current/next effect, rank cap, price and disabled reason; Important—branch/applicability and refund; Contextual—summary totals; Rare—explanation of already-maxed upgrades. **Primary actions:** purchase an upgrade rank. **Secondary actions:** switch branch views and review/confirm a reset.

**Current structure:** default header, four summary stat cards, tabs and repeated upgrade groups, each with rank/current/next/cost. **Strengths:** concrete next effect and reset refund, bounded purchases. **Weaknesses:** “branches” and “constellations” read as a list of products; similarities to achievement counters obscure allocation identity.

**Freedom:** Must preserve cost/effect/refund; Reorganize branches and summaries; Radical upgrade relationships; Legacy equal rectangular treatment of choices whose dependencies/applicability differ. Evidence: [Soulstones][screen-soulstones], [upgrade card][component-soulstone].

### 6.15 Regions and combat-area selection

**Entry:** `/game/world` resolves a region; `/game/world/shenic` and `/game/world/meran`; optional `?area=...` targets an area. **Purpose / primary goal:** select the next accessible activity. **Secondary goals:** inspect drops, Essence collection progress, dungeons, raids and regional boss access.

**Information:** Critical—region/area, level/access requirement, current battle and start action; Important—possible drops and activity availability; Contextual—collection count and Tower gate; Rare—already-cleared area status when farming elsewhere. **Primary actions:** choose an accessible area and start/resume combat. **Secondary actions:** change region, inspect drops and select dungeon/raid/boss previews.

**Current structure:** region heading/tabs, repeated 15rem area cards, activity side rail; an inline preview replaces the main area section. All area cards use the same woodland illustration. **Strengths:** activities grouped by region, active-battle marker and resumption, collection counts. **Weaknesses:** “World Map” is an activity grid rather than a geographic map; place identity is mostly text; choosing a dungeon/raid changes local state rather than a dedicated URL, so refresh can lose the preview selection.

**Freedom:** Must preserve gates/sources/current action; Reorganize regional content; Radical place composition; Legacy identical artwork and grid footprint for every area. Evidence: [region][screen-region], [region styles][style-region], [region data][service-region], [area card][component-area].

### 6.16 Dungeon preview, Sigil assembly, mastery and records

**Entry:** select a dungeon family in the region rail. **Purpose / primary goal:** decide whether and at what difficulty to enter. **Secondary goals:** assemble entry Sigils, inspect rewards/mastery and records.

**Information:** Critical—difficulty, unlock/entry requirement, held Sigils/fragments, start/continue state; Important—rooms, reward quantities/chances, first-clear reward, mastery; Contextual—lore and mastery benefits; Rare—first-clear/total-clear records. **Primary actions:** choose difficulty, satisfy/assemble entry requirements and enter/continue. **Secondary actions:** inspect rewards/mastery and open records.

**Current structure:** dungeon identity/lore and reward tables beside a run-setup panel; mastery detail and a records replacement view. **Strengths:** guaranteed versus chance versus first-clear rewards are distinguished; requirements and assembly are adjacent to entry; mastery is shared across Novice/Veteran/Champion. **Weaknesses:** preview contains several layers of bordered reward/setup groups; records replace setup context; translucent blur on setup adds another surface treatment.

**Freedom:** Must preserve cost, reward and difficulty meaning; Reorganize lore/history; Radical dungeon identity; Legacy nesting a cost card inside setup inside a broader preview solely for framing. Evidence: [dungeon preview][component-dungeon], [records][screen-dungeons].

### 6.17 Active dungeon and resolution

**Entry:** `/game/world/dungeon`, including persistent current-dungeon shortcuts. **Purpose / primary goal:** progress through an expedition while managing risk. **Secondary goals:** inspect effects, loot and failure causes.

**Information:** Critical—current node, available routes, Vigor and cost/forecast, encounter state, unsecured loot and retreat; Important—depth/section and room type; Contextual—threshold effects and item list; Rare—complete threshold reference. **Primary actions:** choose a revealed route/room action, manage Vigor, retreat or resolve rewards. **Secondary actions:** skip playback, inspect details, return to world and retry missing graph data.

**Current structure:** connected node graph, decision region, expedition/loot sidebar, distinct failure and claimed-reward states; mobile switches map orientation. **Strengths:** this is already a game-specific decision layout; pending versus secured/lost rewards is explained; failure provides advice; unavailable graph is not fabricated. **Weaknesses:** map, sidebar and combat can still separate the immediate consequence from the selected path; actual touch target, long-path scrolling and end-state transitions need runtime testing.

**Freedom:** Must preserve topology, hidden information, Vigor, risk and server-authorized actions; Reorganize reference details; Radical visual treatment of the graph; Legacy the surrounding dashboard surface, not the branching mechanic. Evidence: [dungeon run][screen-dungeon-run], [DungeonRunDto][dto-dungeon], [dungeon API][api-dungeon].

### 6.18 Combat viewer, idle loop and combat analysis

**Entry:** `/game/combat`; embedded in training, dungeons, Arena, tournaments, Tower, raids and regional bosses. **Purpose / primary goal:** understand encounter progress/outcome and why a build works or fails. **Secondary goal:** inspect unit/ability performance.

**Information:** Critical—battle context, teams, life/barrier, result, stop/skip/return meaning; Important—damage/healing/threat, party/summon grouping, stagger; Contextual—selected unit's ability statistics and damage types; Rare—full secondary totals. **Primary actions:** follow the encounter and stop idle combat or skip/leave playback as appropriate. **Secondary actions:** select units, expand parties/summons, sort abilities and inspect history/help.

**Current structure:** battle header and a dense combat-stat viewer, with desktop rows and mobile cards; loading/ongoing/empty idle states. This is primarily analytical playback, not a character-art battle stage with manual ability buttons. **Strengths:** diagnostics include healing, barrier, threat and stagger, not only damage; groups reduce roster clutter; context can be injected by each activity. **Weaknesses:** combat can resemble a report before it conveys the drama and decision-relevant result; identical viewer hierarchy gives very different battles similar visual identity; wide stat columns require careful space budgeting.

**Freedom:** Must preserve trustworthy playback/results and stop-versus-skip semantics; Reorganize live versus post-combat detail; Radical encounter presentation; Legacy making the most detailed report the only battle composition. Evidence: [combat][component-combat], [combat statistics][component-combat-stats].

### 6.19 World Tower overview, scouting, preparation and shop

**Entry:** `/game/world/tower`, sidebar World Tower, heading “Legacy's Ascension.” **Purpose / primary goal:** understand the realm's ascent and prepare/join the next floor. **Secondary goals:** contribute scouting/preparation, review unlocks and history.

**Information:** Critical—selected floor/Guardian, floor state, recruiting/current expedition, requirements; Important—realm clears, roster size, recommended rating, known abilities and scouting; Contextual—preparation caps/bonuses, First Clear/Echo rewards; Rare—release summary and shop before stock exists. **Primary actions:** select a floor and create/open an expedition or contribute to preparation. **Secondary actions:** inspect scouting and open records, personal expeditions or shop.

**Current structure:** ascent floor rail, named Guardian focal section, expedition/reward panels and readiness sidebar; mobile has floor selection/detail and readiness tabs. **Strengths:** progression and collective preparation have recognizable identity; reveals are earned; floor selection is specific to the system. **Weaknesses:** competing status/reward/readiness groups dilute the immediate expedition action; three names describe the system; shop opens an empty-stock message, not a purchasing workflow.

**Freedom:** Must preserve global progression, reveals, contribution limits and modes; Reorganize readiness; Radical Guardian/floor composition; Legacy empty shop prominence and repeated summary strips. Evidence: [Tower overview][screen-tower], [Tower styles][style-tower].

### 6.20 Tower expedition: applications, parties and battle report

**Entry:** `/game/world/tower/expeditions/:rallyId`; old `/rallies/:rallyId` redirects. **Purpose / primary goal:** assemble a viable locked-build expedition and attempt the Guardian. **Secondary goals:** evaluate readiness, manage membership/leadership, diagnose defeat.

**Information:** Critical—submitted build/time, application/roster state, permission and readiness/start state; Important—party/slot assignment, rating, recommended strength; Contextual—warnings and participant report; Rare—creation timestamp after launch. **Primary actions:** apply/join/update the submitted build; leaders prepare assignments and start. **Secondary actions:** withdraw/leave, balance/bench, handle requests, transfer leadership and inspect reports.

**Current structure:** Guardian heading/status, action row, bench, party slot panels, applications and post-battle report. **Strengths:** explicitly locked builds, click-select alternatives to dragging, auto-balance, role-based actions. **Weaknesses:** a long command row precedes the roster; rating dominates participant presentation despite ability synergy; submitted versus live build is cross-screen context.

**Freedom:** Must preserve applications, permissions, slots and snapshot semantics; Reorganize leader tools/report; Radical roster composition; Legacy using average rating as the main visual shorthand for readiness. Evidence: [Tower expedition][screen-expedition], [Tower API][api-tower].

### 6.21 Tower personal expeditions

**Entry:** `/game/world/tower/personal-expeditions`. **Purpose / primary goal:** resume inspection of a prior attempt. **Secondary goal:** compare results.

**Information:** Critical—attempt identity, floor/Guardian, result and View link; Important—mode, roster, duration/date; Contextual—attempt number; Rare—older entries. **Primary actions:** open an expedition record. **Secondary actions:** inspect recorded facts and return to the Tower.

**Current structure:** table of the latest 100 attempts. **Strengths:** compact chronology and useful columns. **Weaknesses:** long roster summaries compete with result; mobile table ergonomics need verification. **Freedom:** Must preserve identity/history; Reorganize columns; Radical framing if useful; no need to replace tabular comparison merely to look less web-like. Evidence: [personal expeditions][screen-personal-tower].

### 6.22 Tower Hall of Fame

**Entry:** `/game/world/tower/hall-of-fame`. **Purpose / primary goal:** see first realm clears and replay them. **Secondary goal:** inspect pioneering rosters.

**Information:** Critical—floor, Guardian, first-clear roster and replay; Important—attempt, duration, cleared date; Contextual—historical comparison; Rare—old record detail. **Primary actions:** inspect/replay a recorded first clear. **Secondary actions:** compare supporting historical facts and return to ascent.

**Current structure:** permanent-history table with embedded combat replay. **Strengths:** clear historic scope and compact records. **Weaknesses:** momentous first clears receive similar treatment to routine personal rows. **Freedom:** Must preserve provenance and replay; Reorganize supporting columns; Radical commemorative hierarchy; Legacy making historical significance depend only on a table heading. Evidence: [Hall of Fame][screen-hall].

### 6.23 Raid preview, recruiting directory and Trophy Exchange

**Entry:** regional raid selection, subject to raid feature flag and access. **Purpose / primary goal:** understand the raid and find/create a muster. **Secondary goals:** choose difficulty, spend Trophies, find unclaimed history.

**Information:** Critical—three party roles, requirements, difficulty, current raid and recruiting status; Important—party size, sign-up window, reward rules; Contextual—Trophy balance, vendor unlock/weekly limit; Rare—older raid history. **Primary actions:** choose difficulty and create/request/return to a raid muster. **Secondary actions:** inspect role/reward explanations, vendor stock and historical results.

**Current structure:** three role-explanation encounter cards, setup/recruiting rail, vendor rows and history. Rearguard handles continuous waves, Vanguard breaks defenses, Main Guard endures; all regroup restored for the Final Assault. **Strengths:** meaningful role differentiation and explicit one-active-raid restriction. **Weaknesses:** explanations, shopping and recruitment share one surface; a newcomer must translate role names into build requirements across other pages.

**Freedom:** Must preserve phase relationships and eligibility; Reorganize shopping/history; Radical encounter/party composition; Legacy equal emphasis on recruitment and vendor content. Evidence: [raid preview][screen-raids].

### 6.24 Raid muster, party builder and battle-plan preview

**Entry:** `/game/world/raid/:raidId`. **Purpose / primary goal:** prepare a three-party roster and commence. **Secondary goals:** handle requests, update submitted loadouts and preview likely performance.

**Information:** Critical—sign-up deadline, minimum/maximum roster, party assignment, permissions and captured build; Important—party roles/readiness and unassigned members; Contextual—simulation preview probabilities/confidence and derived boss modifiers; Rare—older administrative timestamps. **Primary actions:** prepare the roster/party assignments, update captured builds and commence when permitted. **Secondary actions:** handle requests/withdrawal/leadership, preview the plan and cancel when allowed.

**Current structure:** boss/status header, metrics/action row, request/bench/party sections, optional simulation report. **Strengths:** click or drag assignment, explicit pending requests, server-provided ability flags; preview states that it does not mutate the real raid. **Weaknesses:** command density and average-rating shorthand; a probabilistic plan can look authoritative unless prediction versus actual result remains clear.

**Freedom:** Must preserve roster and snapshot rules, preview uncertainty and action permissions; Reorganize preparation controls; Radical tactical presentation; Legacy giving every administrative control equal prominence. Evidence: [raid page][screen-raid], [party builder][component-raid-party], [raid API][api-raid].

### 6.25 Raid playback, outcomes and reward claims

**Entry:** the active or historical raid page. **Purpose / primary goal:** understand the coordinated encounter and its outcome. **Secondary goals:** inspect individual contributions and collect eligible rewards.

**Information:** Critical—phase, party outcomes, boss condition and claim availability; Important—remaining allied forces, reinforcement/Guardian modifiers, contribution and reward eligibility; Contextual—individual ability totals; Rare—old replay events. **Primary actions:** follow a party or combined encounter, claim. **Secondary actions:** replay, inspect contribution, leave the viewer.

**Current structure:** concurrent party summaries, focused shared combat viewer, then outcome/contribution/reward sections. **Strengths:** preserves the distinction between preliminary parties and the Final Assault, partial progress and victory, and preview versus real results. **Weaknesses:** several independent combat summaries can obscure the overall raid story; reward explanations are dense. Leaving playback must not appear to cancel a running raid.

**Freedom:** Must preserve phase/state distinctions and claims; Reorganize analysis and history; Radical coordinated battle presentation; Legacy relying on repeated metric panels alone to convey a large cooperative event. Evidence: [raid page][screen-raid], [playback][screen-raid-playback].

### 6.26 Regional boss event

**Entry:** `/game/world/region-boss`, also a regional event entry. **Purpose / primary goal:** participate in or follow the regional boss. **Secondary goals:** manage sign-up, review recent results and claim milestones.

**Information:** Critical—event status, sign-up window, boss health/Fury, party survival and claim eligibility; Important—schedule, revival countdown, player party; Contextual—recent event results; Rare—individual historic combat details. **Primary actions:** sign up/withdraw when allowed, view battle, claim. **Secondary actions:** inspect milestones/history. Development spawn controls are conditional, not an ordinary player workflow.

**Current structure:** scheduled/live event content, combat playback and results/milestones; quiet states expose upcoming/recent activity. Eligibility includes automatic entry based on recent activity, with manual sign-up during the window. **Strengths:** meaningful live countdowns and recoverable history. **Weaknesses:** a scheduled social event can resemble another battle-statistics page; automatic participation needs a clear explanation.

**Freedom:** Must preserve event lifecycle and eligibility; Reorganize history/claims; Radical event identity; Legacy treating the quiet and live phases as equally dense reports. Evidence: [regional boss][screen-region-boss], [event service][service-region-boss].

### 6.27 Colosseum and Arena opponent selection

**Entry:** `/game/city/colosseum`; Arena tab. **Purpose / primary goal:** choose and challenge an opponent. **Secondary goals:** update defense, inspect rating/tickets and reach other PvP systems.

**Information:** Critical—opponent, challenge eligibility, tickets/cooldown and expected rating changes; Important—tier, Glory, defense snapshot validity and freshness; Contextual—streak, daily wins/record; Rare—past matches. **Primary actions:** challenge, update defense. **Secondary actions:** refresh opponents, navigate Colosseum tabs.

**Current structure:** header, five local destinations, seven status tiles and opponent cards; status tiles become a horizontal strip on small screens. **Strengths:** visible consequences of win/draw/loss and explicit defense updates. **Weaknesses:** ticket/rating decisions compete with numerous peer metrics; opponent refresh uses the danger button variant despite being different from destructive operations.

**Freedom:** Must preserve opponent comparison, costs and snapshot distinction; Reorganize status/navigation; Radical competitive composition; Legacy equal-weight metric tiles. Evidence: [Colosseum][screen-colosseum], [Arena][screen-arena].

### 6.28 Tournament Grounds and tournament replay

**Entry:** Colosseum Tournament Grounds tab; dedicated replay at `/game/city/colosseum/tournaments/:tournamentId/matches/:matchId/replay`. **Purpose / primary goal:** register/manage a three-player team and follow weekly elimination competition. **Secondary goals:** inspect other teams, results, standings and rewards.

**Information:** Critical—registration state/deadline, team membership, build capture timing, next match and outcome; Important—round/bracket progression, invitations/applications, reward eligibility; Contextual—season points/champions; Rare—older match statistics. **Primary actions:** form/join/manage a team, follow/replay a match, claim. **Secondary actions:** browse rounds, teams and history.

**Current structure:** state-dependent registration/team panels and round/match navigation with expandable team detail; separate shared-combat replay, including overtime. **Strengths:** recognizably tournament-specific progression and explicit deadlines. **Weaknesses:** management, standings, history and rewards make the area long and conceptually broad; team setup and captured competitive builds are separated from build editing.

**Freedom:** Must preserve bracket and registration semantics; Reorganize management/history; Radical competition identity; Legacy stacking every tournament concern with equal prominence. Evidence: [Tournament Grounds][screen-tournament], [tournament replay][screen-tournament-replay].

### 6.29 Champion Market

**Entry:** Colosseum Champion Market tab. **Purpose / primary goal:** spend Glory. **Secondary goals:** inspect unlocks and stock resets.

**Information:** Critical—item, quantity, price, available Glory and purchase eligibility; Important—remaining stock/limit and balance after purchase; Contextual—category/unlock condition/reset clock; Rare—extended item mechanics. **Primary action:** purchase selected quantity. **Secondary actions:** filter/select an item and inspect it.

**Current structure:** catalog rows with a selected-item purchase aside. **Strengths:** separates browsing from transaction review and makes affordability/limits explicit. **Weaknesses:** generic shop framing underplays the relationship between combat achievement and rewards.

**Freedom:** Must preserve transaction checks and preview; Reorganize supporting copy; Radical only if comparison remains efficient; Legacy redundant wrapper panels. Its similarity to the Guild shop is largely beneficial behavioral reuse, not evidence that every similar layout is wrong. Evidence: [Champion Market][screen-champion-market].

### 6.30 Arena rankings and Record of Battle

**Entry:** Colosseum Rankings and Record of Battle tabs. **Purpose / primary goal:** compare standing or explain recent PvP results. **Secondary goals:** inspect opponents and match breakdowns.

**Information:** Critical—rank/rating for rankings; opponent/outcome/rating change for records; Important—Glory, date, participant identity; Contextual—combat summaries; Rare—individual ability events. **Primary actions:** browse standings or open a result. **Secondary actions:** filter, inspect a player, return from summary.

**Current structure:** reusable leaderboard table/podium for Arena; separate battle-record cards with a detailed summary. Older records can lack detailed data. **Strengths:** compact competitive facts and drill-down. **Weaknesses:** Arena ranking overlaps the broader Leaderboard destination; reports inherit the same analytical character as PvE, with limited competitive ceremony.

**Freedom:** Must preserve ordering, provenance and unavailable-record states; Reorganize navigation/secondary metrics; Radical result emphasis; Legacy duplicate entry concepts without explanation. Evidence: [Arena rankings][screen-arena-rankings], [records][screen-arena-records].

### 6.31 Guild discovery and public guild profile

**Entry:** `/game/city/guild` without membership; `/game/city/guild/:guildId` for a public profile. **Purpose / primary goal:** find/join or inspect a guild. **Secondary goals:** create a guild and handle invitations.

**Information:** Critical—guild identity, capacity, application/invitation state and join/create eligibility; Important—description, members, progression; Contextual—public buildings/rankings; Rare—individual member detail. **Primary actions:** apply, accept/reject invitation, create. **Secondary actions:** browse/open guild profiles.

**Current structure:** discovery/list and invitation areas, creation modal; public profile with members/buildings/rankings. **Strengths:** distinguishes non-member, public and member views. **Weaknesses:** identity is primarily name/tag and description, while numerical capacity/progression can dominate first impressions.

**Freedom:** Must preserve membership and application rules; Reorganize discovery/detail; Radical social identity; Legacy assuming an organization is adequately represented by generic summary boxes. Evidence: [no-guild view][screen-no-guild], [public guild][screen-public-guild].

### 6.32 Guild headquarters, members and permissions

**Entry:** member view at `/game/city/guild`, Guild tab. **Purpose / primary goal:** understand and coordinate the guild. **Secondary goals:** invite, manage applications/roles, edit identity or leave.

**Information:** Critical—identity, membership, role permissions and actionable requests; Important—Favor/capacity, member roles/presence; Contextual—description and permission details; Rare—leadership transfer/disband. **Primary actions:** member coordination and permitted administration. **Secondary actions:** edit identity, configure roles, leave or disband with confirmation.

**Current structure:** outer page header plus guild heading/resource strip, six local tabs, member grid and administrative expansions/modals. **Strengths:** permission-dependent controls, presence and explicit destructive confirmations. **Weaknesses:** repeated headers consume space; everyday member activity and rare administration compete; social identity has no strong visual anchor beyond text.

**Freedom:** Must preserve authority and confirmation boundaries; Reorganize administration; Radical headquarters composition; Legacy placing management detail on the same plane as ordinary participation. Evidence: [member shell][screen-in-guild], [guild information][screen-guild-info].

### 6.33 Guild Vault

**Entry:** guild Vault tab; donation also from eligible inventory items. **Purpose / primary goal:** borrow/return shared equipment or donate an eligible item. **Secondary goals:** inspect availability and ownership.

**Information:** Critical—item stats, availability/borrower and permanent donation/borrowing rules; Important—donor, quality, rank, filters and comparison; Contextual—vault counts; Rare—legacy withdrawal exceptions. **Primary actions:** borrow, return, donate. **Secondary actions:** filter/sort, inspect and confirm.

**Current structure:** status summaries, filterable item list and item/ownership detail, plus donation selection. **Strengths:** protects guild property and documents consequential rules. Donated progression gear is not a reclaimable personal deposit; borrowed gear cannot be improved, restyled, sold or transferred. **Weaknesses:** familiar inventory visuals can disguise very different ownership consequences; repeated count tiles add less value than availability and conditions.

**Freedom:** Must preserve ownership and confirmation semantics; Reorganize summaries; Radical shared-armory identity if rules remain explicit; Legacy treating donation like a reversible inventory move. Evidence: [Vault][screen-guild-vault], [equipment API][api-equipment].

### 6.34 Guild buildings and upgrade targets

**Entry:** guild Buildings tab. **Purpose / primary goal:** understand collective development and fund an eligible upgrade. **Secondary goals:** inspect future levels, set a target and review activity.

**Information:** Critical—selected building/current level, next benefit, supplies/requirements and permission; Important—guild target and ready/locked state; Contextual—future level path; Rare—older activity. **Primary actions:** upgrade or set target when permitted. **Secondary actions:** select building/level and inspect history.

**Current structure:** grouped building list, selected definition/upgrade path and activity column. **Strengths:** purpose-built master/detail layout and explicit current-to-next benefits. **Weaknesses:** buildings are mostly text and numerical effects, so collective place/ownership is weakly expressed.

**Freedom:** Must preserve costs/benefit path and permission; Reorganize activity; Radical place-based interpretation only if supported by resources; Legacy redundant frames rather than the useful list/detail structure. Evidence: [buildings][screen-guild-buildings].

### 6.35 Guild missions

**Entry:** guild Missions tab. **Purpose / primary goal:** contribute to the current collective objective and claim earned rewards. **Secondary goals:** select an objective if authorized, compare contributions and inspect reward tiers.

**Information:** Critical—selected mission, time window, shared progress, personal eligibility and claim state; Important—personal contribution and next tier; Contextual—top contributors, weekly summaries, reward matrix; Rare—older objective detail. **Primary actions:** pursue the objective, select when authorized, claim. **Secondary actions:** inspect tiers and contributors.

**Current structure:** weekly objective/progress plus tier rewards, personal/daily claims and contribution summaries. **Strengths:** distinguishes collective progress from individual entitlement. **Weaknesses:** several progress and reward blocks make it hard to identify the next useful action; personal and guild currencies require careful labels.

**Freedom:** Must preserve shared-versus-personal eligibility; Reorganize tiers/history; Radical collective-goal presentation; Legacy equal-weight progress cards. Evidence: [guild missions][screen-guild-missions].

### 6.36 Guild shop and guild rankings

**Entry:** guild Shop and Rankings tabs. **Purpose / primary goal:** spend personal Favor, or compare guild performance. **Secondary goals:** inspect limits/reset timing or another guild.

**Information:** Critical—purchase item/cost/eligibility, or rank/metric/guild; Important—balance after purchase and stock limits, or level/XP/member context; Contextual—reset time and secondary leaderboard columns; Rare—extended item/player detail. **Primary actions:** purchase or browse rankings. **Secondary actions:** filter, inspect, navigate public profiles.

**Current structure:** catalog/purchase aside comparable to Champion Market; sortable guild data table. **Strengths:** efficient, familiar transaction and comparison patterns. **Weaknesses:** Guild progression and player Favor can be confused if surrounding context is removed; rankings overlap the global destination.

**Freedom:** Must preserve economic and ranking facts; Reorganize entry points; Radical guild identity around the working structures; Legacy repeated explanatory containers. Evidence: [guild shop][screen-guild-shop], [guild rankings][screen-guild-rankings].

### 6.37 Cinder Bazaar: commodity browsing and order books

**Entry:** `/game/city/market-place`, commodity categories. A bound account is required. **Purpose / primary goal:** find an item and transact at a known price. **Secondary goals:** assess supply/demand, inspect Essence mechanics and place an order.

**Information:** Critical—item identity, side, quantity, unit/total price, available balance/stock and eligibility; Important—best bid/ask and own orders; Contextual—recent trade, median, volume and spread; Rare—extended item mechanics. **Primary actions:** buy/sell immediately or submit an order/listing. **Secondary actions:** filter/select, change quantity/price and inspect books.

**Current structure:** category/catalog selection, item-specific detail, buy/sell ticket, two order books and market statistics. Mobile uses book tabs and a sticky action area. **Strengths:** transactional density is justified; ownership and competing price levels are visible. **Weaknesses:** the correct distinction between immediate execution and an unfilled order demands explicit hierarchy; market jargon is a real onboarding burden.

**Freedom:** Must preserve quote, side, fee and ownership semantics; Reorganize secondary statistics; Radical market identity only around a legible transaction workspace; Legacy decorative nesting that makes books smaller. Evidence: [Bazaar shell][screen-market], [commodity exchange][screen-market-commodity].

### 6.38 Cinder Bazaar: exact equipment listings

**Entry:** Bazaar equipment browse. **Purpose / primary goal:** compare and buy one particular equipment instance. **Secondary goals:** narrow by slot/definition/variant/rarity/rank/tier and compare owned gear.

**Information:** Critical—exact item stats, quality/rank/variant, asking price and affordability; Important—comparison with current equipment and binding conditions; Contextual—sort/filter criteria; Rare—extended set/ability explanations. **Primary action:** buy selected item. **Secondary actions:** filter, sort and compare.

**Current structure:** filter/list/selected-item inspector. **Strengths:** treats equipment as individual items rather than interchangeable commodities; keeps purchase and equip-binding distinct. **Weaknesses:** several filters and narrow panes can compete with the most important item difference, especially inside the full shell.

**Freedom:** Must preserve exact-item identity and purchase quote; Reorganize filters; Radical object emphasis with adequate artwork, or typographic emphasis without it; Legacy presenting the purchase chiefly through generic summary boxes. Evidence: [equipment purchases][screen-market-buy], [Bazaar][screen-market].

### 6.39 Cinder Bazaar: selling, My Orders and trade history

**Entry:** Bazaar Sell and My Orders modes. **Purpose / primary goal:** list/sell eligible stock and manage outstanding commitments. **Secondary goals:** review fills, cancel orders and recover escrow.

**Information:** Critical—eligible item/quantity, price, gross/fee/net, side, order state and cancellation consequence; Important—best bid, listing capacity and expiry; Contextual—trade history and past proceeds; Rare—old transaction detail. **Primary actions:** sell immediately, create listing/order or cancel. **Secondary actions:** filter owned items, inspect history.

**Current structure:** owned-stock selection plus transaction review; own buy/sell order lists and history. **Strengths:** exposes fee/net and restriction reasons, prevents self-trading and incompatible own orders. Listing limits differ with Nobility. **Weaknesses:** navigation between catalog, sell and orders is local state rather than fully URL-addressable; a player may lose market context while checking an item elsewhere.

**Freedom:** Must preserve financial review and escrow states; Reorganize modes/history; Radical visual system with restrained emphasis; Legacy decorative card treatment of every order. Evidence: [sell][screen-market-sell], [orders][screen-market-orders].

### 6.40 Global Leaderboard

**Entry:** `/game/city/tavern`, labeled **Leaderboard**. **Purpose / primary goal:** compare performance within a chosen discipline. **Secondary goals:** find oneself, search a player/guild and inspect a profile.

**Information:** Critical—metric, scope, rank, subject and value; Important—own rank, search result and current page; Contextual—top-three podium; Rare—other ranking categories. **Primary actions:** choose metric, search/jump and paginate. **Secondary actions:** inspect profiles.

**Current structure:** Overall/PvE/PvP/Guild categories, metric controls, podium and table with independent own-rank information. **Strengths:** compact rows, cursor pagination and preserved results during refresh failure. **Weaknesses:** the route name does not match the visible system; overlap with local Arena/Guild rankings creates discoverability questions; table minimum width can require mobile scrolling.

**Freedom:** Must preserve metric/ordering semantics and own-rank context; Reorganize scopes/entry points; Radical recognition emphasis; Legacy the tavern metaphor implied only by an old route. Evidence: [Leaderboard][screen-leaderboard].

### 6.41 Quest Journal, branching rewards and community events

**Entry:** `/game/quests`; pinned objective links and journey prompts. **Purpose / primary goal:** identify and complete the next relevant objective. **Secondary goals:** inspect chains, choose a reward, track collective events and claim.

**Information:** Critical—objective, current/required progress, destination, choice consequences and claim eligibility; Important—chain position, rewards and event deadline; Contextual—source/story, community contribution/rank; Rare—completed history. **Primary actions:** pursue/pin/continue/turn in, confirm a choice, claim. **Secondary actions:** filter chains/history, inspect milestones and rankings.

**Current structure:** grouped chain list and detail pane, objective rows/rewards, chain-step navigation; server-wide events in the same journal with shared progress and personal thresholds. **Strengths:** direct objective navigation and explicit irreversible reward selection; shared event completion does not automatically imply individual eligibility. **Weaknesses:** personal chains, timed communal events and claim states share dense wrappers; the most useful next action can be separated from surrounding narrative.

**Freedom:** Must preserve branching, objective/claim states and contribution requirements; Reorganize history/event information; Radical journal identity; Legacy treating every quest state as an equivalent card. Evidence: [Quest Journal][screen-quests], [pinned tracker][screen-tracker].

### 6.42 Prophecies

**Entry:** `/game/prophecies`. **Purpose / primary goal:** choose and complete daily/weekly objectives, then claim and open rewards. **Secondary goals:** reroll when eligible and inspect weekly Favor milestones.

**Information:** Critical—available choice versus accepted objective, requirements, progress and ready claim; Important—weekly Favor, milestone eligibility, Fate Echo cost and expiry; Contextual—reward preview and cache contents; Rare—previous choices. **Primary actions:** choose, reroll, claim and open owned caches. **Secondary actions:** inspect weekly/Greater Prophecy and milestones.

**Current structure:** Favor diamond track, daily choice/active objective, ready rewards/caches and weekly Greater Prophecy sections. **Strengths:** an actual milestone sequence provides more identity than a generic progress bar. **Weaknesses:** repeated reward surfaces can fragment a single choose–complete–claim–open loop; competing claim prompts lack one clear center.

**Freedom:** Must preserve accepted/declined/expired/completed/claimed distinctions; Reorganize stages and secondary details; Radical prophecy presentation; Legacy equal prominence for every stage at once. Evidence: [Prophecies][screen-prophecies].

### 6.43 Settings, account binding and Nobility

**Entry:** `/game/settings`; guest restrictions link here. **Purpose / primary goal:** change preferences or manage account access. **Secondary goals:** redeem Nobility Signets, inspect benefits, read patch notes or log out.

**Information:** Critical—current setting/account binding, consequences of rename/redemption/logout, redemption quantity and resulting duration; Important—Signet balance, Nobility expiry and benefit restrictions; Contextual—patch notes/Discord; Rare—full perk explanation. **Primary actions:** change preferences, bind account, redeem. **Secondary actions:** rename when eligible, toggle badge, read notes and log out.

**Current structure:** prominent Nobility area precedes reading/layout/chat/account sections; inline quantity/redemption states and patch-note modal. **Strengths:** readable/system fonts and size choices, account safeguards and retry-safe redemption state. **Weaknesses:** membership information can dominate a utility destination; account binding required by market/social flows is mixed with unrelated preferences. Direct purchase is currently unavailable, not a working payment checkout.

**Freedom:** Must preserve binding, redemption and preference behavior; Reorganize membership/account/utilities; Radical composition is unnecessary for basic settings; Legacy treating a large benefits presentation as the automatic first settings task. Evidence: [Settings][screen-settings], [Nobility][screen-nobility].

### 6.44 Chat, loot history, help and global feedback

**Entry:** shell chat controls, item/player links, feature help and root overlays. **Purpose / primary goal:** communicate, maintain session awareness or recover from interruption. **Secondary goals:** inspect loot, get help and understand updates.

**Information:** Critical—channel/recipient, unread/latest message, actionable failure and current ongoing activity; Important—mentions, item/player context and offline rewards; Contextual—loot history, timestamps/help; Rare—old messages and release details. **Primary actions:** send/select channel, retry, dismiss a summary or follow help. **Secondary actions:** mention/whisper, link equipment, inspect/clear local loot history and change chat placement.

**Current structure:** docked/floating/mobile chat, popovers, session-summary dialog, notifications/toasts and guided overlays. **Strengths:** mention keyboard navigation, item links, scroll handling, reusable focus/escape behavior and offline-action recovery. **Weaknesses:** chat substantially reduces feature width; loot history is coupled to the desktop docked-chat area rather than an obvious independent destination. Multiple feedback layers can compete.

**Freedom:** Must preserve recipients, state/recovery and focus behavior; Reorganize placement and feedback priority; Radical contextual help/summary composition; Legacy allowing a utility panel to dictate every screen's usable width. Evidence: [chat][screen-chat], [loot history][component-loot], [session summary][component-session], [modal container][component-modal], [help][component-help].

### 6.45 Dormant, conditional and infrastructure-only views

**Entry/purpose:** public Hero/FAQ/World content has commented-out routes; shell/character/city/world host templates primarily provide routing; not-found and maintenance/access states handle navigation failure. Conditional development controls in events/raids are not release features.

**Priority/actions:** Critical—truthful restriction/error reason and valid recovery/return action; Important—retry availability; Contextual—help; Rare—technical details. **Strengths:** separate public/auth/game boundaries. **Weaknesses:** file presence can falsely imply a live feature during redesign planning.

**Freedom:** Must preserve truthful routing/error states; Reorganize recovery; Radical future public presentation only within a separate scope; Legacy unused components should not dictate a new design. No active gathering, crafting, companion-deck, manual-save or faction-management journey was found merely because asset names or references suggest them. The Essence evolution API/state is not evidence of a currently exposed evolution screen. Older hover/click popover components have no external usages found; the combat-filter modal exists but no current opener was found. These are maintenance observations, not requests to delete code. Evidence: [public routes][route-public], [dashboard routes][route-dashboard], [world routes][route-world], [Essence state][state-essence].

## 7. Major Player Journeys

The following describes actual transitions and decision dependencies. Friction is inferred from source structure; the number of clicks and player frequency have not been measured.

| Journey and starting point | Screens and decisions; information needed | Current friction / context loss | Presentation opportunity to investigate |
|---|---|---|---|
| First entry: Login | Guest/sign-in → bootstrap → First Steps → training fight → absorb/attune → Arms Chest/equip → Lumo → chapter objectives. Need account status, irreversible starting choice, next objective and eligible action. | A player learns multiple destinations before understanding how a build works. Progressive navigation reduces overload but hidden destinations can look absent. | Make the causal sequence of fight, acquire, attune and improve understandable; preserve direct objective links and staged access. |
| Returning after absence | Login/game → offline resolution and session summary → ongoing combat or chosen activity → inventory/rewards. Need elapsed activity, gains/losses, whether action continues, and recovery status. | Summary overlays interrupt orientation; dismissing a summary is different from stopping combat. Loot history and ongoing-action controls occupy different places. | Treat return as re-establishing the current game state, with clear next decisions and reliable recovery. |
| Choose an activity | Shell/current action or quest → region/area, dungeon, Tower, raid or PvP. Need unlocks, requirements, objective relevance and whether another action is active. | Activity taxonomy spans World and City; availability changes by journey stage, live event and runtime flag. Regional boss/raid entries are contextual. | Compare purposes and readiness without inventing a universal dashboard or exposing locked systems prematurely. |
| Improve equipment | Inventory → compare → equip/loadout or reinforce/restyle → return to activity. Need slot delta, set effects, exact cost, protected/borrowed status and preset behavior. | Small-screen selection hides the catalog; build effectiveness also depends on Essences/styles elsewhere. Snapshot activities may still use an older build. | Preserve rapid comparison while making the relationship between gear change and the next intended activity legible. |
| Develop an Essence build | Loot/region/quest → Absorb → Archive → select active/passive slots → level/Ascend → assign activity loadout. Need owned/spare status, ability roles, caps, costs and active preset. | Acquisition, collection, individual development and build management coexist in one large feature. Creature/source exploration requires changing views and sometimes regions. | Explain acquisition → usable ability → build role → development, retaining dense ability inspection. |
| Target a missing creature/Essence | Archive/Creatures/Codex → choose focus/location → regional combat → return to absorb. Need missing/owned/completion state, source, focus cooldown and drop effect. | Codex and source pages do not always offer an immediate onward action; selected collection context can be lost when leaving the feature. | Connect a collection gap to its actual source and return state, without adding unsupported map geography. |
| Prepare a complete build | Character overview → equipment/loadouts → Essence activity assignment → Combat Style preview/save → activity readiness. Need gear, active/passive abilities, style conditions, current versus preview and captured version. | Three configuration surfaces and different save models make “which build will fight?” difficult to answer. There is no single composition showing their relationship. | Make build causality and capture timing central; retain preview/save/discard and explicit snapshot updates. |
| Run a dungeon | Region → dungeon preview/difficulty → requirements/sigil assembly → start → revealed graph → encounter/rest/treasury decisions → secure rewards or failure. Need Vigor risk, possible next rooms, pending loot and retreat outcome. | Long preparation copy and rewards can obscure readiness; moving to combat detail may weaken route/risk context. | Preserve the graph and risk ledger as core gameplay; distinguish unclaimed carried loot from safely secured rewards. |
| Climb the World Tower | Tower floor → scouting/readiness → create/join expedition → captured build/party assignment → start → report/replay → next floor or history. Need Guardian evidence, role/readiness, release/gate and first-clear/Echo rules. | Ascent, recruitment, preparation and historical records live across several surfaces; rating alone is an incomplete readiness explanation. | Give floor, known Guardian behavior and cooperative preparation a coherent relationship. |
| Coordinate a raid | Regional raid preview → difficulty/recruitment → muster/party assignment → optional plan preview → three preliminary encounters → Final Assault → claim. Need role purpose, deadline, captured build, probability versus fact and reward rules. | High command density; preview figures can be mistaken for guarantees; changing equipment elsewhere does not automatically communicate a submitted-build change. | Express the dependency between the parties and final boss while preserving roster administration and uncertainty. |
| Follow a regional event | Region/event entry → scheduled/sign-up or live boss → party/revive updates → milestone/result claims. Need timing, automatic/manual entry, party state and personal eligibility. | Quiet/live/completed states have different goals but share report structures. A participant may arrive after automatic enrollment. | Lead with the current event phase and player involvement, keeping older reports secondary. |
| Compete in Arena | Colosseum → defense update/opponent comparison → challenge → combat/result → records/rank/rewards. Need ticket cost, current defense snapshot, rating stakes and outcome explanation. | Seven peer metrics distract from opponent choice; ranking and reward destinations are separate tabs and overlap global ranking. | Preserve explicit stakes and comparison; give challenge/result a stronger competitive identity. |
| Enter a tournament | Grounds → team invitations/applications → registration/loadout readiness → bracket/next match → replay → placement reward/history. Need deadlines, three-player roster, capture timing and qualification. | Team management is a separate social workflow from Guild; round navigation and older rewards can distract from the next match. | Keep the team's current competitive state understandable throughout the weekly lifecycle. |
| Join and contribute to a guild | Discovery/public profile → application/invite → headquarters → mission/building target → contribute → personal/guild rewards. Need membership/role, collective objective, own contribution and claim entitlement. | Identity, officer administration and personal spending coexist; shared progress can be confused with personal eligibility. | Separate meanings through hierarchy while keeping the guild's identity and common objective recognizable. |
| Borrow/donate guild equipment | Inventory/Vault → inspect eligibility/ownership → donate or borrow → equip → return. Need permanent donation terms, borrower state and restrictions. | Familiar item presentation hides unfamiliar ownership consequences; donating is not depositing for later withdrawal. | Maintain explicit ownership transitions and comparison without burying conditions in decoration. |
| Trade | Bazaar → catalog/item → books/comparison → quote → execute/place order → My Orders/history. Need exact item, quantity, side, gross/fee/net, available funds, escrow and expiry. | Local mode state and leaving for inventory can lose item/price context; immediate trade versus outstanding order is complex. | Protect the transactional workspace and continuity; visual identity must not obscure numbers or commit state. |
| Pursue and claim progression | Pinned objective/Quests/Prophecies/Achievements/Soulstones → inspect target → activity → completion → claim/open/spend/equip title. Need requirement, progress, eligibility, choice and claimed state. | Related outcomes are split across systems with similar visual wrappers; completion, claim and opening a cache are distinct steps. | Make reward lifecycles and reasons to revisit clear without merging different currencies or mechanics. |
| Social inspection | Chat/item link/ranking/guild roster → character or item detail → whisper/return. Need identity, self-versus-other context and original location. | Profile navigation can replace the current feature while a selection or transaction is in progress. | Preserve return context and useful inline inspection; keep message recipient explicit. |

Evidence: [journey rules][journey], [inventory][screen-inventory], [Essences][screen-essences], [Combat Styles][screen-styles], [dungeon][screen-dungeon-run], [raid][screen-raid], [Tower][screen-tower], [Colosseum][screen-colosseum], [guild][screen-in-guild], [Bazaar][screen-market], [quests][screen-quests], [chat][screen-chat].

## 8. Information Architecture and Navigation

### 8.1 Current map

This is the current system structure, not a proposed menu. Child views listed after a route may be local tabs or selected states rather than separate URLs.

```text
Public: /login, /signup                 / redirects to login
Game shell: /game                      default → Character Overview
├─ Character
│  ├─ /character/character-overview     self / searched character
│  ├─ /character/inventory              gear / stock / loadouts / inspector
│  ├─ /character/essences[/:essenceId]   Archive / Absorb / Creatures / Codex
│  ├─ /character/combat-styles          build preview, mastery and refinements
│  ├─ /character/achievements           collection / titles
│  └─ /character/soulstone-archive      permanent upgrades
├─ World
│  ├─ /world                           redirect to current/first region
│  ├─ /world/shenic, /world/meran       areas / dungeon and raid previews
│  ├─ /world/:id                       region lookup (last matching route)
│  ├─ /world/dungeon                   current dungeon run
│  ├─ /world/raid/:raidId               muster / play / result
│  ├─ /world/tower                     ascent / recruitment / scouting
│  ├─ /world/tower/expeditions/:rallyId expedition
│  ├─ /world/tower/personal-expeditions history
│  ├─ /world/tower/hall-of-fame         first-clear records
│  └─ /world/region-boss                scheduled / live / completed event
├─ City
│  ├─ /city/guild[/:guildId]            discovery / headquarters / public guild
│  ├─ /city/colosseum                  Arena / Tournament / Market / Rank / Record
│  ├─ /city/colosseum/tournaments/:tournamentId/matches/:matchId/replay
│  ├─ /city/market-place               Bazaar browse / sell / orders
│  └─ /city/tavern                     Leaderboard, not a tavern simulation
├─ /quests                            journal / community events
├─ /prophecies                        daily / weekly / claims / caches
├─ /settings                          reading / layout / account / Nobility
└─ /combat                            shared idle combat viewer

Across the shell: currencies, character identity, pinned objective,
ongoing action/dungeon/raid, chat, loot, help/tour, recovery and summaries.
```

Route suffixes above sit under `/game`. Actual ordering, redirects and guards matter; the diagram does not imply that every branch is always unlocked. Evidence: [app routes][route-app], [dashboard routes][route-dashboard], [character routes][route-character], [world routes][route-world], [city routes][route-city], [sidebar service][service-sidebar].

### 8.2 Persistent, secondary and contextual information

**Persistent candidates justified by current behavior:** player/account identity, current action with a safe way to return/stop, relevant currency, communication access and a compact route to the main activity/build systems. These support cross-feature decisions. This is not an argument that all fifteen sidebar links and every resource must be equally prominent at all times.

**Secondary systems:** detailed records, Hall of Fame, full rankings scopes, title management, Codex totals, vendor inventories, patch notes and infrequent account operations. They need reliable discovery and return paths, but their presence should not compete continuously with a live dungeon or transaction.

**Contextual systems:** item comparison, reinforcement/restyling, captured-loadout update, raid applications, dungeon entry assembly, branch reward choice, event claims and character inspection. Their necessary information comes from the selected entity/activity, so presenting them as unrelated destinations loses meaning.

### 8.3 Navigation problems to resolve, without prescribing a replacement

- **Taxonomy follows broad places rather than all player goals.** Preparing a build crosses three Character destinations; choosing combat spans World and City; reward claims occur in many features. These are legitimate systems, but the menu alone does not explain their relationship.
- **Naming has historical residue.** The Leaderboard uses `tavern`; Tower code and identifiers retain rally/Legacy Ascension terminology. Visible terminology and technical route compatibility are different concerns.
- **Nested orientation can be expensive.** Persistent shell group → feature heading → local tabs → selected list item → inspector is useful in Inventory, but repeated on many progression screens it produces a website-like stack of headings and boxes.
- **Some context is URL-backed and some is not.** Essence selection has a path and views use query state; Colosseum tabs use query state. Bazaar modes and regional dungeon/raid preview selection rely more heavily on local state. Back/refresh/share behavior therefore needs a deliberate audit during implementation.
- **Access is dynamic.** First Steps, progression guards, account restrictions and a raid flag govern visibility/actions. A redesign must avoid treating a temporarily hidden feature as permanently nonexistent or enticing players with unusable actions without an explanation.
- **Utility prominence is uneven.** Docked chat can take more horizontal space than a selected-item inspector; loot history depends on that chat layout. Nobility dominates Settings while account binding is the reason many guests visit it.
- **Duplicate ranking entry points require purpose.** Local Arena/Guild rankings are useful in context; the global ranking browser is useful for comparison. Their distinction should be understandable rather than removed automatically.

Evidence: [shell][screen-shell], [sidebar][service-sidebar], [market component][state-market], [region component][state-region], [journey][journey], [settings][screen-settings].

## 9. Visual System Audit

### 9.1 Palette and semantic roles

The current language is dark charcoal, warm pale gold, ivory text, restrained colored states and rarity/ability accents. It is more coherent at the token level than the varying feature compositions suggest. Actual token values follow; translucent surfaces depend on what lies behind them, so these are not measured rendered contrast ratios.

| Role | Implemented values | Design consequence |
|---|---|---|
| Canvas/background | `#0e0f14`, `#17171e`, deep `#0f1016` | A narrow dark range; nested surfaces can look like one low-contrast mass. |
| Main surfaces | `rgba(16,16,20,.72)`, strong `rgba(11,11,15,.72)`, soft white `.04`, elevated `#292830` | Layering is available, but many consecutive layers add enclosure without much hierarchy. |
| Hover/selected | White `.08`; pale gold `.12` | Restrained state feedback; small selection changes may compete poorly with colored entity names. |
| Text | Primary `#f6f0df`, secondary `#c3bec4`, muted `#a9a9b2`, disabled `#868690` | Comfortable intended separation; tiny muted copy over textured/translucent surfaces needs runtime verification. |
| Primary accent | `#f9dca0`, stronger `#fcd587` | Used for headings, currency, selection, progression and action. The issue is overloaded meaning, not simply excessive color count. |
| Borders/focus | White `.12`, strong gold `.32`, control border `#77747e`, focus `#fcd587` | Thin frames are ubiquitous. Focus has an explicit strong treatment worth retaining. |
| Semantic states | Danger `#ff9aa2`, success `#41f1b6`, warning `#ffbb55`, info `#8ecbff` | Distinct roles exist, although feature-specific rose/emerald utility classes and button choices can drift from them. |
| Rarity | Common `#d4d4d8`, Uncommon `#41f1b6`, Rare `#7cb7ff`, Epic `#e879f9`, Unique `#facc15`, Legendary `#fb923c`, Legacy `#fb7185` | Valuable item semantics. Several colors overlap success, warning and danger families; accompanying labels matter. |
| Damage types | Physical `#e6e2d9`, Magical `#9d86ef`, Bleed `#d94d5c`, Burn `#ef8a3c`, Poison `#82b94b`, Shadow `#69b6dd`, None `#8d8991` | Color is informational and should not be discarded as decorative clutter. Preserve written type and numerical values. |

Tailwind also contains named royal/ancient/blood colors, while templates use local utility colors. A definition alone does not prove current visible use. A future palette audit should distinguish obsolete aliases from necessary rarity/status/damage distinctions. Evidence: [tokens][style-tokens], [Tailwind configuration][config-tailwind], [global styles][style-global].

### 9.2 Typography and numbers

- **Fonts:** body Poppins with Helvetica/sans-serif fallback; display Marcellus. Google font imports request Poppins 300/400 and Marcellus. Tokens also use 600/700, so some heavier Poppins rendering may depend on synthetic weight. Atkinson Hyperlegible has local regular/bold/italic/bold-italic files. Readable and system-font preferences replace both body and display families.
- **Scale:** default root size is 14px, with larger reading preferences. Token sizes run from `.75rem` through `2.857rem`: approximately 10.5, 12, 14, 16, 20, 24, 30 and 40px at default size. Tailwind utilities coexist with this scale. Tiny uppercase labels therefore become a material legibility concern, not just a stylistic detail.
- **Hierarchy:** `DefaultHeader` commonly gives a gold display heading at `text-xl`/`sm:text-2xl`; repeated feature labels and explanatory text follow. Character Overview already has larger level/rating numbers, so “no scale contrast anywhere” would be incorrect. The problem is that many other features start at the same modest heading scale regardless of purpose.
- **Weight and spacing:** light body text is common; global span styling assigns body font/light/small text unless overridden. Uppercase tracked eyebrows identify many groups, sometimes duplicating a heading immediately above. Local tight tracking and display styles are not a single uniformly applied hierarchy.
- **Numerals:** tabular numerals appear in inventory/trading and other metric areas; some trading layouts use monospace. Large numbers have meaning only when the user knows whether they represent level, rating, quantity, probability or remaining resources. The references' oversized levels cannot be transferred indiscriminately to every LL metric.
- **Personality:** Marcellus gives a restrained fantasy cue. The current distinction is mostly “display label versus data,” rather than typography actively defining each screen's composition as in R1/R7/R9.

Evidence: [tokens][style-tokens], [font imports][config-index], [global rules][style-global], [page header][component-header], [Character Overview][screen-overview], [market styles/templates][screen-market-commodity], [Settings][screen-settings].

### 9.3 Surfaces, spacing and controls

| Element | Actual implementation | Assessment and design implications |
|---|---|---|
| Borders | Thin neutral/gold borders on `ll-panel`, `ll-card`, rows, inputs and selected states | A line may mean grouping, interaction or selection. Nested outlines dilute these meanings; keep boundaries that separate actual decisions. |
| Radius | Tokens `.25rem`, `.375rem`, `.5rem`, `999px`; cards generally smaller radius than panels | At default root size these are modest. Not every surface is an identical large rounded rectangle; changing radius alone would miss the problem. |
| Shadows | Ambient `0 0 16px` black `.24` / `28px` black `.28`; separate focus ring | Low-contrast depth rather than pervasive neon. Shadow repetition is less consequential than enclosure repetition. |
| Texture/gradients | Shared texture treatment, subdued dark brown/blue layering, selected-navigation gold; localized attention effects | Background treatment often supplies generic atmosphere rather than location. It is not universally a colorful gradient-blob layout. |
| Spacing | Quarter-rem token steps, Tailwind gaps/padding, repeated page margins | Consistency helps scanning; applying the same padding to every nested surface wastes width. Equal gutter treatment is an assumption, not a gameplay requirement. |
| Buttons | `RegularButton`: outlined gold primary, green secondary, rose danger; CSS `ll-button-primary`: solid gold. Separate ornamented login button and mini button | The same semantic label can have different visual weight. Danger refresh in Arena is a concrete mismatch. Preserve disabled/pending states while rethinking visual hierarchy. |
| Inputs | Shared dark input/select styles plus native controls and authentication form components; search, numeric steppers and multi-filter rows | Input behavior suits dense RPG work. Numerous filters at equal weight increase scanning cost; immediate validation and accurate quantity/price remain essential. |
| Tabs | Older content-projected tabs, navigation tabs and filter tabs; many feature-local segmented controls | Multiple visual/behavioral tab families exist. A redesign needs a distinction between destinations, filters and temporary subviews rather than one universal pill row. |
| Tables/lists | Inventory sortable rows, order books, guild members/rankings, combat statistics and expedition history | Often the right form for comparison. Do not replace these with large art cards simply to appear more game-like. |
| Cards/stat tiles | Global CSS primitives used throughout features, sometimes nested; not one universal Angular Card component | Repetition is an authored composition/style habit as much as a component constraint. Cards work better for bounded collectible objects or opponents than every label/value pair. |
| Icons | Bespoke feature/sidebar SVGs, small equipment/reward art, Material font imports and text glyphs | Custom art already exists. Meaning and consistency need review; adding more generic icons is not a substitute for hierarchy. |
| Item presentation | `app-item` emphasizes rarity-colored names and tooltips; equipment details expose stats/set/ownership | Rarity is not universally a colored border. Current lists are largely textual, limiting object silhouette recognition but enabling compact comparison. |
| Popovers/tooltips | CDK-based positioned overlays, outside/Escape behavior, hover/focus/touch support in the current primitive; some native titles remain | Preserve inspection without navigation. Critical cost, ownership and destructive consequences cannot depend on a tiny hover target. |
| Dialogs | Root modal container, reusable `appDialogFocus`, and feature-native dialogs such as Combat Styles | Useful focus trapping/restoration exists, but mixed stacks require runtime checks. Detached popovers intentionally sit above modals. |
| Notifications | Header/quest indicators, in-feature dots/badges, toast, update dialog, session summary and chat notices | Multiple urgency channels need an order of precedence. A new visual identity must retain failures, actionable rewards and ongoing-state feedback. |
| Progress | HP/barrier bars, XP, quest counts, collection completion, upgrade ranks, dungeon Vigor, Tower rail, Prophecy milestones | These represent different concepts. Similar bars must not imply identical stakes; depleting Vigor is not another completion percentage. |
| Navigation | Fixed shell, sidebar, local tabs and selected panes | Predictable reachability is useful, but strong permanent framing reduces the space for screen-specific composition. |

Evidence: [global styles][style-global], [tokens][style-tokens], [buttons][component-button], [login button][component-login-button], [tabs][component-tabs], [navigation tabs][component-nav-tabs], [popover][component-popover], [dialog focus][directive-dialog], [item][component-item], [notifications][component-toast].

### 9.4 Responsive behavior

The design already supports desktop and mobile through more than simple stacking. The shell uses `100dvh`, constrained flex children and nested scroll regions. Desktop sidebar widths are approximately 20rem detailed or 8rem compact; mobile uses a slide/swipe panel. Docked chat appears at large desktop widths and uses roughly 24rem, growing to 430px at the largest layout; floating and small-screen chat have separate sizes. These widths materially determine feature density.

Tailwind defaults include 640/768/1024/1280/1536px viewport breakpoints; the token file names 40/48/64/80rem breakpoints. CSS media-query units, root reading size and container-relative available width must not be treated as interchangeable. Inventory uses container thresholds around 48rem and 68rem for two/three panes; its small-screen inspector replaces other content. Overview has separate container thresholds for stat and profile columns. Tower uses list/detail and preparation switching; combat becomes stacked unit detail; some tables retain minimum widths and horizontal scrolling.

**Design implication:** a 1677px-wide reference composition does not fit into a 1677px-wide LL viewport after sidebar, chat and gutters. Evaluate concepts at the actual content width and with large text, long names, expanded details, keyboard focus and mobile chat open. No runtime clipping or touch-target results are claimed in this audit. Evidence: [shell][screen-shell], [inventory styles][style-inventory], [overview styles][style-overview], [Tower styles][style-tower], [chat styles][style-chat], [Tailwind][config-tailwind].

## 10. Component Architecture from a Design Perspective

### 10.1 Repetition is measurable, but counts are not a verdict

A source scan of 122 external HTML templates, after removing HTML comments, found these exact class-name occurrences. Counts include class strings in bindings and inactive templates; they are not rendered instance counts or percentages of screen area.

| Shared class | Occurrences | Templates containing it |
|---|---:|---:|
| `ll-panel` | 130 | 41 |
| `ll-card` | 82 | 29 |
| `ll-stat-card` | 30 | 11 |
| `ll-badge` | 56 | 17 |
| `bg-texture` | 49 | 24 |

Template opening-tag counts also found `app-default-header` in 15 templates, 67 `app-regular-button` uses in 21 templates, 21 `app-item` uses in 14 templates, 25 `app-character-tag` uses in 15 templates, and eight embedded `app-combat` uses in addition to its routed role. A repeated list item can multiply at runtime. These figures locate design pressure; they do not establish that reuse itself is harmful.

### 10.2 Behavioral reuse versus visual coupling

| Component / primitive | Where used and responsibility | Benefit | Restriction and future treatment |
|---|---|---|---|
| Dashboard shell, Header, Sidebar | Every authenticated feature; orientation, resources, action continuity, chat | One predictable navigation and recovery layer | Highest structural coupling. Preserve responsibilities and responsive behavior; allow a future concept to question permanent widths, gutters and equal framing. |
| `DefaultHeader` / `FeatureIcon` | Many main features; title, icon and help entry | Consistent naming/help discovery | Repeated icon–heading starting point reduces scene-specific openings. Current header icons use the less-decorated sidebar mode; do not blame ornament that is not rendered. Keep help behavior without requiring an identical header. |
| `.ll-panel`, `.ll-card`, `.ll-stat-card`, `.ll-badge` | Global CSS classes used directly in templates | Central tokens and consistent states | Strong visual coupling without an Angular Card abstraction. Preserve semantic tokens; stop assuming any data group requires one of these wrappers. |
| `RegularButton`, CSS button classes, `Button`, `MiniButton` | Game actions, utility controls, authentication | Pending/disabled semantics and keyboard interaction | Inconsistent visual emphasis across families. Preserve action states and accessible names; allow context-appropriate appearance. |
| `Tabs`, `NavigationTabs`, `FilterTabs` | Guild/Colosseum and other destination/filter groups | Shared selection behavior | Old content tabs destroy inactive content via conditional rendering; selection persistence depends on parent state. Arrow-key selection does not itself move focus as the newer navigation tabs do. Unify behavioral expectations, not every visual treatment. |
| `Item`, equipment displays/set progress | Inventory, loot, shops, quests, chat and previews | Consistent item identity, rarity, comparison and ownership explanations | Text-first identity limits composition but is efficient. Keep the item contract/inspection behavior; allow list, plaque, reward and detail presentations. |
| Equipment upgrade panel | Inventory equipment inspector/modal | Before/after, costs and eligibility in one place | Preserve heavily. Surrounding frames can change without rebuilding the transaction logic. |
| Equipment loadouts | Inventory and relevant build contexts | Autosave/application and locked-preset recovery | Retain save status, conflict/error and copy semantics. A preset's visual form need not dictate the whole build screen. |
| Essence details/description/ability tags | Archive, acquisition, item previews and combat ability explanations | Shared mechanical wording and scaled effects | Nested ability cards can over-enclose dense text. Reuse the formatter and data hierarchy while adapting composition to comparison versus inspection. |
| `Combat` and entity statistics | Idle battle, dungeon, raid, Tower, regional boss and tournament replay | Consistent playback, statistics and event interpretation | Most consequential visual sameness across game modes. Preserve engine/statistics behavior; do not require every battle context to lead with the same analytic report. |
| `CharacterTag` / presence / title decorations | Chat, guild, rankings, parties and social inspection | Stable identity, profile access and status | Beneficial reuse. Allow context-specific scale and density while retaining identity and recipient clarity. |
| Generic leaderboard/podium | Arena; other ranking pages also have their own implementations | Ordered comparison and profile access | Do not assume all rankings already share one architecture. Keep rank/metric behavior; recognition and history can have different emphasis. |
| Popover/overlay registry | Items, abilities, context inspection | Positioning, collision handling, keyboard/pointer dismissal | Preserve heavily; surfaces can differ by content. Older unused popovers should not define future behavior. |
| Modal container / dialog-focus directive | Global and feature dialogs | Focus trap, initial focus, restoration and escape | Preserve heavily. Appearance does not justify losing focus behavior; consolidate only where behavior actually matches. |
| Help/tour/quest tracker | Shell and feature-specific instruction | Context-sensitive guidance and direct next steps | Keep accurate references and focus. A layout change requires updating tour target selectors and screenshots/help where applicable. |
| Session summary, toast and current-action controls | Bootstrap, mutations and ongoing play | Recovery, timely feedback and activity awareness | Retain distinct severity and lifecycle. Different message meanings need hierarchy instead of one generic notification surface. |

Evidence: [shell][screen-shell], [header][component-header], [feature icon][component-feature-icon], [global styles][style-global], [tabs implementation][state-tabs], [navigation tabs implementation][state-nav-tabs], [equipment upgrade][component-upgrade], [Essence formatter][component-essence-description], [combat][component-combat], [character tag][component-character-tag], [popover][component-popover], [dialog focus][directive-dialog], [help][component-help].

**Conclusion:** components constrain behavior productively in several places. The stronger source of accidental sameness is composition built repeatedly from global surface classes, generic summaries and similar heading stacks. A designer can keep the existing state/services, formatter logic and input/accessibility primitives without preserving those compositions.

## 11. Systematic Audit of “AI-Generated UI” Signals

“AI-generated” here names a perceived visual pattern, not the provenance of this code. Source inspection cannot determine who or what authored a screen. Verdicts below distinguish supported problems from partial evidence and counterexamples.

| Signal | Finding and concrete evidence | Verdict / consequence |
|---|---|---|
| Excessive component repetition | Global panel/card classes recur across unrelated features; default headings and repeated summary groups in Achievements, Soulstones, Codex and Colosseum | **Supported selectively.** Shared logic is not the problem; identical visual priority for different goals is. |
| Cardification | Codex collection → bonus group → member rows; Soulstone upgrade cards repeat rank, effect and action; achievement collections repeat framed entries | **Supported.** Compare R6, where each card is one collectible object; a card is not intrinsically generic. |
| Container nesting | Shell frame/content surface → feature panel → local card → inner row/ability group, particularly Archive/Codex and Guild | **Supported.** Width and emphasis are spent on successive boundaries with little new meaning. |
| Excessive/identical radius | Tokens have small, medium, large and full values; most game containers use small radii | **Mostly unsupported.** Familiar rectangles matter more than extreme rounding. |
| Predictable heading–subtitle–grid rhythm | Repeated default headers, muted explanatory copy and summary tiles in several progression features | **Supported.** Not universal: Inventory, Tower and dungeon graph have different spatial logic. |
| SaaS/analytics dashboard character | Seven Colosseum metric tiles, four achievement summaries, numeric character sheets and combat contribution tables | **Partly supported.** Combat analysis is useful; making analysis the dominant battle experience everywhere is the larger question. |
| Generic gradients | Shared dark texture/gradient surfaces; selected navigation and localized feature treatments | **Limited.** No evidence for pervasive bright gradient blobs; gradients are not the primary cause. |
| Generic shadows | Shared small ambient black shadows | **Limited.** More a low-contrast grouping issue than showy drop-shadow decoration. |
| Glassmorphism | Translucent surfaces and localized backdrop blur in chat/dungeon preparation | **Partial, not universal.** Avoid describing the entire application as frosted glass. |
| Glowing borders | Quest attention and authentication decoration; focus ring throughout | **Localized.** Keyboard focus is functional feedback, not decorative noise to remove. |
| Generic icons | Mixed text glyphs/Material sources alongside bespoke sidebar/feature SVGs | **Mixed.** Meaningful custom assets are a strength; icon-plus-label repetition still flattens composition. |
| Pills and badges | `ll-badge` is widespread, often overridden with a less-rounded shape; tags indicate rarity, categories and states | **Partial.** Several badges carry important semantics. Badge proliferation can make a completed collection look as urgent as a required action. |
| Generic stat cards | Achievement/Soulstone summaries, Colosseum status tiles and some Guild counts | **Supported.** Equal-sized totals imply equal decision value when the next action usually depends on only a subset. |
| Generic progress cards | Soulstone upgrades, achievement chains and Codex blocks repeat progress-plus-action presentation | **Supported selectively.** Prophecy milestone track and Tower ascent rail are counterexamples with meaningful sequences. |
| Excessive symmetry | Two-column collections and uniform summary grids | **Partial.** Inventory's unequal columns, trading books/ticket and Tower list/detail are intentionally unequal. |
| Repetitive grids | Region area cards share artwork; collections and upgrades repeat dimensions | **Supported where entities lack differentiation.** Tables/grids are necessary when comparison is the task. |
| No focal composition | Character Overview is mostly identity text, rating and attributes; many feature openings give title and summaries similar weight | **Supported in major identity screens.** Login has a strong illustrated focal environment, and Overview already enlarges some numbers. |
| Weak screen identity | Achievements, Soulstones and Codex are visually closer than their mechanics; region art does not distinguish locations | **Supported.** Dungeon graph, tournament rounds and order books already signal their system. |
| Weak typography | Poppins/Marcellus offer a real pairing, but many labels use the same small gold display hierarchy | **Partial.** More typographic direction is possible without declaring the current fonts inherently unsuitable. |
| Insufficient scale contrast | Repeated modest headers and inset section labels; character level/rating exceptions | **Supported locally.** Enlarging every number would reproduce the problem at a larger scale. |
| Unintentional whitespace | Fixed shell allocation and repeated padding reduce space for content; some equal cards include variable copy | **Inferred risk.** Actual empty-space balance cannot be established without rendering representative data. No evidence proves filler was added just to occupy space. |
| Weak environmental storytelling | One global study background; the same combat-area image reused for differently named places; themed background assets appear dormant | **Supported.** Existing art is not used to explain location as in R10. Bespoke scenes are an optional resource decision. |
| Weak relationships between mechanics | Gear, Essence loadouts and Style choices separate build causes; submitted snapshots live in activity pages | **Supported.** R4's spatial connection of selected item, stat change and character illustrates a transferable principle. |
| Decorative noise without meaning | Repeated texture and frames around already grouped content; localized ornaments | **Partial.** Rarity colors, damage labels, graph edges, focus rings and progress landmarks are meaningful and should survive. |
| Generic Tailwind/Bootstrap/Material appearance | Tailwind classes and some Material infrastructure exist, but much of the game uses custom components/CSS | **Not a framework diagnosis.** There is no basis for calling it a stock Bootstrap or Material dashboard. Changing libraries would not fix hierarchy. |
| Independently styled features | Multiple button/tab families, local color overrides and different layout approaches | **Supported as design drift, not author provenance.** A shared art direction should harmonize meaning while allowing different screen structures. |

Evidence: [Achievements][screen-achievements], [Soulstones][screen-soulstones], [Archive/Codex][screen-essences], [Colosseum][screen-colosseum], [guild][screen-in-guild], [region][screen-region], [area cards][component-area], [combat][component-combat], [tokens][style-tokens], [global CSS][style-global], [login][screen-login], [Inventory][screen-inventory], [dungeon][screen-dungeon-run], [Tower][screen-tower].

**Priority of the diagnosis:** first examine composition and information hierarchy; then relationships between systems and context continuity; then the visual vocabulary of surfaces/type/actions. Merely changing colors, adding angular corners or removing every card would leave most of the supported problems intact.

## 12. Screen Identity: What Makes Each System Itself?

These anchors are existing mechanics/data. They are not promises that corresponding illustrations exist.

| System | Existing anchor | Where identity is weak now | What a concept must make understandable |
|---|---|---|---|
| Entry / return | Persistent adventurer, offline activity and First Steps | Login atmosphere and in-game status are disconnected | Who the player is, what happened, what continues and what is next |
| Character | Name/title/guild, level, rating, attributes and build | Identity competes with an attribute report; no focal character art in the active sheet | The relationship between identity, capabilities and equipped build |
| Equipment | Eight slots, individual stat rolls, quality/rank, sets and variants | Mostly names/rows and repeated inspector groups | Selected object, actual difference and ownership |
| Essence Archive | Creatures become active/passive build abilities; absorption/Ascension | Collection, upgrade and loadout functions share generic panels | Source → ability → role → development |
| Creatures / Codex | Source habitats, focus, kill/collection history and completion bonuses | Repeated textual cards underplay discovery and missing links | What is known, missing and worth pursuing |
| Combat Styles | Distinct rules and milestone choices for Bastion, Conduit, Duelist, Reaper | Mechanic descriptions and preview facts can feel like settings | What changes the build's behavior, including conditions and tradeoffs |
| Regions | Shenic/Meran, area names/enemies and progression gates | Reused art and uniform area tiles flatten location | Where progress leads and why this area matters |
| Dungeons | Revealed route, room type, Vigor and carried loot risk | Supporting panels can compete with route consequences | The next route decision and what is at stake |
| Tower | Floor/Guardian, realm first clear, scouting and cooperative readiness | Guardian identity is textual and history resembles routine records | Collective ascent, what is known and who is ready |
| Raids | Three interdependent parties followed by one Final Assault | Many administrative metrics precede the encounter relationship | How each party changes the final battle |
| Regional boss | Scheduled participation, shared boss state, Fury/revival | Live event often resembles another combat report | The current event phase and personal contribution |
| Combat viewer | Automatic encounter, party/summons, typed effects and outcome | Statistical inspection dominates moment-to-moment expression | What happened, why it mattered and where control actually exists |
| Arena | Opponent choice, rating stakes, defense snapshot | Status tiles dilute the contest | Who to challenge and what the result means |
| Tournament | Team of three, timed registration and elimination rounds | Bracket competes with management/history | Team readiness and path through the competition |
| Guild | Shared identity, roles, Vault, supplies, buildings and mission | Numerical administration can dominate belonging | Collective purpose, contribution and authority |
| Bazaar | Price discovery, individual gear, orders and escrow | Useful exchange structure lacks distinctive setting | A reliable transaction with clear item, side and commitment |
| Quests / events | Branching chains, direct objectives and community thresholds | Similar frames obscure different lifecycles | What to do next and whether reward entitlement is personal or shared |
| Prophecies | Daily choice, weekly Favor sequence, reroll and caches | Loop spread over repeated claim/progress sections | Commitment, progress, milestone and claim/open sequence |
| Achievements / titles | Durable accomplishment, Renown and public identity | Generic collection progress underplays recognition | What was accomplished and how it can be expressed |
| Soulstones | Permanent incremental power across branches | Upgrade cards feel interchangeable | Current investment, next marginal benefit and refund/reset consequence |
| Rankings / records | Ordered comparison and provenance | Similar tables can flatten first clear versus ordinary history | Metric, significance, player position and trustworthy result |
| Settings / Nobility | Preferences, account continuity and timed membership benefits | Utility tasks compete with a large benefit explanation | Current account/preference state and safe, explicit changes |

The reference images demonstrate that **identity can come from hierarchy and relationships as well as artwork**. A list of Guardians with a meaningful ascent order already has a stronger foundation than an unrelated illustration placed above the same summary cards. Evidence: screen files linked in section 6 and [game models][model-character], [Essence models][model-essence], [dungeon state][dto-dungeon].

## 13. Data Density and Disclosure Priorities

Frequency below is a task-based design hypothesis, not measured usage. “High” means needed repeatedly while performing that screen's main task; a rarely visited screen can still require high density during use. Critical information should remain visible at the relevant decision, even if the decision itself is infrequent.

| Area | High-frequency / immediate | Medium-frequency / secondary | Low-frequency / expandable or separate | Density judgment |
|---|---|---|---|---|
| Shell / return | Identity, current action, relevant resources, actionable error | Pinned objective, unread communication | Full offline breakdown, historical loot, account metadata | Compact persistent information; rich summary only when relevant |
| Character | Identity, level/rating and build summary | Grouped combat attributes and selected equipment | Detailed passive sources, other-player metadata | Moderate overview, dense inspection |
| Inventory / gear | Slot, selected item, name/rank/quality, comparison delta, eligibility/action | Set progress, sorting/filtering, favorite/new | Full roll/ability explanation, past source | High: adjacent comparison is a functional requirement |
| Stock / crates | Item/quantity, selected use and required choice | Category, favorites and transfer eligibility | Extended reward explanation | Moderate with precise use confirmation |
| Essence build | Active/passive slots, selected abilities, preset and save status | Level/Ascension requirements and auto-use activity | Full source/drop history, completed collections | High during build editing; collection browsing can be lighter |
| Combat Styles | Selected versus preview, operative conditions, save/discard | Mastery and next milestone | Full tuning explanations and other-style history | High mechanical clarity, limited simultaneous detail |
| Regions | Area/enemies, eligibility, active action and objective relevance | Drops/collection completion, adjacent activities | Lore, all distant locked content | Moderate comparison; not every area needs a large card |
| Dungeon preparation | Difficulty, readiness, entry cost and start/continue | Expected rewards/mastery | Historical records and full lore | Moderate, weighted toward commitment |
| Dungeon run | Current/available rooms, Vigor consequence, carried loot risk | Party condition and recent encounter | Full combat logs and previous room details | High localized decision density |
| Tower / raid preparation | Roster, assignment, readiness, deadline and captured build | Known boss mechanics and forecast | Old activity, detailed simulations/history | High; party comparison cannot be hidden behind separate decorative profiles |
| Combat / replay | Phase, living units, HP/barrier, decisive state and result | Contribution totals and selected-unit abilities | Per-event logs and full typed statistics | Layered: the full report need not be the first visual layer |
| Regional event | Scheduled/live state, participation, boss/party status | Revival and milestones | Recent event history | Phase-dependent rather than permanently maximum density |
| Arena / tournament | Opponent/team, stakes, readiness, time/round | Rating/Glory/record and next rewards | Full match analytics, prior seasons | Moderate selection, high tactical/team detail |
| Guild headquarters | Current shared objective, own role and actionable requests | Resources, member presence, target building | Permission matrix, disband/transfer controls | Member/officer task differences matter more than one universal dashboard |
| Guild Vault / buildings | Selected gear/ownership or current-to-next building benefit/cost | Availability/borrower or level path/target | Legacy rules and older activity | Dense comparison at selected object, lighter summary |
| Bazaar | Exact item, quantity, side, quote, fee/net and commitment state | Best bids/asks, owned orders, affordability | Historical medians/volume and old fills | High; trustworthy numbers outrank atmospheric whitespace |
| Quests / Prophecies | Objective/choice, progress, next action, expiry and claim state | Reward preview and chain/milestone position | Completed history and long descriptions | Moderate, with disclosure based on lifecycle |
| Achievements / Soulstones | Selected accomplishment/upgrade, eligibility, current/next effect | Category progress, rank and costs | Full completed chains and all bonus sources | Moderate; repeated summaries can be reduced without losing mechanics |
| Rankings / records | Rank/metric/name/value or result/date | Own position, paging and comparison | Full profile/replay details | High compact rows; maintain meaningful column alignment |
| Settings / account | Selected preference, binding state, action consequence | Nobility expiry/quantity when redeeming | Full perks, patch notes and rare account actions | Low–moderate; emphasize one utility task at a time |

**Disclosure boundaries:** never hide the total price/fee behind a tooltip, a guild donation's permanence behind an item popover, the build capture state behind a distant settings page, or pending dungeon loot loss behind generic help. Conversely, displaying every ability event, completed collection member and historical reward at once is not necessary for informed action. Small screens need a way back to the original selection and a retained decision summary, not just all desktop panels stacked vertically.

## 14. Existing Artwork and Art Dependency

### 14.1 Repository asset inventory

Counts describe files currently in `src/assets`, including original/optimized duplicates. Size is on-disk total, not page download weight. An asset's presence does not prove it is used or licensed for every future purpose.

| Asset group | Inventory and approximate size | Observed use / limitations |
|---|---|---|
| `backgrounds` | Five PNGs plus five WebPs; 11.91 MiB total. `background`, `colosseum`, `market-place`, `tavern`, `temple` | Global optimized background is used. No active app/style references to the other four named scenes were found in the inspected source. They are potential existing resources, not current geographic coverage. |
| Background dimensions | Main background 1440×1024; Colosseum/Tavern/Temple 1536×1024; Marketplace 1024×1024 | Optimized files are 512px wide, roughly 10–26 KB. They should not automatically be stretched into sharp full-screen focal art. |
| `cards` | Four PNGs and four WebPs; 4.30 MiB. `combatArea`, `mining`, `mining2`, `woodcutting` | Combat-area image is reused across combat-area cards. Gathering-related names do not imply working gathering screens. Combat source is 1440×1024, mining images 1024², woodcutting only 310²; its 512px optimized copy is upscaled. |
| `landing` | Login PNG and blur SVG; 1.86 MiB | Login provides a distinct illustrated entrance. It is not a library of character/creature portraits. |
| `portrait` | `Avatar.svg` and four ellipse/frame SVGs; 1.42 MiB | No active character-sheet portrait composition found. Avatar alone is about 1.17 MB; SVG format does not guarantee a lightweight asset. |
| `combat` | Victory, Defeat, CombatVS and healthBar SVGs; 5.63 MiB | No current active references found in inspected combat templates/styles. `healthBar.svg` alone is 5,824,100 bytes. Audit internals before proposing reuse. |
| `icons` | 53 files, about .15 MiB | Bespoke feature/sidebar symbols, currencies/rewards and small equipment imagery. Seven weapon/shield icons are around 64²; suitable for compact roles, not large item showcases. |
| `core` | 13 files, about .52 MiB, including a 544² texture and ornamental elements | Reusable visual material exists, but adding more of it does not solve repetitive grouping. |
| `entities` | No files | No ready catalog of unique creature/Essence/party full-body art in this directory. |
| `fonts` | Four Atkinson WOFF2 faces and OFL text, about .10 MiB | Local readability option is a dependable existing resource. Poppins/Marcellus and icon fonts are externally imported. |
| `help` | 26 JSON files, about .03 MiB | Content/guidance resource rather than illustration. Must remain synchronized with a redesigned flow. |

Four repository images were visually inspected: [main background][asset-background] is a candlelit arcane study with warm gold light; [Colosseum][asset-colosseum] depicts monumental golden ruins; [combat-area art][asset-area] is a teal woodland scene with robed figures; [login art][asset-login] is a cold, ominous architectural/skull scene. The descriptions of other asset groups above come from inventory, dimensions and references, not a claim that every image was visually inspected.

There is **no established production pipeline here for unique art for every equipment instance, Essence, creature, Guardian, region and player**. The active character data and sheet do not provide the complete full-body-art mechanism assumed by several references. Art provenance for the game's illustrations was not established in this audit. The attached references are not an asset pack to crop into the game.

### 14.2 Dependency tiers for future concepts

| Dependency | Techniques grounded in the references | LL applications | Maintenance implications |
|---|---|---|---|
| **Low** | R1/R9 typographic scale and dividers; R2 compact selection rows; R4 selected-item/stat relationship; R10 selected-node briefing; restrained frames and meaningful negative space | Character identity through name/title/build, inventory comparison, Tower order, quest chain, tournament rounds, order books | Primarily layout, type, CSS/SVG geometry and existing icons. Still requires states, responsive design and accessibility work. |
| **Medium** | R3/R4/R10 contextual backgrounds; R8 recurring patterned menu surfaces; reusable ornaments; a small authored set of feature illustrations | Selective reuse/improvement of existing scenes, region atmosphere, one consistent family of Guardian/feature emblems | A finite asset set can suit a solo developer. Plan crops, contrast zones and variants for different aspect ratios. A map illustration also needs a maintained relationship to actual content. |
| **High** | R1/R7/R9 full-body characters breaking frames; R5 art-directed hub; R6 unique collectible art; R10 bespoke geographic relief | Individual creature/Essence catalogs, custom character identity, unique boss scenes, animated environments | Continuous art demand as content grows; portrait/cutout variants, ownership/rights, resolution, fallbacks and loading behavior. A concept depending on every object having bespoke art is not yet supported by the repository. |

“Limited art” need not mean “same cards with a new color.” R1's alignment and type contrast, R2's selection continuity, R4's cause-and-effect grouping and R10's map/briefing relationship survive with much less illustration. Conversely, the emotional force of R5/R7 cannot be honestly promised using a single reused 512px background.

### 14.3 Pipeline facts designers should respect

The current [conversion script][config-image-pipeline] selects the `backgrounds` folder and uses Sharp to resize PNGs to 512px-wide WebP at quality 80. It is not a responsive-image or portrait-generation pipeline. The [Angular asset configuration][config-angular] copies the asset directory through two entries, supporting existing mixed `assets/...` and root-relative asset paths; apparent URL inconsistency should be investigated before being called a broken image. New focal illustrations will need intentional export sizes/crops and delivery budgets. The conversion script was not run and no assets were altered.

## 15. Technical and Interaction Constraints

### 15.1 Platform facts with design consequences

| Reality | What the designer needs to know |
|---|---|
| Angular application | Angular core is locked at 20.3.27; CDK/Material at 20.2.14. Standalone components, lazy feature routes and mixed Signals/RxJS state are present. There is no need to replace the framework to change composition. |
| Styling | Tailwind 3.4.19 in the lockfile, global CSS tokens/utilities and feature CSS/SCSS. Material is not the dominant visual template; it and CDK provide selected form/overlay/virtualization infrastructure. |
| Package management | npm with `package-lock.json` only. Future implementation should use the repository scripts and keep caches outside the checkout. This analysis installed nothing. |
| Shared layout | Persistent dashboard shell constrains height and width, with separate sidebar/chat preferences. A full-screen concept must explicitly account for these responsibilities even if their current placement changes. |
| Theme/readability | One authored dark visual system, plus readable/system font and size preferences. These are not complete alternate light/dark themes. A light reference-inspired direction would require a real semantic theme redesign and contrast validation. |
| Responsive support | Viewport breakpoints, container queries and mobile-specific layouts already exist. Large text, chat placement and long content change usable width. Desktop reference images are not mobile specifications. |
| Browser features | `100dvh`, CSS container queries, `color-mix`, `:has`, ResizeObserver and native dialogs appear in the implementation. No explicit minimum browser matrix was established here; do not promise compatibility beyond validated project targets. |
| Data boundaries | ASP.NET API contracts and server rules own progression, prices, eligibility, rewards, combat results and build capture. A cosmetic redesign should use those facts rather than duplicate calculations or imply unsupported interaction. |
| World data | Region catalog provides areas/requirements/entities; dungeon state provides revealed graph information. There is no established geographic coordinate model for an R10-style world relief. A new authored map is a separate content/data decision. |
| Identity data | Current character/Essence DTOs support names, stats, mechanics and progression; no complete full-body avatar/art assignment system was found for the proposed visual use. Large identity art needs an explicit product and asset strategy. |
| Build/runtime generation | Build scripts generate state-sync scopes and, for production build, a version artifact before compiling. Running them is not necessary to validate a Markdown audit and would create unrelated output. |

Evidence: [package manifest][config-package], [lockfile][config-lock], [Angular configuration][config-angular], [tokens][style-tokens], [shell][screen-shell], [character DTO][model-character], [Essence DTOs][model-essence], [region catalog][service-region], [dungeon state contract][dto-dungeon].

### 15.2 Realtime and state changes are part of the design

The game uses SignalR with reconnection/subscription management, domain-version coordination and API refresh/mutation state. Chat is a separate service boundary. Server time synchronization supports event deadlines and countdowns. Raid and regional-boss pages also combine polling with live state; the regional boss includes a live clock. Idle/offline combat resolves through server-controlled actions and summaries.

This has visible implications:

- **Loading, empty, locked, restricted, stale, failed and completed are different states.** A blank panel must not impersonate an empty collection; expired content must not look merely unselected.
- **Live updates must not destabilize an active decision.** A refreshed list should preserve selected item, filter, scroll and typed quantity where appropriate. Roster changes must be noticeable without moving the control the player is about to activate.
- **A countdown is informative, not an authority to grant an action.** The server decides whether sign-up, start or claim is allowed.
- **Builds have multiple meanings.** Current equipment/Essences, an unsaved Style preview, a loadout assigned to an activity and a captured raid/Tower/Arena/tournament build cannot be displayed as one interchangeable state.
- **Playback is not manual combat.** Skip/instant playback affects presentation; it is not a tactical ability or a different reward outcome. Viewing another page does not necessarily stop the action.
- **Retry is a first-class action.** Bootstrap, failed saves, unavailable graphs and pending Nobility redemption already have recovery behavior. Preserve it through new transitions and animation.

Evidence: [realtime connection][service-realtime], [bootstrap state][service-bootstrap], [time synchronization][service-time], [action state][service-actions], [raid page][state-raid], [regional boss][state-region-boss], [loadouts][component-loadouts], [Nobility][screen-nobility].

### 15.3 Accessibility constraints and existing strengths

The implementation has global visible focus and reduced-motion handling, local readable fonts, focus-trapped dialogs, restoration to origins, contextual help, keyboard-capable selection and pointer alternatives to drag assignment. The newer navigation tabs explicitly move focus during keyboard navigation. The older generic tabs update active selection without equivalent focus movement, so not all tab implementations are equally complete.

Designs inspired by thin reference text, skewed cards, low-contrast textured backgrounds and controller prompts must still support mouse, keyboard, touch and readable text scaling. Preserve logical DOM reading order even with asymmetric visual placement. Decorative artwork should not intercept actions; actionable text needs a stable contrast surface. Written status/rarity labels must accompany color, and critical facts must remain available without hover. Multiple nested scrollers, modal/popover stacking, mobile graph targets and long-name truncation require live validation before any future implementation is declared accessible. Evidence: [global styles][style-global], [dialog focus][directive-dialog], [tabs][state-tabs], [navigation tabs][state-nav-tabs], [party builder][component-raid-party], [help drawer][component-help].

## 16. What Must Not Be Lost

1. **Fast comparison rather than repeated navigation:** equipment rows, selected inspector, stat deltas, exact-item purchasing and buy/sell books let players make informed choices without memorizing numbers across screens.
2. **Honest costs and ownership:** reinforcement before/after, variant cost, guild-property restrictions, donation permanence, market fee/net, affordability and server restriction reasons.
3. **Protected inventory actions:** favorites/equipped items are excluded from inappropriate bulk salvage, and consequential operations have explicit review/confirmation.
4. **Loadout state and recovery:** autosave visibility, failed-save blocking/retry, locked-preset copy/recovery, activity assignment and captured-build updates.
5. **Gameplay-specific structures already present:** dungeon graph/Vigor/pending loot; Tower ascent/scouting; tournament rounds; Guild building path; Prophecy milestone track; market order books.
6. **Accurate outcome explanation:** detailed combat statistics, typed effects, summons, contributions and historical replay. These can be secondary without being removed.
7. **Progression guidance:** First Steps, pinned objective links, objective destinations and restriction/unlock reasons prevent new-player disorientation.
8. **Distinct reward lifecycles:** available/accepted/completed/claimable/claimed/opened, personal versus community thresholds, and safe versus carried dungeon rewards.
9. **Social continuity:** guild permissions, clear chat channel/recipient, mentions, item links, presence and profile access.
10. **Responsive alternatives:** container-aware inventory panes, mobile selected views, combat stacking, Tower switching and click alternatives to drag-and-drop.
11. **Accessibility and recovery infrastructure:** focus rings, readable fonts/sizes, reduced motion, dialog focus, help, bootstrap retry and persistent ongoing-action awareness.
12. **Trustworthy histories:** exact ranking metrics, own-rank context, first-clear provenance, old-data-unavailable states and retained lists during refresh errors.

These are reasons to preserve **behavior**, not reasons to preserve their current borders, fonts, colors or exact placement. Evidence: [inventory][screen-inventory], [upgrade panel][component-upgrade], [Vault][screen-guild-vault], [market][screen-market-commodity], [loadouts][component-loadouts], [dungeon][screen-dungeon-run], [tournament][screen-tournament], [Prophecies][screen-prophecies], [journey][journey], [dialog focus][directive-dialog].

## 17. Assumptions the redesign should challenge

These are questions for concept exploration, grounded in current limitations. They are not a proposed final layout.

1. Must all fifteen navigation destinations have equal persistent visibility after onboarding, even when a player is in a committed encounter or trade?
2. Must every feature occupy the same inset shell, fixed gutters and chat-constrained width?
3. Does every page need the same icon and modest heading before meaningful content starts?
4. Does each numerical summary deserve a bordered tile, especially when it rarely affects the next action?
5. Should Archive, absorption, creature discovery and Codex completion share one visual grammar merely because they share Essence data?
6. Can a player understand their complete build when equipment, Essence roles, Combat Style and captured activity builds are presented separately?
7. Must all battle contexts initially look like the same contribution report, even when their stakes and phases differ?
8. Can region identity be expressed more clearly with current names, enemies and progression relationships before commissioning a world of new illustrations?
9. Should the most important historical first clear be visually equivalent to an ordinary personal combat record?
10. Do all quests/rewards need the same framing when branching choice, community eligibility, daily commitment and passive achievement are different experiences?
11. Should Guild emphasize administration for ordinary members, or the current collective objective and participation?
12. Does Nobility need to be the first and largest Settings concern when guests often arrive to bind an account?
13. Should loot history be discoverable only through a particular desktop chat arrangement?
14. Are uniform grids justified by comparison, or merely convenient markup? Preserve the former and challenge the latter.
15. Is artwork a compositional participant or just a texture under dark panels? If focal art is unavailable, what hierarchy can typography and relationships provide?
16. Should item rarity be copied as a reference-style border when the current game already uses readable rarity names/colors and several other color semantics?
17. Can contextual actions feel specific to the game while retaining visible labels, keyboard operation, pending states and confirmation?
18. Which selections, filters and scroll positions should survive a profile inspection, build edit, refresh or Back action?

## 18. Reference → LegendsLegacy Translation Matrix

“Worth exploring” is an analysis judgment, not a design prescription. Art requirements distinguish a principle from a literal recreation.

| Reference principle | Why it works | Applicable LegendsLegacy systems | Risks | Art requirement | Worth exploring? |
|---|---|---|---|---|---|
| R1/R7/R9 oversized identity typography | Establishes one subject before detailed numbers | Character, selected Essence, Guardian, result | Long names/localization; level may not be the best focal metric | Low for type; high for accompanying cutout | Yes, choose the meaningful subject per screen |
| R1 strong type-weight/italic contrast | Creates hierarchy within one name/identity line | Character/title, boss/encounter headings | Illegibility or artificial name splitting; readable-font preference | Low | Yes, as hierarchy rather than a mandatory font recipe |
| R2 stable roster + selected dossier | Keeps collection context while inspecting one subject | Raid/Tower roster, guild members, Archive | LL needs many build facts; three columns can fail in shell width | Low; portraits optional/medium | Strong fit for selection continuity |
| R3 repeated party portrait frames | Equal frames imply comparable party members; shared baselines help reading | Cooperative party overview | LL does not have three owned RPG companions; important roles may need unequal detail | High if literal unique portraits | Explore the comparison principle, not the assumed party fiction |
| R4 adjacent inventory, stat delta and subject | Shows cause and effect in one composition | Gear, Essence/Style build preparation | Too much simultaneous detail on mobile | Low for relationship, high for full-body image | Strong fit |
| R1/R9 thin dividers and fewer enclosing boxes | Whitespace/alignment group facts without nested panels | Overview, records, quest detail, upgrade effects | Contrast and grouping can become ambiguous on artwork | Low | Strong fit where boundaries are redundant |
| R5 unequal feature tiles | Size/location communicate importance instead of a uniform grid | Activity selection or a future entry concept | Promotional overload; user task priority is not established by tile size alone | Medium–high | Explore hierarchy; do not import store/pass/pets/deck features |
| R6 object cards with consistent metadata zones | Makes a real collection scannable and comparable | Essence catalog, selected rewards, equipment with enough art | Cannot replace ability mechanics with grade/level shorthand | High for unique art; low for metadata discipline | Conditional; use cards for actual bounded objects |
| R7 selection ribbon plus large detail | Connects catalog membership to a focused subject | Adjacent Essence inspection, roster member detail | Tiny diamond targets and art dependency; no basis to add wardrobe actions | Medium–high | Conditional on navigation and asset needs |
| R8 broad selected-action emphasis | One choice is instantly distinguishable from peers | Contextual activity selection, quest branch | Overlarge controls reduce density; reference uses placeholder copy | Low–medium | Yes where few consequential choices exist |
| R9 text roster with restrained selection marker | Dense navigation lets identity art/detail dominate | Creature/Essence lists, cooperative rosters | Selection must remain keyboard-visible; unknown entities need useful text | Low | Strong fit even without art |
| R10 connected progression and selected briefing | Joins location/path with the reason to act | Dungeon revealed graph, Tower floor, quest chain | Must not expose unrevealed nodes or invent geographic facts | Low for abstract graph; high for relief map | Strong principle, conditional literal map |
| R1/R2/R8–R10 compact resource/top bars | Provides continuity without a large dashboard of currencies | Shell, event/market context | LL balances and action labels can be long; exact amounts matter in trade | Low | Yes, with context-specific precision |
| R3/R4/R10 contextual environments | Background supports the content's place and stakes | Regions, dungeon/Tower/raid context | Texture competes with small type; asset coverage/resolution limited | Medium–high | Conditional on a sustainable art budget |
| R1/R5/R7 overlapping artwork and UI planes | Subjects feel part of the screen rather than a thumbnail | Character, Guardian, special result | Occlusion, pointer interception, loading and mobile crops | High if character cutouts; low for limited geometric layering | Conditional; not required for every screen |
| R3/R6 aligned vitality/progression blocks | Repeated baselines support comparison | Party health, relevant build progression | Identical bars can imply equivalent units/stakes; LL has no assumed SP stat | Low | Yes with actual LL semantics |
| R3/R8/R10 distinctive frames and notches | Creates recurring shape grammar and selected-state identity | Key actions, selected items, encounter/mission status | Decorative complexity, focus outline clipping, inconsistent hit areas | Low–medium | Explore sparingly after hierarchy |
| All references contextual action prompts | Commands explain what can be done in the current selection | Inspectors, dungeon decision, raid preparation | Controller glyphs alone are inappropriate for browser/touch users | Low | Strong fit with explicit labels and accessible controls |
| R1/R2 versus R3–R7 versus R8–R10 different compositions | Layout follows dossier, collection, hub or campaign purpose | Distinct LL feature identities within each proposed direction | Incoherence if unrelated style fragments are assembled | Variable | Essential principle; keep each concept internally directed |

## 19. Redesign Opportunity Map

Categories concern the current **structure**, not how many CSS declarations might change. A page can have a radical composition opportunity and still contain interactions that should be preserved heavily.

| Category | Screens / scope | Reason and boundary |
|---|---|---|
| **A — Radical rethink recommended** | Authenticated shell and activity orientation | Permanent widths, layered headings and equal navigation priority shape every feature. Preserve access, communication and current-action responsibilities, not necessarily their framing. |
| **A** | Character/build identity across Overview, equipment, Essences and Styles | The relationship between build causes and the actual activity build is not expressed in one coherent mental model. Radical exploration should not imply changing mechanics or merging every editor. |
| **A** | Region/activity selection | Uniform area cards and shared art flatten actual locations and objectives. Explore spatial/progression identity using existing data; geography/art is a separate decision. |
| **A** | Combat's primary presentation across modes | A shared analytical viewer is valuable, but an encounter's initial presentation can communicate phase/stakes more clearly. Preserve statistics/playback beneath any new composition. |
| **B — Layout rethink recommended** | Essence Archive, Absorb, Creatures and Codex | Strong data and filters; acquisition, collection, progression and loadouts are visually entangled. |
| **B** | Achievements/titles and Soulstone Archive | Repeated summary/upgrade cards underplay identity and marginal decision value. Preserve progress, costs and title behavior. |
| **B** | Dungeon preview, mastery and records | Readiness, entry costs, expected rewards and history need a clearer priority; mechanics are already sound. |
| **B** | Tower overview, expedition preparation, Hall of Fame | Ascent/scouting/rosters are useful; Guardian and first-clear significance need stronger hierarchy and relationships. |
| **B** | Raid preview/muster/plan/playback/results and regional boss lifecycle | Role/phase/participation relationships are more important than equal report panels. Preserve administration, capture rules and uncertainty. |
| **B** | Colosseum Arena, tournament management and battle results | Important stakes compete with summary metrics and management/history. Keep opponent comparison and bracket logic. |
| **B** | Guild discovery/headquarters/missions | Collective identity and objective are weak relative to administrative panels. Preserve roles and personal-versus-shared eligibility. |
| **B** | Quest Journal/community events and Prophecies | Different lifecycle stages and next actions compete; branch choice and claims must remain exact. |
| **B** | Settings/Nobility and returning-player summary | Utility/account tasks and membership information have uneven prominence; return needs orientation as well as a report. |
| **B** | Chat/loot placement and notification hierarchy | Useful features compete for permanent screen space and attention. Preserve messaging behavior and recipients. |
| **C — Visual-system redesign mainly** | Login/signup and access/error states | Clear core purpose; improve identity/control consistency without inventing account capabilities. |
| **C** | Inventory catalog/stock/inspector and loadout controls | Specialized, responsive comparison mostly works. Integrate with the wider build concept without discarding list efficiency. |
| **C** | Bazaar commodities, exact listings, selling/orders | Dense transaction structure is justified. Improve hierarchy, continuity and visual identity without hiding prices or escrow. |
| **C** | Guild Vault/building detail, Guild and Champion shops | Ownership, current-to-next upgrades and purchase-aside patterns largely fit their tasks. |
| **C** | Global/local rankings, personal expedition history and routine records | Tables support genuine comparison. Improve recognition, mobile access and navigation overlap; first clears warrant stronger treatment. |
| **D — Preserve heavily** | Dungeon revealed graph, Vigor risk and pending/secured/lost reward logic | Already distinctive gameplay structure with meaningful decisions. Restyling must not turn it into an invented full-map reveal. |
| **D** | Equipment comparison/upgrade quote, protected bulk actions and transaction review | Accurate before/after and consequences are more valuable than superficial simplification. |
| **D** | Tournament round relationships, party click/drag assignment and explicit build updates | Strong task-specific interactions; their framing may change. |
| **D** | Focus, readable fonts/sizes, reduced motion, help/quest shortcuts, retry/recovery | Functional quality to preserve across all visual directions. |

## 20. Open Questions, Evidence Index and Verification

### 20.1 Questions for the design brief, not blockers to this audit

1. What is the first emotional promise: an individual adventurer's identity, creature/Essence discovery, cooperative progression, or mastery of builds? The code supports all of them, but they cannot all be the largest focal element.
2. What are the actual session patterns: desktop multitasking with docked chat, phone check-ins, long build sessions, or spectating cooperative battles? Telemetry/interviews should determine persistent density.
3. How much new art can be sustained per content release? Is a small fixed environment set realistic, or is a maintained portrait/creature library funded?
4. Should player identity have authored full-body artwork, selectable portraits, or primarily typography/titles/build identity? That product choice is not answered by the current DTOs.
5. Which long-term systems should concepts show if the current focused-beta experience and production raid flag differ? Show feature availability explicitly rather than assuming every player sees everything.
6. What information do experienced players use to evaluate builds beyond Combat Rating? This determines which comparisons must stay visible.
7. How prominent should live combat be relative to its analysis and the next preparation decision, given automatic combat?
8. Which selections and views should be linkable/shareable or restored through Back/refresh? The current implementation is inconsistent by feature.
9. How should high-value/irreversible decisions be reviewed without forcing routine actions through excessive confirmation?
10. What minimum devices, browser targets, text sizes and accessibility acceptance criteria will future implementation validate?
11. Which existing illustrations have confirmed ownership/license and suitable source resolution? Reference images alone do not answer that.
12. Which three or four representative screens must prove each direction? At minimum include identity/build, a dense transaction or inventory, a progression/encounter view and small-screen behavior; a beautiful hub alone cannot establish viability.

### 20.2 Evidence index

Links throughout this document point to concrete templates, styles, services, DTOs and domain/API boundaries. These groups give a designer or implementer a practical starting point without requiring the designer to inspect code to understand the game.

| Evidence group | Primary files / symbols |
|---|---|
| Routing/access | [app.routes.ts][route-app], [dashboard.routes.ts][route-dashboard], [character.routes.ts][route-character], [world.routes.ts][route-world], [city.routes.ts][route-city], [SidebarService][service-sidebar], [player-journey rules][journey], [focused-beta guards][guard-beta], [environment flags][config-environment] |
| Shell/continuity | [Dashboard template][screen-shell], [Header][screen-header], [Sidebar][screen-sidebar], [QuestTracker][screen-tracker], [Chat][screen-chat], [LootTracker][component-loot], [SessionSummaryPopup][component-session] |
| Build and collections | [CharacterOverview][screen-overview], [Inventory][screen-inventory], [EquipmentLoadouts][component-loadouts], [EquipmentUpgradePanel][component-upgrade], [Essences][screen-essences], [EssenceStateService][state-essence], [CombatStyles][screen-styles], [Achievements][screen-achievements], [SoulstoneArchive][screen-soulstones] |
| PvE and cooperative | [RegionService][service-region], [Region][screen-region], [DungeonPage][screen-dungeon-run], [Combat][component-combat], [TowerOverview][screen-tower], [TowerRally][screen-expedition], [RaidPage][screen-raid], [RaidPartyBuilder][component-raid-party], [RegionBoss][screen-region-boss] |
| City/social/economy | [Colosseum][screen-colosseum], [TournamentGrounds][screen-tournament], [InAGuild][screen-in-guild], [GuildVault][screen-guild-vault], [GuildBuildings][screen-guild-buildings], [MarketPlace][screen-market], [Commodity exchange][screen-market-commodity], [Tavern/Leaderboard][screen-leaderboard] |
| Objectives/account | [QuestJournalPage][screen-quests], [PropheciesPage][screen-prophecies], [Settings][screen-settings], [NobilityPanel][screen-nobility] |
| Contracts/rules | [Character DTO][model-character], [Essence models][model-essence], [EssenceProgressionConstants][rule-essence], [CombatStyleRules][rule-style], [DungeonRunStateDto][dto-dungeon], [EquipmentController][api-equipment], [RaidController][api-raid], [WorldTowerController][api-tower] |
| Visual/behavior primitives | [tokens.css][style-tokens], [styles.css][style-global], [tailwind.config.js][config-tailwind], [Popover][component-popover], [DialogFocusDirective][directive-dialog], [Tabs][state-tabs], [NavigationTabs][state-nav-tabs], [HelpDrawer][component-help] |
| Platform/assets | [package.json][config-package], [package-lock.json][config-lock], [angular.json][config-angular], [index.html][config-index], [convert-images.mjs][config-image-pipeline], [realtime connection][service-realtime], [bootstrap state][service-bootstrap], [time sync][service-time] |

The source classes and template structures support the audit's factual claims. Interpretations such as weak hierarchy, likely context loss and information priority remain design judgments. There is no claim that the reference screenshots prove motion, focus, selection behavior, platform or a particular font family.

### 20.3 Verification and change scope

Only `UI_REWORK_ANALYSIS.md` was created for this task. No Angular component, stylesheet, asset, dependency, backend file, configuration or migration was changed. Existing unrelated BalanceHarness changes were left untouched. No service was deployed and no game state, shared database or external environment was modified.

Verification completed: route/entry-point cross-checks; reproduction of template, component, asset and locked-version counts; 138 defined references with no missing local targets; ten individual reference-image analyses; 45 screen groups; complete inventory fields for the 44 active screen groups; valid contents anchors; and one self-contained handoff. A read-only Node check found no trailing whitespace or unbalanced code fences. `git diff --no-index --check -- /dev/null UI_REWORK_ANALYSIS.md` produced no whitespace diagnostics (the new-file comparison returns difference status). The working-tree comparison showed only this document added by the task.

Application build, browser gameplay tests and backend tests were not run: this is a documentation-only, source-based analysis, and a build generates unrelated artifacts. Consequently this document does not claim runtime UI validation. A later implementation should run the repository's relevant frontend checks and validate representative states at desktop/mobile widths; backend changes, if separately required, must use `build/run-tests.ps1`.

[api-actions]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/CharacterActionsController.cs>
[api-dungeon]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/DungeonController.cs>
[api-equipment]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/EquipmentController.cs>
[api-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/EssenceController.cs>
[api-raid]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/RaidController.cs>
[api-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/WorldTowerController.cs>
[asset-area]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/assets/cards/combatArea.png>
[asset-background]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/assets/backgrounds/background.png>
[asset-colosseum]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/assets/backgrounds/colosseum.png>
[asset-login]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/assets/landing/loginbackground.png>
[component-area]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat-area-card/combat-area-card.component.html>
[component-button]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/buttons/regular-button/regular-button.component.html>
[component-character-tag]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/character/character-tag/character-tag.component.html>
[component-combat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat.component.html>
[component-combat-stats]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat-entity-stats/combat-entity-stats.component.html>
[component-dungeon]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/dungeons/dungeon-card/dungeon-card.component.html>
[component-essence-description]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/essences/essence-description/essence-description-formatter.ts>
[component-feature-icon]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/feature-icon/feature-icon.component.html>
[component-header]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/default-header/default-header.component.html>
[component-help]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/help/help-drawer.component.ts>
[component-item]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/item/item.component.html>
[component-item-modal]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/modal-container/item-modals/inventory-item-modal/inventory-item-modal.component.html>
[component-loadouts]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment-loadouts/equipment-loadouts.component.html>
[component-login-button]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/buttons/button/button.component.html>
[component-loot]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/loot-tracker/loot-tracker.component.html>
[component-modal]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/modal-container/modal-container.component.html>
[component-nav-tabs]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component.html>
[component-popover]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/popover/popover.component.ts>
[component-raid-party]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/party-builder/raid-party-builder.component.html>
[component-session]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/session-summary-popup/session-summary-popup.component.html>
[component-soulstone]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-upgrade-card/soulstone-upgrade-card.component.html>
[component-tabs]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/tabs.component.html>
[component-toast]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/toast/toast.component.html>
[component-transfer]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/inventory-transfer/inventory-transfer.component.html>
[component-upgrade]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-upgrade-panel/equipment-upgrade-panel.component.html>
[config-angular]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/angular.json>
[config-environment]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/environments/environment.ts>
[config-image-pipeline]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/convert-images.mjs>
[config-index]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/index.html>
[config-lock]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/package-lock.json>
[config-package]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/package.json>
[config-tailwind]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/tailwind.config.js>
[directive-dialog]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/directives/dialog-focus/dialog-focus.directive.ts>
[dto-dungeon]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Dungeons/Dtos/DungeonRunStateDto.cs>
[guard-beta]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/guards/focused-beta-journey.guard.ts>
[journey]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/player-journey/player-journey.ts>
[model-character]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/Dtos/characterDto.ts>
[model-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/essence-system.ts>
[ref1]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-8639ece8-2de7-4788-a2b5-a0afecb6570a.png>
[ref10]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-40694642-7f13-4c08-b1ed-cdf4a743aa0d.png>
[ref2]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-88be0719-24e7-4e96-b240-8b646accf66b.png>
[ref3]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-96865f73-b35f-444e-93e9-cc0eedd4373d.png>
[ref4]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-7acda489-b5e8-48e3-8137-e6916df6f570.png>
[ref5]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-792a9a91-6f48-4a2c-9b27-b8168137f35f.png>
[ref6]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-0debafff-3d84-4751-affd-6920b36a424e.png>
[ref7]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-f1f1cb9e-6ed9-4c0e-b061-6a91eb8f94d8.png>
[ref8]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-4b559139-defa-4e16-a869-fcc9d6c73788.png>
[ref9]: <C:/Users/HrHoe/AppData/Local/Temp/codex-clipboard-df013757-1d6f-4012-bf66-3c66413d027b.png>
[route-app]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/app.routes.ts>
[route-character]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character.routes.ts>
[route-city]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/city.routes.ts>
[route-dashboard]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.routes.ts>
[route-public]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/public/landing/landing.routes.ts>
[route-world]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/world.routes.ts>
[rule-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceProgressionConstants.cs>
[rule-style]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs>
[screen-absorb]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/essences/essences-absorb/essences-absorb.component.html>
[screen-achievements]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/achievements/achievements.component.html>
[screen-arena]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/arena-battle/arena-battle.component.html>
[screen-arena-rankings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/rankings-glory/rankings-glory.component.html>
[screen-arena-records]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/record-of-battle/record-of-battle.component.html>
[screen-champion-market]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/champions-market/champions-market.component.html>
[screen-chat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.html>
[screen-colosseum]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/colosseum.component.html>
[screen-dungeon-run]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.html>
[screen-dungeons]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeons.component.html>
[screen-essences]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/essences/essences.component.html>
[screen-expedition]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/rally/tower-rally.component.html>
[screen-guild-buildings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/guild-buildings.component.html>
[screen-guild-info]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-info/guild-info.component.html>
[screen-guild-missions]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-missions/guild-missions.component.html>
[screen-guild-rankings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-rankings/guild-rankings.component.html>
[screen-guild-shop]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-shop/guild-shop.component.html>
[screen-guild-vault]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-vault/guild-vault.component.html>
[screen-hall]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/hall-of-fame/tower-hall-of-fame.component.html>
[screen-header]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/game-header/game-header.component.html>
[screen-in-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/in-a-guild.component.html>
[screen-inventory]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.html>
[screen-leaderboard]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/tavern/tavern.component.html>
[screen-login]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/public/landing/login/login.component.html>
[screen-market]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place.component.html>
[screen-market-buy]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-buy/market-place-buy.component.html>
[screen-market-commodity]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-commodity/market-place-commodity.component.html>
[screen-market-orders]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-orders/market-place-orders.component.html>
[screen-market-sell]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place-sell/market-place-sell.component.html>
[screen-no-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/no-guild/no-guild.component.html>
[screen-nobility]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/settings/nobility-panel.component.html>
[screen-overview]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.html>
[screen-personal-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/personal-expeditions/tower-personal-expeditions.component.html>
[screen-prophecies]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/prophecies/prophecies-page.component.html>
[screen-public-guild]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/public-guild/public-guild.component.html>
[screen-quests]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.html>
[screen-raid]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/raid-page.component.html>
[screen-raid-playback]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/playback/raid-playback.component.html>
[screen-raids]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/raids/raids.component.html>
[screen-region]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/region.component.html>
[screen-region-boss]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region-boss/region-boss.component.html>
[screen-settings]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html>
[screen-shell]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.component.html>
[screen-sidebar]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/sidebar/sidebar.component.html>
[screen-signup]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/public/landing/signup/signup.component.html>
[screen-soulstones]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-archive.component.html>
[screen-styles]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/combat-styles/combat-styles.component.html>
[screen-tournament]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-grounds/tournament-grounds.component.html>
[screen-tournament-replay]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/colosseum/tournament-replay/tournament-replay.component.html>
[screen-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.html>
[screen-tracker]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/quest-tracker/quest-tracker.component.html>
[service-actions]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/character-actions/character-actions.state.service.ts>
[service-bootstrap]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/game-bootstrap/game-bootstrap-state.service.ts>
[service-realtime]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-connection.service.ts>
[service-region]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/region/region.service.ts>
[service-region-boss]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/region-boss/region-boss.service.ts>
[service-sidebar]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/sidebar/sidebar.service.ts>
[service-time]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/time-sync/time-sync.service.ts>
[state-essence]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/essences/essence-state.service.ts>
[state-market]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/market-place/market-place.component.ts>
[state-nav-tabs]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component.ts>
[state-raid]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/raid/raid-page.component.ts>
[state-region]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/region.component.ts>
[state-region-boss]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region-boss/region-boss.component.ts>
[state-tabs]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/custom-components/tabs/tabs.component.ts>
[style-chat]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/chat/chat.component.scss>
[style-global]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/styles.css>
[style-inventory]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.scss>
[style-overview]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.scss>
[style-region]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/region.component.scss>
[style-tokens]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/styles/tokens.css>
[style-tower]: <C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/tower/overview/tower-overview.component.scss>

# CLAUDE UI REDESIGN HANDOFF

The following section is self-contained and can be copied with the ten reference images. Your task is to propose multiple original UI directions for LegendsLegacy. Do not treat the existing frontend as a layout that merely needs new colors, and do not copy one reference or collapse all ten into one blended aesthetic. This brief describes the inspected repository as of 13 September 2026; proposed visual directions must preserve actual mechanics unless a product change is explicitly identified.

## What LegendsLegacy is

LegendsLegacy is a persistent browser RPG. Players repeatedly fight through automatic combat, collect loot and creature Essences, improve equipment and permanent upgrades, assemble active/passive Essence loadouts and Combat Styles, and progress through regions, dungeons, competitive and cooperative systems. Most player decisions occur before combat, between encounters, during dungeon route selection, or when collecting and reinvesting rewards. It is not a manual-action combat game or a three-companion party RPG merely because some reference images depict those interfaces.

Character equipment has eight slots and individually meaningful rank/quality/variant/set information. Essences are collected from creatures, absorbed, assigned as active/passive abilities, leveled and Ascended; the Archive also contains creature discovery/focus and Codex collections. Combat Styles such as Bastion, Conduit, Duelist and Reaper change build behavior through rules and mastery choices. A build's effectiveness cannot be represented completely by one Combat Rating number.

## What the frontend needs to accomplish

Help a newcomer learn fight → acquire → absorb/attune → equip → progress; let a returning player understand offline gains and the action still running; make build changes and comparisons efficient; explain activity readiness and consequences; communicate automatic battles and their outcomes; support cooperative rosters and guild participation; handle trading accurately; and make progression/claim states clear.

The interface must support both high-density desktop sessions and phone use. Navigation/access changes with onboarding and progression. Guests can start playing, but account binding gates some economic/social functions. Raids have a feature flag, so do not assume every player always sees all systems.

## Current frontend problems

The strongest problem is repeated composition: modest gold title, muted description, dark bordered panel, summary tiles and nested cards across unrelated systems. Achievements, Soulstones and Essence Codex are especially similar despite different player goals. The shell, headings, local tabs, chat and inspectors compete for usable width. Gold serves too many roles: identity, selection, resource, progress and primary action.

Character/creature identity is mainly names and numbers. Regions reuse one combat-area illustration. A global illustrated background is largely covered by dark surfaces. Equipment, Essence roles, Combat Style choices and submitted activity builds are separated, obscuring which build will actually fight. Combat is mostly presented as an analytical viewer across many encounter contexts.

Do not overstate the criticism: radii are generally small; gradients/glows/blur are not universally excessive; custom icons and design tokens exist; this is not a stock Material dashboard. Inventory comparison, market order books, dungeon graphs, Tower ascent, building paths and tournament rounds are already specialized. No claim is made about whether code was generated by AI.

## Current strengths to preserve

Keep sortable/filterable compact lists, selected-item inspection and comparison deltas, exact cost/fee/net previews, ownership restrictions, protected bulk actions, save/pending/error/retry feedback, activity loadouts and explicit captured-build updates. Keep detailed combat analysis and replay, but its visual priority may change. Keep dungeon revealed routes, Vigor risk and pending/secured/lost loot distinctions. Keep objective shortcuts, staged onboarding, clear deadlines and personal-versus-shared reward eligibility.

Retain keyboard focus, readable font/size preferences, reduced-motion behavior, dialog focus trapping/restoration, touch-friendly alternatives to drag assignment, social recipient clarity, and mobile list/detail navigation. A beautiful redesign that makes players memorize comparison numbers across pages or hides irreversible consequences is a regression.

## Major screens and their actual purposes

- **Entry/return:** login, signup, guest entry, First Steps, offline/session summary, bootstrap/recovery.
- **Character/build:** self/other overview; equipment inventory, stock/containers, loadouts, reinforcement/restyling; Essence Archive/Absorb/Creatures/Codex; Combat Style preview/save; Achievements/titles; Soulstone permanent upgrades.
- **World:** Shenic/Meran area selection; dungeon briefing/difficulty/entry items/mastery/records; revealed dungeon route with Vigor and carried loot; automatic combat and analysis.
- **Cooperative:** World Tower floor/Guardian/scouting, expedition roster, histories and first-clear Hall of Fame; raids with three different preliminary parties followed by a combined Final Assault; scheduled regional boss with sign-up/live/revival/milestone states.
- **Competitive:** Arena opponent choice, tickets/rating/defense snapshots, battle records; weekly three-player tournament teams, registration, elimination rounds, replay and rewards; Champion Market.
- **Guild:** discovery/invitations/public profiles, member headquarters/roles, Vault borrow/return/permanent donation, building upgrades, weekly missions/contributions, Favor shop and rankings.
- **Economy:** Cinder Bazaar commodity order books, individual equipment listings/comparison, selling, own orders/escrow/cancellation/history. This is a dense exchange, not a decorative shop shelf.
- **Progress/social/utilities:** Quest Journal and community events, daily/weekly Prophecies/Favor/caches, global rankings, chat/mentions/item links/loot, help, Settings/account binding and Nobility Signet redemption. Direct membership purchase is currently unavailable.

There is no active companion-deck, pet, wardrobe, faction-management, manual-save, gathering or crafting workflow established by this audit. Do not import such systems from reference labels or dormant asset names.

## Core journeys and their weak connections

1. Enter/return → know current state → follow an objective or choose an eligible activity.
2. Fight → acquire item/Essence → compare/absorb → equip/attune/develop → return with an improved build.
3. Prepare gear + Essence loadout + Style → understand the activity's assigned/captured build → join/challenge/start. Current, preview and submitted builds are different states.
4. Dungeon briefing → pay/assemble entry requirement → choose revealed route → manage Vigor and carried loot → retreat/complete/fail → secure or lose rewards.
5. Tower/raid recruitment → party roles and readiness → captured build update → encounter → contribution/outcome → claim/history.
6. Arena/tournament team readiness → opponent/match stakes → battle/result → rank/reward.
7. Guild membership → shared objective or building → contribution → personal/shared rewards; Vault donation and borrowing have distinct ownership consequences.
8. Bazaar selection → price/quantity/fee/net → immediate trade or outstanding order → manage commitment/history.
9. Quest/Prophecy/achievement progress → eligibility → claim/open/spend/equip title. Completion is not always the same as claim, and a community milestone is not automatic personal entitlement.

Preserve selection and return context when users inspect profiles, edit builds or switch between catalog and activity. The current menu reflects feature boundaries more clearly than these cross-feature goals.

## Information-density requirements

Inventory, Essence build editing, roster preparation, market transactions, rankings and detailed combat inspection need real density and adjacent comparisons. Do not hide all numbers in tooltips to reproduce the references' spacious character screens. Keep the chosen item's difference, transaction total, ownership consequence, activity readiness and immediate risk visible at the decision.

Other information can be secondary: complete ability logs, old records, finished collections, full perk text, administrative permissions and long lore. Screen density should depend on task and lifecycle. A live dungeon, quiet scheduled event, completed quest and selected market order have different priorities. Large typography should emphasize the meaningful subject or outcome, not make every metric equally loud.

## Important technical constraints

The client is Angular 20.3.27 with CDK/Material 20.2.14, Tailwind 3.4.19, global tokens and local CSS/SCSS. It uses npm only. Shared behavior can remain while presentation changes; there is no need to replace Angular. Persistent sidebar/chat and nested scrollers currently constrain space, but their exact geometry is open to design exploration. Container-aware and mobile-specific views exist and must be tested with long content and larger reading text.

The server owns combat, costs, eligibility, rewards and snapshots. SignalR/polling and server-synchronized deadlines change lists, rosters and events while the user is interacting. Preserve loading/empty/locked/restricted/stale/error distinctions and retry states. Playback speed/skip is presentation, not manual gameplay. The region catalog is not a geographic coordinate map; dungeon data intentionally hides unrevealed route information.

Available default fonts are Poppins and Marcellus, with locally bundled Atkinson Hyperlegible and system-font reading options. A new font or light theme can be proposed, but it requires an explicit implementation/accessibility plan. Controller glyphs in the references are not sufficient labels for a browser/touch interface.

## Existing art and realistic scope

The repository has five environment backgrounds with 512px optimized WebP copies; one combat-area illustration reused across locations; several dormant mining/woodcutting images; login art; bespoke icons, small weapon/reward images, texture/ornaments; a single Avatar SVG with frames; and legacy combat SVG graphics. Several SVGs are unexpectedly large. There is no ready full-body character/Guardian/creature/Essence art library. Illustration rights/source resolution need confirmation before reuse is promised.

Low-art directions can use typography, unequal hierarchy, compact rosters, deliberate dividers, meaningful geometry, existing icons and the actual dungeon/Tower/tournament relationships. Medium-art directions can use a finite set of environments or feature illustrations. Literal R1/R5/R7/R9 character compositions and an R6-style unique collectible catalog have high ongoing art requirements. Show this difference honestly for a solo developer. Reference images are inspiration, not assets to crop and ship.

## Redesign freedom and things not to preserve by habit

You may radically question the shell, persistent navigation prominence, repeated headers, equal margins, page boundaries, card/panel nesting, shared composition across combat modes and the relationship between character/build editors. You may reorganize stories, records, vendor tabs, progression summaries and claims while maintaining discoverability.

Do not preserve identical frames for unrelated systems, four/seven equal metric tiles, the same area image everywhere, generic title–description–grid openings, a giant Settings benefits block, or loot's dependence on docked chat simply because they already exist. Do not assume behavioral reuse requires visual sameness. Conversely, do not replace effective lists/order books/graphs just to eliminate rectangles.

## Reference principles to explore in distinct directions

Use the attachment order as R1–R10:

- **R1:** clean editorial character dossier; large italic/weight-contrasted identity, dominant cutout, thin dividers and asymmetric information. Hierarchy is transferable; the full-body art is expensive.
- **R2:** light tactical personnel interface; dense roster remains visible beside selected character and structured statistics. Selection continuity is valuable for LL cooperative rosters and collections; worker/schedule mechanics are not LL features.
- **R3–R4:** dark landscape/map texture, framed party portraits, then a dense inventory/character composition. R4's selected item → stat change → character relationship is particularly useful. Do not assume LL has a three-character owned party.
- **R5–R7:** illustrated fantasy hub and collectible/selected-character system. Unequal hub tiles, stable metadata regions, clear selected subject and overlapping artwork have different uses. Avoid importing pets, reward passes, cosmetic shops or outfit changes. Literal art coverage is high-cost.
- **R8–R10:** light patterned menu/character/campaign family. Shallow top navigation, restrained text roster, large identity and selected mission briefing tied to connected map nodes. Explore progression relationships without revealing hidden dungeon nodes or inventing a geographic world model.

Across these families, typography shapes composition, selection is obvious, one subject has priority, artwork participates in layout, and commands relate to the current task. They differ fundamentally in light/dark field, serif/sans emphasis, use of cutouts versus framed portraits, density and the role of environment. Preserve those differences when proposing multiple concepts instead of combining all decorative details.

## Important unanswered design questions

Which fantasy should lead: individual identity, discovery, cooperative ascent or build mastery? What are actual desktop/phone session patterns? How much bespoke art is sustainable? What character identity representation should exist? Which advanced systems should concepts include under current feature flags? Which metrics do experienced players truly compare? What should persist across Back/refresh/profile inspection? How prominent should spectacle be in automatic combat? What accessibility/device targets will prove the concepts?

For each proposed direction, explain its organizing idea, screen-specific hierarchy, behavior preserved, art/implementation cost and density tradeoffs. Demonstrate it on representative identity/build, dense comparison/transaction, progression/encounter and small-screen cases. Leave room for genuinely different original solutions; this handoff does not choose one final aesthetic.
