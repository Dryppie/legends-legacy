# Text-profile generation prompts

Mode: built-in `image_gen`. The bottom-chat image edits the previous dark-shell bottom-chat concept; the right-chat image edits that new text-only result. No CLI fallback was used.

## Bottom chat: remove artwork and show all attributes

```text
Use case: ui-mockup. EDIT the supplied LEGENDSLEGACY bottom-chat desktop UI concept to REMOVE THE PLAYER PORTRAIT COMPLETELY and make ALL ESSENCES TEXT-ONLY, and use the freed space to show ALL 19 actual game attributes. Produce ONE refined high fidelity full-bleed wide 16:9 browser-game screenshot, no device, browser chrome, collage or annotations.

CRITICAL USER REQUIREMENTS: No human character, no body, no face, no profile picture, no silhouette, no ghost figure, no player avatar placeholder anywhere. No creature portrait, wolf head, animal drawing, Essence thumbnail, Essence icon, Essence painting or collectible card art anywhere. Remove the big ruin/landscape illustration too. The entire main canvas becomes quiet charcoal texture with useful information. Do not leave a blank portrait-shaped gap. Small existing SIDEBAR icons, currency icons and EIGHT EQUIPMENT icons can remain; the user did not ask to remove these.

PRESERVE THE ACCEPTED SHELL AND STYLE: identical full-height LEFT SIDEBAR (13% width), same exact navigation group hierarchy and items as input, selected "Overview", logo and "Wolfsbane Reach" idle action with View combat and Stop. Header from sidebar edge to right edge: "Aelric • Lv. 48", "Cinders 24,860", "Soulstones 318", bell/help. Keep EXPANDED BOTTOM CHAT occupying bottom 23% of screen to the RIGHT of sidebar, never covering sidebar or gameplay; identical Global/Guild/Loot tabs, 128 online, four timestamped fictional messages and composer. Preserve its realistic compact text, no message bubbles. No extra chat column on right.

THEME EXACTLY AS EXISTING GAME: charcoal #0e0f14 and #17171e, subtly grainy dark texture, elevated dark gray #292830 where needed, warm pale gold #f9dca0 / #fcd587 for headings, active states and one primary action, ivory #f6f0df text, gray #c3bec4 labels, fine muted gray dividers. Teal only for a saved-state tick. Restrained Marcellus-style serif headings and clearly sans-serif Poppins-style body, navigation, values and chat. No cream surfaces, purple gradients, gold-plated ornament, huge marketing headline, generic SaaS metric tiles or cards nested inside cards. Typography, alignment and useful information supply the game identity.

REBUILD MAIN WORKSPACE ABOVE CHAT, preserving ample readable type. A compact 85px horizontal identity band spans gameplay: small "Character overview", restrained gold serif "AELRIC", "Level 48 • The Ashen Covenant", a short slim "Combat XP • 68% to level 49" gold bar, and a separately labeled large "Combat Rating 12,840". Add a compact "Health 8,240 / 8,240" bar. Do not waste area on an oversized name.

Beneath identity, LEFT 67% of game content is "Combat Attributes", subtitle "After equipment and permanent bonuses". An elegant 2-by-2 arrangement of attribute groups uses fine dividers and modest gold section headings, not 19 separate metric tiles. OFFENSE top left, DEFENSE top right, RECOVERY lower left, UTILITY lower right. EXACTLY the following 19 label/value rows must be rendered ONCE EACH and visibly fit without scrolling, overlap, truncation or any missing row. Each group has left-aligned labels, right-aligned tabular values, thin faint horizontal row rules. Longer labels get enough width; prioritize correctness and legibility.

OFFENSE (6 rows):
"Power" "1,284"
"Attack Speed" "32%"
"Crit Chance" "24.8%"
"Crit Damage" "75%"
"Armor Penetration" "12%"
"Magic Penetration" "6%"

DEFENSE (6 rows):
"Max Health" "8,240"
"Physical Damage Reduction" "38%"
"Magical Damage Reduction" "31%"
"Dodge" "8%"
"Block" "16%"
"Damage Reduction" "5%"

RECOVERY (3 rows):
"Healing Power" "15%"
"Health Regen" "84 HP/5s"
"Life Steal" "4%"

UTILITY (4 rows):
"Cooldown Reduction" "12%"
"Status Resistance" "20%"
"Crowd Control Resistance" "15%"
"Threat" "184.6 threat/s"

These labels and units come from the real game. In particular Attack Speed is a percentage; Physical/Magical Damage Reduction are percentage values, not generic Defense or armor points; Health Regen is HP/5s. Values are illustrative sample data. Small note under Utility "Threat estimated from attuned Essences."

RIGHT 33% of gameplay is a clear build sidebar WITHIN THE GAME AREA, not chat. Slim divider from attributes. "CURRENT BUILD", dropdown "Wolfsbane Hunt" and compact "Equipped" tick. Text-only Combat Style summary "COMBAT STYLE" then "Conduit" in moderate gold serif, with short factual line "First occupied Essence slot powers Conduit." No large decorative rune.
Below, "ATTUNED ESSENCES" and "3 / 3 slots". Three compact TEXT-ONLY entries separated by thin rules, NO PICTURE AREA:
"01  Alpha Wolf Essence" with "Lv. 10" aligned right; secondary "Active: Alpha Fangs" and "Passive: Ruthless Instinct"; small "Conduit source".
"02  Horned Wolf Essence" with "Lv. 8"; secondary "Active: Piercing Horn" and "Passive: Protective Instinct".
"03  Bloodfang Wolf Essence" with "Lv. 6"; secondary "Active: Bloodfang Bite" and "Passive: Cruel Precision".
Names gold, secondary text muted ivory, slot numbers gray, right-aligned levels. Do not turn slots into giant bordered cards. At bottom of build area, a modest pale-gold button "Edit Essence loadout".

At lower edge of attributes, ABOVE the chat divider, a slim "EQUIPMENT" rail with EXACTLY EIGHT small square gear icons from reference and link "Open inventory →". Gear icons can be reduced to keep every attribute and button visible.

Final result should feel like an organized, premium dark-fantasy CHARACTER SHEET: dense and legible, complete combat numbers, text-only Essences, no character or monster art, same gold/charcoal sidebar and bottom chat shell.
```

## Right chat: preserve text-only design and reflow shell

```text
Use case: ui-mockup. EDIT the provided LEGENDSLEGACY text-based character sheet to create a RIGHT-SIDE CHAT variant. Keep all information, the full attribute list, and the same charcoal/gold visual system. Produce one high fidelity full-bleed 16:9 desktop browser-game screenshot, no annotations, device frame or collage.

PRESERVE THE USER'S MOST IMPORTANT CHANGES: absolutely NO profile image, human artwork, avatar, silhouette, landscape, wolf portrait, creature thumbnail, Essence icon, animal artwork or empty portrait placeholder. Essence entries must remain TEXT ONLY. The eight small equipment icons, navigation icons, currency symbols and small guild emblem can remain as shown. Use only quiet charcoal texture as backdrop.

Keep identical current-game colors: #0e0f14 charcoal canvas, #17171e surfaces, subtle grain, warm gold #f9dca0 / #fcd587 headings and action, ivory #f6f0df and gray #c3bec4 readable text, modest corners, fine gray rules. Marcellus-style serif headings with clearly sans-serif Poppins body text and tabular numeric values. No gold ornament frames, cream surfaces, neon gradient, decorative metric tiles or card nesting.

LAYOUT CHANGE: preserve the full-height left sidebar at 13% screen width, exactly the same all navigation labels and groups (Character, World, City, System), selected Overview, current idle action and Settings. REMOVE the bottom chat drawer. Create a separate dedicated CHAT PANE at far RIGHT occupying 18% of screen width and full height. Gameplay occupies the remaining center 69%, and gains the entire height. Keep a fine vertical separator between chat and gameplay. The shallow top resource header spans only central gameplay, showing Aelric / Lv.48, Cinders 24,860 and Soulstones 318; no new horizontal navigation.
Right chat: heading "Chat", "128 online", collapse chevron; tabs "Global" selected, "Guild", "Loot"; timestamps and compact fictional messages from the source with ordinary text lines, no chat bubbles; add lines "21:04 Mira: Anyone running Catacombs?" and "21:06 Mira: That sword looks great." if space permits. Composer "Message Global…" and pale-gold Send arrow fixed at bottom of chat. Gameplay is never overlaid by chat.

REFLOW CENTRAL GAMEPLAY TO USE THE FULL HEIGHT: compact name/identity band at top, "Character overview", restrained "AELRIC", "Level 48 • The Ashen Covenant", Combat XP "68% to level 49", distinct Combat Rating "12,840" and Health "8,240 / 8,240". Compact rather than an oversized headline.

Below identity, left roughly 66% of central game area shows all nineteen attributes in four groups, Offense and Defense side by side on top, Recovery and Utility side by side below, with equipment rail beneath. Right 34% contains the TEXT-ONLY current build and Essences. Allow enough column width for longest attribute names; modest readable body size, labels and values must not touch. All fields must remain visible without scrolling, omitting or truncating anything. Clear grouping by gold section names, vertical gutter, thin separators, aligned tabular right-side values.

Exact attributes, each ONCE:
OFFENSE: Power 1,284; Attack Speed 32%; Crit Chance 24.8%; Crit Damage 75%; Armor Penetration 12%; Magic Penetration 6%.
DEFENSE: Max Health 8,240; Physical Damage Reduction 38%; Magical Damage Reduction 31%; Dodge 8%; Block 16%; Damage Reduction 5%.
RECOVERY: Healing Power 15%; Health Regen 84 HP/5s; Life Steal 4%.
UTILITY: Cooldown Reduction 12%; Status Resistance 20%; Crowd Control Resistance 15%; Threat 184.6 threat/s.
Small Utility note "Threat estimated from attuned Essences." Keep these actual game labels/units, no generic Attack or Defense substitutions. The numbers are illustrative. There are exactly nineteen attribute rows.

Build pane retains "CURRENT BUILD", dropdown "Wolfsbane Hunt", "Equipped" tick, "COMBAT STYLE", gold "Conduit", the line "First occupied Essence slot powers Conduit." Beneath: "ATTUNED ESSENCES" and "3 / 3 slots".
Each text-only entry is separated by one thin horizontal rule, no boxes for images:
"01 Alpha Wolf Essence", "Lv. 10"; "Active: Alpha Fangs"; "Passive: Ruthless Instinct"; "Conduit source".
"02 Horned Wolf Essence", "Lv. 8"; "Active: Piercing Horn"; "Passive: Protective Instinct".
"03 Bloodfang Wolf Essence", "Lv. 6"; "Active: Bloodfang Bite"; "Passive: Cruel Precision".
Button "Edit Essence loadout" beneath.
Under attributes a compact horizontal rail "EQUIPMENT", EXACTLY EIGHT gear icons from input, "Open inventory →".

This should look like the same completed dark-fantasy character sheet as the source, with the chat moved to the right and the content sensibly reflowed. User priority is a legible complete attribute sheet and text-only Essences. No art must reappear.
```

