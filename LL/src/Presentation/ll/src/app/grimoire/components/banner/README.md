# Banner

The headline block.

**Status:** Draft

A framed band with painted art behind it, for the one headline block on an information screen (the Combat Profile).

**Use:**

```html
<lg-banner label="Combat profile" [image]="art" [cornerSrc]="corner">
  <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane">…</lg-profile-identity>
  <lg-banner-aside>
    <lg-stat-figure label="Level" [value]="17" caption="8,420 / 12,000 Combat XP" />
    <lg-stat-figure label="Combat Rating" value="1,284" />
  </lg-banner-aside>
</lg-banner>
```

**Provide:** the identity as content (eyebrow, name, meta), the headline figures in `<lg-banner-aside>` (StatFigures, or a LevelPlate where the level is the subject — they sit beside the identity whenever both fit and drop under it only when they don't, D-122), an optional `<lg-banner-footer>`, `label`, `image` (a Backgrounds asset), `focus` and `cornerSrc` (the CornerOrnament asset). Import `LG_BANNER` for all three. With a `label` the Banner is a region named by it.

- The art is darkened, warmed and blurred, then covered by the veil: a left-to-right `ground-deep` vignette and a film grain like the Stage's. Without an `image` there is no veil and no grain: both belong to the art. The veil is the text's contrast surface — at least 66% `ground-deep` across the left 60%, where the text sits, so `ink` holds 9.9:1 over the brightest art — and text also takes `shadow-text-art`. The Banner is a Level 1 enclosure filled with art instead of `surface` (Foundations · Surfaces & Layering).
- The double `gilt` hairline frame at 28% and the four corner ornaments set it apart from plain panels. The frame is its one edge: no border outside it. The double gilt frame belongs to the Banner and the Folio only (Foundations · Lines).
- The identity and the headline figures sit side by side in a Wide region and stack below it (Foundations · Layout). The art may bleed edge to edge of the stage (`.lg-bleed`); the text keeps to the content edge. In a region under 30rem (400% zoom) the body's padding steps down to `inset-lg`.
- **Its ornament budget** (Foundations · Ornament): the Banner is the screen's one ornamented framed surface — frame, corners, grain and vignette — so a screen with a Banner has no Folio. It holds no ornament rule unless the screen spends its second one there, and nothing in it is lit. A Banner beside a Folio inside one GameShell logs a console warning.
- One Banner per screen, on information screens only. Everything else is a Panel.
