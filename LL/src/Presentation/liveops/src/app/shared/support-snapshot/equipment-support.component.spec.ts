import { TestBed } from '@angular/core/testing';
import { EquipmentSupportComponent } from './equipment-support.component';
import { EquipmentSupportSnapshot } from '../../liveops.models';

describe('EquipmentSupportComponent', () => {
  const empty = (): EquipmentSupportSnapshot => ({ rowLimit: 100, equipmentCount: 0, items: [] });

  it('shows unavailable data as unavailable rather than empty holdings', async () => {
    await TestBed.configureTestingModule({ imports: [EquipmentSupportComponent] }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentSupportComponent);
    fixture.componentRef.setInput('section', { isAvailable: false, source: 'Game database', fetchedAtUtc: '2026-09-03T00:00:00Z', message: 'Retry the snapshot.', data: null });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Retry the snapshot.');
    expect(fixture.nativeElement.textContent).not.toContain('No equipment');
  });

  it('shows authored rank and style alongside legacy equipment', async () => {
    await TestBed.configureTestingModule({ imports: [EquipmentSupportComponent] }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentSupportComponent);
    const data = empty();
    data.equipmentCount = 2;
    data.items = [
      { instanceId: 'gear', itemBaseId: 'sword', name: 'Named Sword', locations: ['Equipped: MainHand'], progression: {
        definitionId: 'named-sword', archetypeId: 'plain.sword', tier: 1, rank: 3, balanceVersion: 1, rarity: 'Rare',
        nativeStyleId: 'fury', activeStyleId: null, ownership: 'BoundPersonal', ownerId: 'owner', awardKind: 'ProtectedReward',
        sourceId: 'dungeon', awardId: 'run',
      } },
      { instanceId: 'legacy', itemBaseId: 'old', name: 'Old Tool', locations: ['Inventory'], progression: null },
    ];
    fixture.componentRef.setInput('section', { isAvailable: true, source: 'Game database', fetchedAtUtc: '2026-09-03T00:00:00Z', data });
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Rank 3');
    expect(text).toContain('Legacy equipment');
    expect(text).toContain('fury / Plain');
  });

  it('reports bounded samples explicitly', async () => {
    await TestBed.configureTestingModule({ imports: [EquipmentSupportComponent] }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentSupportComponent);
    fixture.componentRef.setInput('section', { isAvailable: true, source: 'Game database', fetchedAtUtc: '2026-09-03T00:00:00Z', data: { ...empty(), equipmentCount: 101 } });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('snapshot is truncated');
  });
});
