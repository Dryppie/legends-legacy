import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader, TestKey } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_MENU } from './menu.component';
import { LgMenuHarness } from '../../testing/menu.harness';

@Component({
  imports: [...LG_MENU],
  template: `
    <button type="button" class="trigger" [lgMenuTrigger]="actions">
      Actions
    </button>
    <ng-template #actions>
      <lg-menu label="Ashen Blade">
        <button lgMenuItem (triggered)="done.push('equip')">Equip</button>
        <button lgMenuItem disabled (triggered)="done.push('sell')">
          Sell
        </button>
        <button lgMenuItem (triggered)="done.push('salvage')">Salvage</button>
        <button lgMenuItemCheckbox [(checked)]="favourite">Favourite</button>
      </lg-menu>
    </ng-template>
  `,
})
class MenuHost {
  readonly done: string[] = [];
  favourite = false;
}

describe('LgMenuComponent', () => {
  let fixture: ComponentFixture<MenuHost>;
  let page: HarnessLoader;

  beforeEach(() => {
    fixture = TestBed.createComponent(MenuHost);
    fixture.detectChanges();
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => fixture.destroy());

  const trigger = () =>
    fixture.nativeElement.querySelector('.trigger') as HTMLElement;
  async function open(): Promise<LgMenuHarness> {
    trigger().focus();
    trigger().click();
    fixture.detectChanges();
    return page.getHarness(LgMenuHarness.with({ label: 'Ashen Blade' }));
  }

  it('its trigger says it opens a menu; a press opens it with its items', async () => {
    expect(trigger().getAttribute('aria-haspopup')).toBe('menu');
    const menu = await open();

    expect(trigger().getAttribute('aria-expanded')).toBe('true');
    expect(await menu.getItemTexts()).toEqual([
      'Equip',
      'Sell',
      'Salvage',
      'Favourite',
    ]);
    expect(await (await menu.host()).getAttribute('role')).toBe('menu');
    expect(await menu.isItemDisabled('Sell')).toBeTrue();
    expect(await menu.isChecked('Favourite')).toBeFalse();
    expect(await menu.isChecked('Equip')).toBeNull();
  });

  it('a press on an item does it and closes the menu', async () => {
    const menu = await open();
    await menu.pressItem('Salvage');

    expect(fixture.componentInstance.done).toEqual(['salvage']);
    expect(await page.getHarnessOrNull(LgMenuHarness)).toBeNull();
  });

  it('the keys: the arrows pass over a disabled item, Enter does the item, and focus goes back to the trigger', async () => {
    const menu = await open();
    await menu.pressKey(TestKey.DOWN_ARROW);
    expect(await menu.getFocusedItemText()).toBe('Equip');
    await menu.pressKey(TestKey.DOWN_ARROW);
    expect(await menu.getFocusedItemText()).toBe('Salvage');

    await menu.pressKey(TestKey.ENTER);
    expect(fixture.componentInstance.done).toEqual(['salvage']);
    expect(await page.getHarnessOrNull(LgMenuHarness)).toBeNull();
    expect(document.activeElement).toBe(trigger());
  });

  it('Escape closes it, and focus goes back to the trigger', async () => {
    const menu = await open();
    await menu.pressKey(TestKey.DOWN_ARROW);
    await menu.pressKey(TestKey.ESCAPE);

    expect(await page.getHarnessOrNull(LgMenuHarness)).toBeNull();
    expect(document.activeElement).toBe(trigger());
  });

  it('a checkbox item turns over, and [(checked)] follows it', async () => {
    let menu = await open();
    await menu.pressItem('Favourite');
    expect(fixture.componentInstance.favourite).toBeTrue();

    menu = await open();
    expect(await menu.isChecked('Favourite')).toBeTrue();
  });
});
