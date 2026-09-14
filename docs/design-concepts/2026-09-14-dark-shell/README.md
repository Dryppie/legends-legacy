# LegendsLegacy — current theme, sidebar, and chat

Two revised static desktop concepts, created 14 September 2026 using the built-in image generation tool. Both show the same character overview so chat placement can be compared.

The existing game's theme was taken from the actual frontend tokens: charcoal canvas `#0e0f14`, background `#17171e`, warm gold `#f9dca0` and `#fcd587`, ivory `#f6f0df`, and gray secondary text. The existing `assets/core/texture.png` was supplied as a visual reference. Generated colors approximate these targets; these images are not pixel-exact token specifications.

Both retain the labeled left sidebar with Character, World, City, and System groups, using the destinations in the current sidebar service. The current idle activity sits above navigation. A shallow header carries player identity and currencies. New character and creature artwork gives the overview identity while statistics, eight equipment icons, Combat Style, and ordered Essence slots remain visible.

## A — right-side chat

Chat occupies a dedicated narrow right pane, with tabs, visible messages, and a bottom-anchored composer. The main game area retains its height; the sidebar and chat reduce available width. This composition suits sessions where watching chat continuously matters.

![Dark shell with right-side chat](C:/repos/Legends-Legacy/legends-legacy/docs/design-concepts/2026-09-14-dark-shell/01-right-chat.png)

## B — bottom chat

Chat occupies an expanded horizontal drawer below gameplay, while the sidebar remains full-height. Gameplay uses the full width to the right of navigation, leaving more horizontal space for inventory comparisons, Essence editing, and route maps. The drawer reduces vertical space and shows fewer messages at once; its visible collapse control is part of the proposed layout.

![Dark shell with bottom chat](C:/repos/Legends-Legacy/legends-legacy/docs/design-concepts/2026-09-14-dark-shell/02-bottom-chat.png)

## Design judgment and scope

Both positions can share one visual system. A selectable dock position would accommodate different screens and player preferences. For comparison-heavy screens, bottom chat offers useful width; right-side chat retains more vertical space.

These remain visual concepts. Sample names, chat messages, item examples, and values are fictional illustration data, not a live game session. The character/creature art is generated. No messages were sent. No application functionality was implemented.

Added files: two PNGs, this visual index, and [PROMPTS.md](PROMPTS.md). The previous pale concepts and all generated originals remain intact.

Verification: both images were visually reviewed for theme, sidebar groups, chat placement, and visible build information; both were successfully decoded and their copied bytes checked against the originals using SHA-256 hashes. Markdown was checked for whitespace errors. No builds or application tests were needed for static image/document outputs, and no required checks were blocked. No source-code changes, migrations, configuration changes, or deployment actions.

