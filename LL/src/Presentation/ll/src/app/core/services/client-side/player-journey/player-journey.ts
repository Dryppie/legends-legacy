import {
  FIRST_WEAPON_QUEST_ID,
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
}

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

  return PlayerJourneyStage.Shenic;
}

export function buildPlayerJourneyGuidance(
  journal: QuestJournal,
  characterLevel: number,
): PlayerJourneyGuidance {
  const stage = resolvePlayerJourneyStage(journal);
  const stageQuestId = STAGE_QUESTS.find(
    (entry) => entry.stage === stage,
  )?.questId;
  const stageQuest = stageQuestId
    ? journal.quests.find((quest) => quest.questId === stageQuestId)
    : undefined;
  const pinnedQuest = findPinnedQuest(journal);
  const quest = stage === PlayerJourneyStage.Shenic ? pinnedQuest : stageQuest;
  const incompleteObjective = quest?.objectives.find(
    (objective) => !objective.isCompleted,
  );
  const requiresChoice = !!quest?.choice && !quest.choice.selectedOptionKey;

  if (quest) {
    return {
      stage,
      phaseLabel: phaseLabel(stage),
      title: quest.title,
      summary: quest.summary,
      objective: requiresChoice
        ? (quest.choice?.selectionSummary ?? 'Choose how your journey begins.')
        : (incompleteObjective?.description ??
          'Claim the reward and continue.'),
      primaryAction: {
        label: requiresChoice
          ? 'Choose First Hunt'
          : (incompleteObjective?.presentation.actionLabel ?? 'Open Quests'),
        route: requiresChoice
          ? '/game/quests'
          : incompleteObjective?.presentation.destinationRoute ||
            '/game/quests',
      },
      optionalAction: optionalAction(stage),
      nextUnlock: nextUnlock(stage, characterLevel),
    };
  }

  return {
    stage,
    phaseLabel: phaseLabel(stage),
    title:
      stage === PlayerJourneyStage.Shenic
        ? 'Develop your Shenic build'
        : 'Continue the First Steps',
    summary:
      stage === PlayerJourneyStage.Shenic
        ? 'Fight in the newest available area, strengthen your loadout, and prepare for the next Shenic challenge.'
        : 'Open the Quest Journal to continue the guided introduction.',
    objective:
      stage === PlayerJourneyStage.Shenic
        ? 'Choose a current quest or return to the World Map.'
        : 'Open the Quest Journal and follow the highlighted objective.',
    primaryAction: {
      label:
        stage === PlayerJourneyStage.Shenic ? 'Open World Map' : 'Open Quests',
      route:
        stage === PlayerJourneyStage.Shenic
          ? '/game/world/shenic'
          : '/game/quests',
    },
    optionalAction: optionalAction(stage),
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
  if (stage >= PlayerJourneyStage.Shenic && characterLevel >= 10) {
    visibleItemIds.add('achievements');
    visibleItemIds.add('soulstone-archive');
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
    default:
      if (characterLevel < 10) return 'A second Essence slot at level 10';
      if (characterLevel < 20) return 'A third Essence slot at level 20';
      if (characterLevel < 30) return 'A fourth Essence slot at level 30';
      return 'The level 30 Shenic Beta milestone';
  }
}
