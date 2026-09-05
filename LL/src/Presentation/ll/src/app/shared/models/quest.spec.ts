import { isQuestReadyToTurnIn, QuestState, QuestStatus } from './quest';

describe('quest turn-in readiness', () => {
  const ready = {
    status: QuestStatus.Active,
    objectives: [{ isCompleted: true }],
  } as QuestState;

  it('keeps completed objectives ready until the quest is turned in', () => {
    expect(isQuestReadyToTurnIn(ready)).toBeTrue();
    expect(
      isQuestReadyToTurnIn({ ...ready, status: QuestStatus.Completed }),
    ).toBeFalse();
  });

  it('requires objectives and a confirmed choice when applicable', () => {
    expect(isQuestReadyToTurnIn(null)).toBeFalse();
    expect(isQuestReadyToTurnIn({ ...ready, objectives: [] })).toBeFalse();
    expect(
      isQuestReadyToTurnIn({
        ...ready,
        objectives: [{ isCompleted: false }],
      } as QuestState),
    ).toBeFalse();
    expect(
      isQuestReadyToTurnIn({
        ...ready,
        choice: { selectedOptionKey: null },
      } as QuestState),
    ).toBeFalse();
  });
});
