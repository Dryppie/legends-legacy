import { ComponentHarness, TestElement, TestKey } from '@angular/cdk/testing';

/** The screen frame (lg-game-shell): the rail drawer on narrow screens, and where the floating Chronicle sits. */
export class LgGameShellHarness extends ComponentHarness {
  static hostSelector = 'lg-game-shell';

  private readonly shell = this.locatorFor('.lg-shell');
  private readonly main = this.locatorFor('.lg-shell__main');
  private readonly railFocusables = this.locatorForAll(
    '.lg-shell__rail a[href]',
    '.lg-shell__rail button',
    '.lg-shell__rail [tabindex]',
  );
  private readonly focusables = this.locatorForAll(
    'a[href]',
    'button',
    'input',
    '[tabindex]',
  );
  private readonly chronicleDrawer = this.locatorForOptional(
    '.lg-shell__chronicle',
  );

  /** Whether the rail drawer is open (narrow screens). */
  async isRailOpen(): Promise<boolean> {
    return (await this.shell()).hasClass('is-rail-open');
  }

  /** Whether the stage (and the TopBar in it) is inert, as it is behind the open drawer. */
  async isStageInert(): Promise<boolean> {
    return (await (await this.main()).getAttribute('inert')) !== null;
  }

  /** The text of the rail item that has focus, or null when focus is not in the rail. */
  async getFocusedRailItem(): Promise<string | null> {
    for (const el of await this.railFocusables()) {
      if (await el.isFocused()) return el.text();
    }
    return null;
  }

  /** A key pressed where focus is inside the shell. */
  async pressKey(key: TestKey | string): Promise<void> {
    const focused = await this.focusedElement();
    if (!focused) throw Error('Focus is not inside the shell.');
    await focused.sendKeys(key);
  }

  /** The floating Chronicle's place: px from the shell's left and bottom edges. */
  async getChroniclePosition(): Promise<{ left: number; bottom: number }> {
    const drawer = await this.chronicleDrawer();
    if (!drawer) throw Error('The shell holds no Chronicle.');
    const [shell, chat] = await Promise.all([
      (await this.shell()).getDimensions(),
      drawer.getDimensions(),
    ]);
    return {
      left: Math.round(chat.left - shell.left),
      bottom: Math.round(shell.top + shell.height - (chat.top + chat.height)),
    };
  }

  private async focusedElement(): Promise<TestElement | null> {
    for (const el of await this.focusables()) {
      if (await el.isFocused()) return el;
    }
    return null;
  }
}
