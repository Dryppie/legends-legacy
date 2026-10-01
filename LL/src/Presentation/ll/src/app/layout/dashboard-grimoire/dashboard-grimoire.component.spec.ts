import { ActivatedRoute } from '@angular/router';
import { isGrimoireView } from './dashboard-grimoire.component';

describe('isGrimoireView', () => {
  const route = (data: Record<string, unknown>, child: unknown = null) => ({ snapshot: { data }, firstChild: child }) as unknown as ActivatedRoute;

  it('is true when a route in the active chain says grimoireView', () => {
    expect(isGrimoireView(route({}, route({}, route({ grimoireView: true }))))).toBeTrue();
  });

  it('is false for a legacy screen', () => {
    expect(isGrimoireView(route({}, route({ title: 'Inventory' })))).toBeFalse();
  });
});
