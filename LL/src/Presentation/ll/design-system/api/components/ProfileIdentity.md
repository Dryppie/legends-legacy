<!-- Maintained by hand (D-091): update this card when the component's props in components/index.d.ts change. -->
# ProfileIdentity

Who a player is.

`LL.ProfileIdentity` · Game Components · Character

## Use

```jsx
<LL.ProfileIdentity name="Aldric Vane" />
```

```html
<x-import component-from-global-scope="LL.ProfileIdentity" name="Aldric Vane"></x-import>
```

## Notes

Who a player is, at the head of a profile: put it in a Banner's body (D-099).

## Props (ProfileIdentityProps)

| prop | type | note |
| --- | --- | --- |
| `eyebrow` | `React.ReactNode` | "Combat Profile"; "Viewing player" on someone else's. |
| `name` * | `React.ReactNode` |  |
| `noble` | `boolean` | Active Nobility: the crown before the name (D-066). |
| `presence` | `PresenceProps` | Another player's: Presence after the name. |
| `facts` | `(ProfileFact \| null \| false \| undefined)[]` | Guild, Essences, Achievement Points, Nobility… A value may hold a link, a Tag or a link Button. Falsy entries are skipped. |
| `as` | `'h1' \| 'h2' \| 'h3'` | The name's heading element. Default h2. |
| `headingId` | `string` |  |

### ProfileFact

| prop | type | note |
| --- | --- | --- |
| `label` * | `React.ReactNode` |  |
| `value` * | `React.ReactNode` |  |
| `key` | `string` |  |

## More

Long-form usage: `components/ProfileIdentity/README.md`.
