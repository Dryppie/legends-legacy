import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { GuildMissionsComponent } from './guild-missions.component';

describe('GuildMissionsComponent', () => {
  it('tracks when the missions view is visible', () => {
    const state = {
      missions: () => null,
      loading: () => false,
      activateMissionsView: jasmine.createSpy('activateMissionsView'),
      deactivateMissionsView: jasmine.createSpy('deactivateMissionsView'),
    } as unknown as GuildStateService;
    const component = new GuildMissionsComponent(state);

    component.ngOnInit();
    component.ngOnDestroy();

    expect(state.activateMissionsView).toHaveBeenCalledTimes(1);
    expect(state.deactivateMissionsView).toHaveBeenCalledTimes(1);
  });
});
