/**
 * Central registry that links the *string* you emit from the server
 * (`env.event`) to the concrete DTO interface you generated from C#.
 *
 * The DTO interfaces below are assumed to come from your OpenAPI/NSwag
 * client – adjust the import path to match your project.
 */

import { Signal } from '@angular/core';
import type { GameEventEnvelope } from './game-event-envelope';
import { LootReceivedMsg } from '../loot/loot-received';
import { MarketListingSoldMsg } from '../market/market-listing-sold';
import { MarketListingCreatedMsg } from '../market/market-listing-created';
import { MarketListingCanceledMsg } from '../market/market-listing-canceled';
import { MarketBuyOrderCreatedMsg } from '../market/market-buy-order-created';
import { MarketBuyOrderFulfilledMsg } from '../market/market-buy-order-fulfilled';
import { MarketBuyOrderCanceledMsg } from '../market/market-buy-order-canceled';
import { SoulstoneDropMsg } from '../character/soulstone-drop';
import { CharacterLevelUpMsg } from '../character/character-level-up';
import { ArenaBattleCompletedMsg } from '../colosseum/arena-battle-completed';
import { GuildBuildingsChangedMsg } from '../guild/guild-buildings-changed';
import { TournamentGroundsUpdated } from '../colosseum/tournament-grounds-updated';
import { GuildApplicationMsg } from '../guild/guild-application';
import { GuildInviteReceivedMsg } from '../guild/guild-invite-received';
import { GuildInviteRejectedMsg } from '../guild/guild-invite-rejected';
import { GuildApplicationRejectedMsg } from '../guild/guild-application-rejected';
import { GuildStateChangedMsg } from '../guild/guild-state-changed';
import { GuildMembershipChangedMsg } from '../guild/guild-membership-changed';
import { GuildDisbandedMsg } from '../guild/guild-disbanded';
import { GuildDirectoryChangedMsg } from '../guild/guild-directory-changed';
import { ProphecyProgressedMsg } from '../prophecies/prophecy-progressed';
import { AchievementUnlockedMsg } from '../achievement/achievement-unlocked';
import { QuestJournalChangedMsg } from '../quest/quest-journal-changed';

export const gameEventNames = [
  'LootReceivedMsg',
  'MarketListingSoldMsg',
  'MarketListingCreatedMsg',
  'MarketListingCanceledMsg',
  'MarketBuyOrderCreatedMsg',
  'MarketBuyOrderFulfilledMsg',
  'MarketBuyOrderCanceledMsg',
  'SoulstoneDropMsg',
  'CharacterLevelUpMsg',
  'ArenaBattleCompletedMsg',
  'GuildBuildingsChangedMsg',
  'TournamentGroundsUpdated',
  'GuildApplicationMsg',
  'GuildInviteReceivedMsg',
  'GuildInviteRejectedMsg',
  'GuildApplicationRejectedMsg',
  'GuildStateChangedMsg',
  'GuildMembershipChangedMsg',
  'GuildDisbandedMsg',
  'GuildDirectoryChangedMsg',
  'ProphecyProgressedMsg',
  'AchievementUnlockedMsg',
  'QuestJournalChangedMsg',
] as const;

export type GameEventSignalMap = {
  [K in GameEventName]: Signal<GameEventMap[K] | null>;
};

export type GameEventEnvelopeSignalMap = {
  [K in GameEventName]: Signal<GameEventEnvelope<K> | null>;
};

/** Key = discriminator string, Value = payload DTO */
export interface GameEventMap {
  LootReceivedMsg: LootReceivedMsg;
  MarketListingSoldMsg: MarketListingSoldMsg;
  MarketListingCreatedMsg: MarketListingCreatedMsg;
  MarketListingCanceledMsg: MarketListingCanceledMsg;
  MarketBuyOrderCreatedMsg: MarketBuyOrderCreatedMsg;
  MarketBuyOrderFulfilledMsg: MarketBuyOrderFulfilledMsg;
  MarketBuyOrderCanceledMsg: MarketBuyOrderCanceledMsg;
  SoulstoneDropMsg: SoulstoneDropMsg;
  CharacterLevelUpMsg: CharacterLevelUpMsg;
  ArenaBattleCompletedMsg: ArenaBattleCompletedMsg;
  GuildBuildingsChangedMsg: GuildBuildingsChangedMsg;
  TournamentGroundsUpdated: TournamentGroundsUpdated;
  GuildApplicationMsg: GuildApplicationMsg;
  GuildInviteReceivedMsg: GuildInviteReceivedMsg;
  GuildInviteRejectedMsg: GuildInviteRejectedMsg;
  GuildApplicationRejectedMsg: GuildApplicationRejectedMsg;
  GuildStateChangedMsg: GuildStateChangedMsg;
  GuildMembershipChangedMsg: GuildMembershipChangedMsg;
  GuildDisbandedMsg: GuildDisbandedMsg;
  GuildDirectoryChangedMsg: GuildDirectoryChangedMsg;
  ProphecyProgressedMsg: ProphecyProgressedMsg;
  AchievementUnlockedMsg: AchievementUnlockedMsg;
  QuestJournalChangedMsg: QuestJournalChangedMsg;
  //   SaleCompleted: SaleCompletedMsg;
  //   RiftOpened:    RiftOpenedMsg;
  // 💡 when you add a new C# record and regenerate the client
  // just place another line here – TypeScript will yell until you do.
}

/** Convenience alias you can reuse elsewhere */
export type GameEventName = (typeof gameEventNames)[number];

export function isGameEventName(name: string): name is GameEventName {
  return gameEventNames.includes(name as GameEventName);
}
