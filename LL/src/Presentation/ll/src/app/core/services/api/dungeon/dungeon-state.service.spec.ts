import { DungeonStateService } from './dungeon-state.service';

describe('DungeonStateService dungeon actions', () => {
  it('uses the canonical rest action at a Rest Site', () => {
    const state = Object.create(
      DungeonStateService.prototype,
    ) as DungeonStateService;
    spyOn(state, 'executeAction');

    state.restAtSite();

    expect(state.executeAction).toHaveBeenCalledOnceWith('rest');
  });
});
