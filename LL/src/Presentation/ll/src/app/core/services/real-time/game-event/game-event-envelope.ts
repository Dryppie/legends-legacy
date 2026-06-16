import type { GameEventMap, GameEventName } from './game-event.map';

export interface GameEventEnvelope<TEvent extends string = GameEventName> {
  updateId?: string;
  occurredAt?: string;
  event: TEvent;
  payload: TEvent extends GameEventName ? GameEventMap[TEvent] : unknown;
}
