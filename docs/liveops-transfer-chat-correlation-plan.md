# LiveOps Transfer and Chat Correlation Plan

## Objective

Give LiveOps operators a consolidated view of player-to-player transfers and the
recorded in-game communication between the participants. The feature should help
surface transfer patterns that may warrant review for alternate-account abuse.

The absence of recorded chat is a useful signal, but it is not proof that two
players did not communicate. Players may coordinate through external services,
voice chat, guild communities, or in person. The dashboard must therefore use the
phrase **No recorded in-game conversation** and present the underlying evidence
instead of making an automatic accusation.

This plan assumes that a wire is a direct player-to-player item or currency
transfer. If wires are represented by a separate domain model, they should be
normalized into the same evidence contract as other transfers.

## 1. Transfer and conversation evidence model

For every transfer, collect the following evidence:

- Sender and recipient character and account identifiers.
- Transfer timestamp, direction, items, currency, and estimated value.
- Direct whispers in both directions between the participants.
- Relevant shared guild or raid chat activity.
- Account creation dates.
- Previous transfers between the same accounts.
- Existing login-timestamp correlation signals.

Use configurable observation windows. Initial defaults should be:

- Conversation history: 30 days before the transfer.
- Immediate coordination: 24 hours before through 2 hours after the transfer.
- Transfer relationship history: 90 days before the transfer.

The returned evidence must retain the exact window used so operators can
understand what was and was not searched.

## 2. Batched internal Chat endpoint

Add a secret-authenticated internal Chat endpoint that accepts multiple player
pairs and observation windows in one request. A batch contract prevents a
LiveOps transfer page from making one Chat request per transfer.

For each pair, return:

- Direct-message count from the sender to the recipient.
- Direct-message count from the recipient to the sender.
- First and last direct-message timestamps.
- Whether direct communication was bidirectional.
- Shared guild or raid activity as separate, weaker evidence.
- A bounded sample of relevant messages.
- An opaque cursor for loading additional messages.
- A distinct availability state.

An unavailable or timed-out Chat service must never be represented as zero
messages.

The LiveOps browser must continue to communicate only with API.LiveOps. The
existing server-to-server Chat secret remains inside the LiveOps API gateway.

## 3. Chat persistence and indexes

Support efficient direct-conversation queries in both directions. Two possible
approaches are available:

### Initial approach

Add indexes covering:

- Sender, recipient, channel type, timestamp, and message ID.
- Recipient, sender, channel type, timestamp, and message ID.

This works with the existing Chat message model and avoids a historical data
backfill.

### Long-term approach

Add a normalized conversation key derived from the sorted participant IDs and
index it with timestamp and message ID. This produces simpler pair queries but
requires populating the key for existing whispers.

Shared-channel evidence should have an index covering channel type, context key,
timestamp, and sender ID if its query plan requires one.

## 4. LiveOps correlation evaluator

Classify the communication associated with each transfer as one of:

- **Established conversation**: direct messages exist in both directions.
- **One-way conversation**: only one participant sent direct messages.
- **Shared-channel activity**: no direct messages were found, but relevant guild
  or raid activity exists.
- **No recorded in-game conversation**: no relevant retained messages were found
  in the configured window.
- **Chat unavailable**: Chat could not provide reliable evidence.

The evaluator should also calculate transfer-pattern facts such as:

- Repeated high-value, one-way transfers.
- Multiple transfers without recorded conversation.
- A newly created recipient account.
- Login timestamps correlated around transfer events.
- A recipient receiving value from several related accounts.
- Little or no reciprocal economic activity.
- Transfers shortly after synchronized logins.
- Transfer value materially outside normal player behavior.

These should be data-driven facts with configurable thresholds. The dashboard
should show each contributing fact and its measurements rather than exposing only
a black-box score.

An aggregated signal such as `UncommunicativeValueTransferPattern` can be raised
when all of the following are true:

- At least three qualifying transfers exist in the observation period.
- Their combined value exceeds the configured materiality threshold.
- No direct bidirectional conversation is recorded for the relationship during
  the relevant observation windows.

This signal does not require another independent risk signal. Its severity should
still reflect the transfer count, total value, directionality, and strength of the
available conversation evidence.

## 5. LiveOps transfer-history presentation

Add a **Conversation** column to the player transfer history:

- Green: established direct conversation.
- Amber: one-way conversation or shared-channel activity.
- Gray: no recorded in-game conversation.
- Neutral unavailable state: Chat evidence could not be retrieved.
- Red risk treatment only when the transfer pattern itself crosses the configured
  investigation threshold.

Selecting a transfer should open a combined chronological timeline, for example:

```text
19:42  Sender logs in
19:45  Whisper: "send it to this character"
19:47  500,000 currency transferred
19:48  Recipient logs out
```

Display 25 messages initially and use opaque cursor pagination to load 25 more.
Message content must be rendered as text and safely escaped.

The detail view should also show:

- The observation window.
- Message counts in each direction.
- The transfer relationship's total count and value.
- Whether participants shared a guild or raid channel.
- Any unavailable or incomplete evidence sources.

## 6. Account-risk aggregation

Aggregate qualifying transfers by account pair and observation period. Add the
result to the existing account-risk details rather than creating a disconnected
investigation surface.

The risk explanation should use concrete language such as:

> Five one-way transfers totaling 820,000 value were recorded over 12 days. No
> bidirectional in-game conversation was found in the configured windows.

It must not state that the accounts are definitively controlled by the same
person. Operators should be able to navigate from the aggregate signal to every
supporting transfer and conversation record.

## 7. Authorization, privacy, and auditing

Because this feature may expose whispers:

- Require the existing LiveOps read permission at the API boundary.
- Keep Chat service credentials server-side.
- Escape all player-authored content.
- Log access to conversation evidence with the operator and target identifiers.
- Avoid placing message bodies in ordinary application logs or error telemetry.
- Respect the configured Chat retention period.
- Display retained-data limitations alongside negative conversation evidence.

## 8. Delivery phases

### Phase 1: Evidence viewer

- Implement the batched internal Chat conversation endpoint.
- Add the LiveOps gateway and transfer-level evidence endpoint.
- Display conversation status and a paginated combined timeline.
- Do not change account-risk severity automatically in this phase.

### Phase 2: Transfer-pattern correlation

- Aggregate transfer relationships over configurable periods.
- Add `UncommunicativeValueTransferPattern` and its explanation.
- Link every signal to the supporting transfers and messages.

### Phase 3: Calibration

- Compare the signal against confirmed abuse cases and legitimate transfers.
- Tune transfer count, value, and observation-window thresholds.
- Measure how often external communication or ordinary gifting explains a hit.

### Phase 4: Operationalization

- Enable filtering and sorting by the new signal in account-risk views.
- Add monitoring for Chat evidence availability, latency, and batch size.
- Document the investigation workflow and interpretation limitations.

## 9. Testing and acceptance criteria

The implementation is complete when:

- Direct whispers are detected in both directions.
- One-way communication is distinguished from bidirectional conversation.
- Guild and raid activity is not misrepresented as direct conversation.
- System-generated messages do not count as player conversation.
- Chat failures produce an unavailable state rather than a no-message result.
- Transfer pages use batched Chat queries rather than an N+1 request pattern.
- All result pages are bounded and cursor-paginated.
- Equal-timestamp messages cannot be skipped or duplicated at page boundaries.
- Message bodies are safely rendered and excluded from routine logs.
- Operators can inspect the exact evidence behind every classification and risk
  signal.
- The UI consistently says **No recorded in-game conversation** instead of
  claiming that no communication occurred.
- Database migrations and deployment ordering are documented and verified.

## 10. Deployment implications

The likely rollout order is:

1. Apply the required Chat database indexes.
2. Deploy Chat with the new internal batch endpoint.
3. Deploy API.LiveOps with gateway and correlation support.
4. Deploy the LiveOps frontend.
5. Enable aggregate risk classification after calibration.

No infrastructure repository changes should be included here. If endpoint
timeouts or batch limits require environment configuration, add only documented
application settings with safe defaults and coordinate the external deployment
configuration separately.
