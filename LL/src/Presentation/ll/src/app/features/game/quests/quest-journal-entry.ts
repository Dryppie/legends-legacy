import { QuestState, QuestStatus } from '../../../shared/models/quest';

export interface QuestJournalEntry {
  key: string;
  title: string;
  summary: string;
  category: string;
  sortOrder: number;
  status: QuestStatus;
  isChain: boolean;
  totalParts: number;
  quests: QuestState[];
}

export function buildQuestJournalEntries(
  quests: readonly QuestState[],
): QuestJournalEntry[] {
  const grouped = new Map<string, QuestState[]>();

  for (const quest of quests) {
    const key = quest.chain
      ? 'chain:' + quest.chain.id
      : 'quest:' + quest.questId;
    const group = grouped.get(key);
    if (group) {
      group.push(quest);
    } else {
      grouped.set(key, [quest]);
    }
  }

  return Array.from(grouped.entries()).flatMap(([key, groupedQuests]) => {
    const parts = [...groupedQuests].sort(
      (left, right) =>
        (left.chain?.step ?? 1) - (right.chain?.step ?? 1) ||
        left.sortOrder - right.sortOrder,
    );
    const first = parts[0];
    const chain = first.chain;
    if (!chain) {
      return [
        {
          key,
          title: first.title,
          summary: first.summary,
          category: first.category,
          sortOrder: first.sortOrder,
          status: first.status,
          isChain: false,
          totalParts: 1,
          quests: parts,
        },
      ];
    }

    const totalParts = Math.max(
      chain.totalSteps,
      ...parts.map((part) => part.chain?.totalSteps ?? 0),
    );
    const isCompleted =
      parts.length >= totalParts &&
      parts.every((part) => part.status === QuestStatus.Completed);
    const completedParts = parts.filter(
      (part) => part.status === QuestStatus.Completed,
    );
    const entryBase = {
      title: chain.title,
      summary: chain.description,
      category: first.category,
      sortOrder: Math.min(...parts.map((part) => part.sortOrder)),
      isChain: true,
      totalParts,
    };
    const entries: QuestJournalEntry[] = [];

    if (!isCompleted) {
      entries.push({
        ...entryBase,
        key: key + ':active',
        status: QuestStatus.Active,
        quests: parts,
      });
    }

    if (completedParts.length) {
      entries.push({
        ...entryBase,
        key: key + ':completed',
        status: QuestStatus.Completed,
        quests: completedParts,
      });
    }

    return entries;
  });
}

export function preferredQuestForEntry(entry: QuestJournalEntry): QuestState {
  return (
    entry.quests.find((quest) => quest.isPinned) ??
    entry.quests.find((quest) => quest.status === QuestStatus.Active) ??
    entry.quests[entry.quests.length - 1]
  );
}
