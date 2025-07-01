/**
 * Central registry that links the *string* you emit from the server
 * (`env.event`) to the concrete DTO interface you generated from C#.
 *
 * The DTO interfaces below are assumed to come from your OpenAPI/NSwag
 * client – adjust the import path to match your project.
 */

import { Signal } from '@angular/core';
import { LootReceivedMsg } from '../loot/loot-received';

export type GameEventSignalMap = {
  [K in GameEventName]: Signal<GameEventMap[K] | null>;
};

/** Key = discriminator string, Value = payload DTO */
export interface GameEventMap {
  LootReceivedMsg: LootReceivedMsg;
  //   SaleCompleted: SaleCompletedMsg;
  //   GuildApplication:        GuildApplicationMsg;
  //   GuildBuildingUpgraded:   GuildBuildingUpgradedMsg;
  //   RiftOpened:    RiftOpenedMsg;
  // 💡 when you add a new C# record and regenerate the client
  // just place another line here – TypeScript will yell until you do.
}

/** Convenience alias you can reuse elsewhere */
export type GameEventName = keyof GameEventMap;
