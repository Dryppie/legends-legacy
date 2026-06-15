/**
 * Central registry that links the *string* you emit from the server
 * (`env.event`) to the concrete DTO interface you generated from C#.
 *
 * The DTO interfaces below are assumed to come from your OpenAPI/NSwag
 * client – adjust the import path to match your project.
 */

import { Signal } from '@angular/core';
import { LootReceivedMsg } from '../loot/loot-received';
import { MarketListingSoldMsg } from '../market/market-listing-sold';
import { GuildBuildingUpgradedMsg } from '../guild/guild-building-upgraded';

export const gameEventNames = [
  'LootReceivedMsg',
  'MarketListingSoldMsg',
  'GuildBuildingUpgradedMsg',
] as const;

export type GameEventSignalMap = {
  [K in GameEventName]: Signal<GameEventMap[K] | null>;
};

/** Key = discriminator string, Value = payload DTO */
export interface GameEventMap {
  LootReceivedMsg: LootReceivedMsg;
  MarketListingSoldMsg: MarketListingSoldMsg;
  GuildBuildingUpgradedMsg: GuildBuildingUpgradedMsg;
  //   SaleCompleted: SaleCompletedMsg;
  //   GuildApplication:        GuildApplicationMsg;
  //   RiftOpened:    RiftOpenedMsg;
  // 💡 when you add a new C# record and regenerate the client
  // just place another line here – TypeScript will yell until you do.
}

/** Convenience alias you can reuse elsewhere */
export type GameEventName = (typeof gameEventNames)[number];

export function isGameEventName(name: string): name is GameEventName {
  return gameEventNames.includes(name as GameEventName);
}
