import { ComponentHarness } from '@angular/cdk/testing';

/** The TopBar (lg-top-bar): the player's name and, on narrow screens, the menu button that opens the rail drawer. */
export class LgTopBarHarness extends ComponentHarness {
  static hostSelector = 'lg-top-bar';

  private readonly menu = this.locatorForOptional('.lg-topbar__menu');

  /** The character's name: its `heading`. */
  async getHeading(): Promise<string> {
    return (await this.locatorFor('.lg-topbar__name')()).text();
  }

  async hasMenu(): Promise<boolean> {
    return !!(await this.menu());
  }

  /** A pointer press on the menu button: focus moves to it, then it is clicked. */
  async pressMenu(): Promise<void> {
    const menu = await this.menu();
    if (!menu) throw Error('This TopBar has no menu button.');
    await menu.focus();
    await menu.click();
  }

  async isMenuFocused(): Promise<boolean> {
    const menu = await this.menu();
    return !!menu && (await menu.isFocused());
  }
}
