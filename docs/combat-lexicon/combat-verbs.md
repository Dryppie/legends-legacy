# Combat Verbs

One verb has one canonical meaning. A verb may be valid vocabulary while still lacking a runtime primitive.

| Verb      | Stable ID        | Canonical meaning                                                                  | Current implementation                                    |
| --------- | ---------------- | ---------------------------------------------------------------------------------- | --------------------------------------------------------- |
| Apply     | `verb.apply`     | Add a condition, stack, charge, or pool contribution according to its rules. The hover definition specifies what X means in `Condition(X)`. | `ApplyStatus`, `GrantBarrier`                             |
| Refresh   | `verb.refresh`   | Reset remaining duration to its normal or specified duration.                      | Status `Refresh` policy                                   |
| Extend    | `verb.extend`    | Add time to remaining duration, subject to a cap.                                  | No general operation                                      |
| Intensify | `verb.intensify` | Increase documented magnitude, stacks, charges, or stored value.                   | `ModifyStatusStacks` covers stacks only                   |
| Consume   | `verb.consume`   | Remove a documented quantity to produce another effect.                            | Stack consumption exists; no general charge/pool verb     |
| Cleanse   | `verb.cleanse`   | Remove qualifying harmful conditions from an ally or self.                         | `Cleanse` exists but currently removes all statuses       |
| Dispel    | `verb.dispel`    | Remove qualifying beneficial conditions from an enemy.                             | `Dispel`; positive `baseValue` limits removals per target  |
| Detonate  | `verb.detonate`  | Trigger remaining periodic or stored damage immediately under its condition rules. | No general operation                                      |
| Spread    | `verb.spread`    | Copy a condition to additional valid targets while preserving source ownership.    | No general operation                                      |
| Transfer  | `verb.transfer`  | Move a condition from one target to another.                                       | No general operation                                      |
| Copy      | `verb.copy`      | Create a new instance from an effect without removing the original.                | No general operation                                      |
| Convert   | `verb.convert`   | Replace one condition, type, resource, or value with another under explicit rules. | No general operation                                      |
| Prevent   | `verb.prevent`   | Stop a qualifying event before state mutation.                                     | No generic operation; canonical Ward uses this behavior   |
| Redirect  | `verb.redirect`  | Change the destination of a qualifying target or effect.                           | Taunt influences selection but is not general redirection |
| Suppress  | `verb.suppress`  | Temporarily stop a mechanic from producing its effect without removing it.         | No general operation                                      |
| Suspend   | `verb.suspend`   | Pause duration or effect processing without removing the object.                   | No current operation                                      |

Additional executable operations retain literal meanings: **Deal** resolves `Damage`; **Heal** restores health; **Grant** adds Barrier/resource; **Modify** changes an attribute; **Reset** returns one named active ability to its full effective cooldown through `ResetAbilityCooldown`; **Summon** creates an owner-linked combatant; **Expire** ends lifecycle state.

Aliases such as _purge_ should map to Dispel or Cleanse only after target polarity is known. _Remove_ is neutral and should name the exact condition. _Absorb_ describes Barrier consumption, not healing. Standard conditions classify harmful and beneficial effects for Cleanse and Dispel; legacy statuses still lack that polarity metadata. Expiry, explicit removal, Cleanse, and Dispel publish distinct events.
