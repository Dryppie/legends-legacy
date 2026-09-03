import { TestBed } from '@angular/core/testing';
import { EquipmentSupportDungeonRun, EquipmentSupportItem, EquipmentSupportSnapshot } from '../../liveops.models';
import { EquipmentSupportComponent } from './equipment-support.component';

describe('EquipmentSupportComponent dungeon evidence', () => {
  const item = (): EquipmentSupportItem => ({
    instanceId: 'reserved-instance', itemBaseId: 'sword', name: 'Frozen Sword', locations: [],
    progression: {
      definitionId: 'historical.target', archetypeId: 'plain.sword', tier: 1, rank: 1,
      balanceVersion: 91, rarity: 'Rare', nativeStyleId: 'old-style', activeStyleId: 'old-style',
      ownership: 'UnboundPersonal', ownerId: 'owner', awardKind: 'RandomDiscovery', sourceId: 'dungeon',
      awardId: 'run', baseSalvageScrap: 6, paidScrap: 0, paidCinders: 0, investments: [],
    },
  });
  const run = (): EquipmentSupportDungeonRun => ({
    runId: 'run', dungeonId: 'old-dungeon', name: 'Saved Dungeon', status: 'Active', currentRoomIndex: 4,
    createdAtUtc: '2026-09-03T08:00:00Z', completedAtUtc: null, rewardsClaimedAtUtc: null,
    commitment: { characterId: 'owner', runId: 'run', dungeonId: 'old-dungeon', poolId: 'old-pool',
      difficulty: 2, matchingChance: 0.125, guaranteeCompletions: 11, completionScrap: 37, target: item() },
    receipt: null, rewardRowCount: 0, rewardRows: [],
  });
  const snapshot = (dungeonRun?: EquipmentSupportDungeonRun | null): EquipmentSupportSnapshot => ({
    rowLimit: 100, equipmentCount: 0, pendingRewardCount: 0, progressTruncated: false,
    items: [], pendingRewards: [], protection: [], ordinary: [], learnedStyles: [], dungeonRun,
  });
  async function render(data: EquipmentSupportSnapshot): Promise<string> {
    await TestBed.configureTestingModule({ imports: [EquipmentSupportComponent] }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentSupportComponent);
    fixture.componentRef.setInput('section', { isAvailable: true, source: 'Game database',
      fetchedAtUtc: '2026-09-03T09:00:00Z', data });
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('[data-testid="dungeon-equipment-support"]').textContent;
  }

  it('labels entry terms as a possible reward without implying that completion occurred', async () => {
    const text = await render(snapshot(run()));
    expect(text).toContain('Frozen target; not an award');
    expect(text).toContain('historical.target');
    expect(text).toContain('Balance 91');
    expect(text).toContain('12.5% matching chance');
    expect(text).toContain('11 completion threshold');
    expect(text).toContain('37 completion Scrap');
    expect(text).toContain('No protected completion receipt recorded');
    expect(text).toContain('Current target selection and content changes do not replace');
  });

  it('shows claimed receipt evidence and exact reward IDs without summing overlapping awards', async () => {
    const saved = run();
    saved.status = 'RewardsClaimed';
    saved.rewardsClaimedAtUtc = '2026-09-03T09:00:00Z';
    saved.receipt = { runId: 'run', poolId: 'old-pool', securedAtUtc: '2026-09-03T08:55:00Z',
      claimedAtUtc: saved.rewardsClaimedAtUtc, previousProgress: 7, progress: 0, scrap: 37, equipment: item() };
    saved.rewardRowCount = 1;
    saved.rewardRows = [{ rewardRowId: 'exact-reward-row', itemBaseId: 'sword', name: 'Frozen Sword',
      itemType: 'Equipment', quantity: 1, source: 'saved-source', equipment: item() }];
    const text = await render(snapshot(saved));
    expect(text).toContain('RewardsClaimed');
    expect(text).toContain('Receipt claimed');
    expect(text).toContain('7 → 0');
    expect(text).toContain('exact-reward-row');
    expect(text).toContain('reserved-instance');
    expect(text).toContain('do not add them together');
  });

  it('distinguishes an old response from confirmation that no run exists', async () => {
    const text = await render(snapshot());
    expect(text).toContain('inspection was not included');
    expect(text).not.toContain('No retained dungeon run');
  });

  it('explains that receipts can survive a removed run', async () => {
    const text = await render(snapshot(null));
    expect(text).toContain('No retained dungeon run');
    expect(text).toContain('receipts can still appear below');
  });

  it('does not invent a commitment for a legacy run', async () => {
    const saved = run(); saved.commitment = null;
    const text = await render(snapshot(saved));
    expect(text).toContain('No equipment commitment recorded');
    expect(text).not.toContain('No equipment target was committed');
  });

  it('retains Scrap terms for a commitment without an equipment target', async () => {
    const saved = run(); saved.commitment!.target = null;
    const text = await render(snapshot(saved));
    expect(text).toContain('No equipment target was committed');
    expect(text).toContain('37 completion Scrap');
  });

  it('reports reward-row truncation independently of holdings', async () => {
    const saved = run(); saved.rewardRowCount = 101;
    const text = await render(snapshot(saved));
    expect(text).toContain('Saved run reward rows (0 of 101)');
    expect(text).toContain('Run reward rows are truncated to 100');
  });
});
