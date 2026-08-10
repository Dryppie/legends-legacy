import { QueryList } from '@angular/core';
import { TabComponent } from './tab/tab.component';
import { TabsComponent } from './tabs.component';

describe('TabsComponent', () => {
  it('opens the requested tab when content becomes available', () => {
    const component = tabsWithTwoPanes();
    component.selectedIndex = 1;

    component.ngAfterContentInit();

    expect(component.activeIndex()).toBe(1);
  });

  it('reports user tab changes', () => {
    const component = tabsWithTwoPanes();
    const selectedIndexes: number[] = [];
    component.selectedIndexChange.subscribe((index) =>
      selectedIndexes.push(index),
    );

    component.select(1);

    expect(component.activeIndex()).toBe(1);
    expect(selectedIndexes).toEqual([1]);
  });
});

function tabsWithTwoPanes(): TabsComponent {
  const component = new TabsComponent();
  const panes = new QueryList<TabComponent>();
  panes.reset([new TabComponent(), new TabComponent()]);
  component.panes = panes;
  return component;
}
