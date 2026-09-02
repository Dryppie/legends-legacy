import {
  FIRST_WEAPON_QUEST_ID,
  HEART_OF_THE_HOLLOW_QUEST_ID,
  INTO_LUMO_RUINS_QUEST_ID,
  QuestJournal,
  QuestState,
  QuestStatus,
  SOUL_ARCHIVE_QUEST_ID,
  TOOLS_OF_THE_TRADE_QUEST_ID,
  TRAINING_DAY_QUEST_ID,
} from '../../../../shared/models/quest';
import { SidebarSection } from '../../../../shared/models/sidebar-item';

export enum PlayerJourneyStage {
  FirstHunt = 0,
  SoulArchive = 1,
  FirstWeapon = 2,
  GatheringTool = 3,
  EnterLumo = 4,
  Shenic = 5,
  BetaComplete = 6,
}

export const PLAYER_JOURNEY_SOCIAL_UNLOCK_LEVEL = 10;
export const PLAYER_JOURNEY_ECONOMY_UNLOCK_LEVEL = 20;
export const PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL = 30;

export interface PlayerJourneyAction {
  label: string;
  route: string;
}

export interface PlayerJourneyGuidance {
  stage: PlayerJourneyStage;
  phaseLabel: string;
  title: string;
  summary: string;
  objective: string;
  primaryAction: PlayerJourneyAction;
  optionalAction: PlayerJourneyAction;
  nextUnlockLabel: string;
  nextUnlock: string;
}

const STAGE_QUESTS: ReadonlyArray<{
  stage: PlayerJourneyStage;
  questId: string;
}> = [
  { stage: PlayerJourneyStage.FirstHunt, questId: TRAINING_DAY_QUEST_ID },
  { stage: PlayerJourneyStage.SoulArchive, questId: SOUL_ARCHIVE_QUEST_ID },
  { stage: PlayerJourneyStage.FirstWeapon, questId: FIRST_WEAPON_QUEST_ID },
  {
    stage: PlayerJourneyStage.GatheringTool,
    questId: TOOLS_OF_THE_TRADE_QUEST_ID,
  },
  { stage: PlayerJourneyStage.EnterLumo, questId: INTO_LUMO_RUINS_QUEST_ID },
];

export function resolvePlayerJourneyStage(
  journal: QuestJournal,
): PlayerJourneyStage {
  for (const stageQuest of STAGE_QUESTS) {
    if (!isQuestCompleted(journal, stageQuest.questId)) {
      return stageQuest.stage;
    }
  }

  if (isQuestCompleted(journal, HEART_OF_THE_HOLLOW_QUEST_ID)) {
    return PlayerJourneyStage.BetaComplete;
  }

  return PlayerJourneyStage.Shenic;
}

export function buildPlayerJourneyGuidance(
  journal: QuestJournal,
  characterLevel: number,
): PlayerJourneyGuidance | null {
  const stage = resolvePlayerJourneyStage(journal);
  const stageQuestId = STAGE_QUESTS.find(
    (entry) => entry.stage === stage,
  )?.questId;
  const stageQuest = stageQuestId
    ? journal.quests.find((quest) => quest.questId === stageQuestId)
    : undefined;
  const journeyQuest = findJourneyQuest(journal);
  const quest =
    stage >= PlayerJourneyStage.Shenic ? journeyQuest : stageQuest;
  const incompleteObjective = quest?.objectives.find(
    (objective) => !objective.isCompleted,
  );
  const requiresChoice = !!quest?.choice && !quest.choice.selectedOptionKey;

  if (quest) {
    return {
      stage,
      phaseLabel: quest.chain?.title ?? phaseLabel(stage),
      title: quest.title,
      summary: quest.summary,
      objective: requiresChoice
        ? (quest.choice?.selectionSummary ?? 'Choose how your journey begins.')
        : (incompleteObjective?.description ??
          'Claim the reward and continue.'),
      primaryAction: {
        label: requiresChoice
          ? quest.questId === TRAINING_DAY_QUEST_ID
            ? 'Choose First Hunt'
            : 'Choose Chapter Reward'
          : (incompleteObjective?.presentation.actionLabel ?? 'Open Quests'),
        route: requiresChoice
          ? '/game/quests'
          : incompleteObjective?.presentation.destinationRoute ||
            '/game/quests',
      },
      optionalAction: optionalAction(stage),
      nextUnlockLabel: quest.chain?.promisedReward
        ? 'Chapter reward'
        : 'Next unlock',
      nextUnlock:
        quest.chain?.promisedReward ?? nextUnlock(stage, characterLevel),
    };
  }

  if (
    stage === PlayerJourneyStage.BetaComplete &&
    characterLevel > PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL
  ) {
    return null;
  }

  return {
    stage,
    phaseLabel: phaseLabel(stage),
    title:
      stage === PlayerJourneyStage.BetaComplete
        ? 'Shenic Beta journey complete'
        : stage === PlayerJourneyStage.Shenic
        ? 'Develop your Shenic build'
        : 'Continue the First Steps',
    summary:
      stage === PlayerJourneyStage.BetaComplete
        ? 'You reached level 30, cleared the Heart of the Hollow, and completed the focused Beta journey.'
        : stage === PlayerJourneyStage.Shenic
        ? 'Fight in the newest available area, strengthen your loadout, and prepare for the next Shenic challenge.'
        : 'Open the Quest Journal to continue the guided introduction.',
    objective:
      stage === PlayerJourneyStage.BetaComplete
        ? 'Review the build that carried you through Shenic and the choices you made along the way.'
        : stage === PlayerJourneyStage.Shenic
        ? 'Choose a current quest or return to the World Map.'
        : 'Open the Quest Journal and follow the highlighted objective.',
    primaryAction: {
      label:
        stage === PlayerJourneyStage.BetaComplete
          ? 'Review Your Build'
          : stage === PlayerJourneyStage.Shenic
            ? 'Open World Map'
            : 'Open Quests',
      route:
        stage === PlayerJourneyStage.BetaComplete
          ? '/game/character/essences'
          : stage === PlayerJourneyStage.Shenic
            ? '/game/world/shenic'
            : '/game/quests',
    },
    optionalAction: optionalAction(stage),
    nextUnlockLabel:
      stage === PlayerJourneyStage.BetaComplete
        ? 'Future aspiration'
        : 'Next unlock',
    nextUnlock: nextUnlock(stage, characterLevel),
  };
}

export function filterSidebarForPlayerJourney(
  sections: SidebarSection[],
  journal: QuestJournal,
  characterLevel: number,
  focusedBetaJourney: boolean,
): SidebarSection[] {
  if (!focusedBetaJourney) return sections;

  const stage = resolvePlayerJourneyStage(journal);
  if (characterLevel >= PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL) {
    return sections;
  }

  const visibleItemIds = new Set(['character-overview', 'quests', 'settings']);

  if (stage >= PlayerJourneyStage.SoulArchive) {
    visibleItemIds.add('inventory');
    visibleItemIds.add('essences');
  }
  if (stage >= PlayerJourneyStage.FirstWeapon) {
    visibleItemIds.add('crafting');
  }
  if (stage >= PlayerJourneyStage.EnterLumo) {
    visibleItemIds.add('world');
  }
  if (stage >= PlayerJourneyStage.Shenic) {
    visibleItemIds.add('prophecies');
    visibleItemIds.add('colosseum');

    if (characterLevel >= PLAYER_JOURNEY_SOCIAL_UNLOCK_LEVEL) {
      visibleItemIds.add('achievements');
      visibleItemIds.add('soulstone-archive');
      visibleItemIds.add('guild');
    }

    if (characterLevel >= PLAYER_JOURNEY_ECONOMY_UNLOCK_LEVEL) {
      visibleItemIds.add('market-place');
      visibleItemIds.add('tavern');
    }
  }

  const destinationRoute = getPlayerJourneyDestinationRoute(journal);

  return sections
    .map((section) => ({
      ...section,
      items: section.items.filter(
        (item) =>
          visibleItemIds.has(item.id) ||
          (!!destinationRoute &&
            routeMatchesItem(destinationRoute, item.route)),
      ),
    }))
    .filter((section) => section.items.length > 0);
}

export function isPlayerJourneyOnboardingComplete(
  journal: QuestJournal,
  characterLevel: number,
): boolean {
  return (
    characterLevel >= PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL ||
    resolvePlayerJourneyStage(journal) >= PlayerJourneyStage.Shenic
  );
}

export function getPlayerJourneyDestinationRoute(
  journal: QuestJournal,
): string | null {
  const pinnedQuest = findPinnedQuest(journal);
  if (
    !pinnedQuest ||
    (pinnedQuest.choice && !pinnedQuest.choice.selectedOptionKey)
  ) {
    return null;
  }

  return (
    pinnedQuest.objectives.find((objective) => !objective.isCompleted)
      ?.presentation.destinationRoute ?? null
  );
}

function findPinnedQuest(journal: QuestJournal): QuestState | undefined {
  return (
    journal.quests.find((quest) => quest.questId === journal.pinnedQuestId) ??
    journal.quests.find((quest) => quest.isPinned)
  );
}

function findJourneyQuest(journal: QuestJournal): QuestState | undefined {
  const pinnedQuest = findPinnedQuest(journal);
  if (pinnedQuest?.status === QuestStatus.Active) {
    return pinnedQuest;
  }

  return journal.quests
    .filter(
      (quest) => quest.status === QuestStatus.Active && !!quest.chain,
    )
    .sort((left, right) => left.sortOrder - right.sortOrder)[0];
}

function isQuestCompleted(journal: QuestJournal, questId: string): boolean {
  return journal.quests.some(
    (quest) =>
      quest.questId === questId && quest.status === QuestStatus.Completed,
  );
}

function routeMatchesItem(
  destinationRoute: string,
  itemRoute: string[],
): boolean {
  const destinationPath = normalizeRoute(destinationRoute);
  const routePath = normalizeRoute(`/game/${itemRoute.join('/')}`);
  return (
    destinationPath === routePath || destinationPath.startsWith(`${routePath}/`)
  );
}

function normalizeRoute(route: string): string {
  const path = route.split(/[?#]/, 1)[0];
  const withSlash = path.startsWith('/') ? path : `/${path}`;
  return withSlash.startsWith('/game/') ? withSlash : `/game${withSlash}`;
}

function phaseLabel(stage: PlayerJourneyStage): string {
  switch (stage) {
    case PlayerJourneyStage.FirstHunt:
      return 'First Hunt';
    case PlayerJourneyStage.SoulArchive:
      return 'Claim Your Power';
    case PlayerJourneyStage.FirstWeapon:
      return 'Prepare Your Gear';
    case PlayerJourneyStage.GatheringTool:
      return 'Choose a Trade';
    case PlayerJourneyStage.EnterLumo:
      return 'Enter Shenic';
    case PlayerJourneyStage.BetaComplete:
      return 'Journey Complete';
    default:
      return 'Shenic Journey';
  }
}

function optionalAction(stage: PlayerJourneyStage): PlayerJourneyAction {
  switch (stage) {
    case PlayerJourneyStage.FirstHunt:
      return { label: 'Review Tutorial', route: '/game/quests' };
    case PlayerJourneyStage.SoulArchive:
      return { label: 'Inspect Inventory', route: '/game/character/inventory' };
    case PlayerJourneyStage.FirstWeapon:
    case PlayerJourneyStage.GatheringTool:
      return { label: 'Review Loadout', route: '/game/character/essences' };
    case PlayerJourneyStage.EnterLumo:
      return { label: 'Check Equipment', route: '/game/character/inventory' };
    case PlayerJourneyStage.BetaComplete:
      return { label: 'Review Completed Quests', route: '/game/quests' };
    default:
      return { label: 'Review Loadout', route: '/game/character/essences' };
  }
}

function nextUnlock(stage: PlayerJourneyStage, characterLevel: number): string {
  switch (stage) {
    case PlayerJourneyStage.FirstHunt:
      return 'Soul Archive after completing your First Hunt';
    case PlayerJourneyStage.SoulArchive:
      return 'Crafting after attuning your first Essence';
    case PlayerJourneyStage.FirstWeapon:
      return 'Gathering tools after crafting your first weapon';
    case PlayerJourneyStage.GatheringTool:
      return 'World Map after equipping a gathering tool';
    case PlayerJourneyStage.EnterLumo:
      return 'The Shenic journey after your first Lumo victory';
    case PlayerJourneyStage.BetaComplete:
      return 'Future Shenic chapters beyond the focused Beta';
    default:
      if (characterLevel < 10) return 'A second Essence slot at level 10';
      if (characterLevel < 20) return 'A third Essence slot at level 20';
      if (characterLevel < 30) return 'A fourth Essence slot at level 30';
      return 'The level 30 Shenic Beta milestone';
  }
}
