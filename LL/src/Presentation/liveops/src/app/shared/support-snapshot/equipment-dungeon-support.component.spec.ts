import { TestBed } from '@angular/core/testing';
import { EquipmentSupportDungeonRun, EquipmentSupportItem, EquipmentSupportSnapshot } from '../../liveops.models';
import { EquipmentSupportComponent } from './equipment-support.component';

describe('EquipmentSupportComponent dungeon evidence', () => {
  const item = (): EquipmentSupportItem => ({
    instanceId: 'reward-instance', itemBaseId: 'sword', name: 'Epic Sword', locations: [],
    progression: {
      definitionId: 'plain.sword.rarity.epic', archetypeId: 'plain.sword', tier: 2, rank: 1,
      balanceVersion: 91, rarity: 'Epic', nativeStyleId: null, activeStyleId: null,
      ownership: 'UnboundPersonal', ownerId: 'owner', awardKind: 'RandomDiscovery', sourceId: 'dungeon',
      awardId: 'run',
    },
  });
  const run = (): EquipmentSupportDungeonRun => ({
    runId: 'run', dungeonId: 'great_tree', name: 'Great Tree', status: 'Completed', currentRoomIndex: 4,
    createdAtUtc: '2026-09-03T08:00:00Z', completedAtUtc: '2026-09-03T09:00:00Z', rewardsClaimedAtUtc: null,
    rewardRowCount: 1,
    rewardRows: [{ rewardRowId: 'exact-reward-row', itemBaseId: 'sword', name: 'Epic Sword',
      itemType: 'Equipment', quantity: 1, source: 'dungeon-completion', equipment: item() }],
  });
  const snapshot = (dungeonRun?: EquipmentSupportDungeonRun | null): EquipmentSupportSnapshot => ({
    rowLimit: 100, equipmentCount: 0, items: [], dungeonRun,
  });
  async function render(data: EquipmentSupportSnapshot): Promise<string> {
    await TestBed.configureTestingModule({ imports: [EquipmentSupportComponent] }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentSupportComponent);
    fixture.componentRef.setInput('section', { isAvailable: true, source: 'Game database',
      fetchedAtUtc: '2026-09-03T09:00:00Z', data });
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('[data-testid="dungeon-equipment-support"]').textContent;
  }

  it('shows the exact saved random equipment reward', async () => {
    const text = await render(snapshot(run()));
    expect(text).toContain('Great Tree');
    expect(text).toContain('exact-reward-row');
    expect(text).toContain('plain.sword.rarity.epic');
    expect(text).toContain('Epic');
    expect(text).toContain('Rank 1');
  });

  it('distinguishes an old response without dungeon inspection', async () => {
    expect(await render(snapshot())).toContain('inspection was not included');
  });

  it('reports when no retained dungeon run exists', async () => {
    expect(await render(snapshot(null))).toContain('No retained dungeon run');
  });

  it('reports reward-row truncation independently of holdings', async () => {
    const saved = run(); saved.rewardRowCount = 101; saved.rewardRows = [];
    const text = await render(snapshot(saved));
    expect(text).toContain('Saved run reward rows (0 of 101)');
    expect(text).toContain('Run reward rows are truncated to 100');
  });
});
