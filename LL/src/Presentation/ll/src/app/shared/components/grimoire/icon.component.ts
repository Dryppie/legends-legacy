import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { LG_ICONS, LgIconName } from './grimoire-icons';

/** One of the game's sidebar icons, drawn in currentColor. */
@Component({
  selector: 'lg-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <svg
      class="lg-icon"
      [attr.width]="size()"
      [attr.height]="size()"
      [attr.viewBox]="icon().viewBox"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth() ?? icon().strokeWidth"
      stroke-linecap="round"
      stroke-linejoin="round"
      focusable="false"
      [attr.role]="title() ? 'img' : null"
      [attr.aria-label]="title() ?? null"
      [attr.aria-hidden]="title() ? null : 'true'"
      [innerHTML]="body()"
    ></svg>
  `,
})
export class LgIconComponent {
  readonly name = input.required<LgIconName>();
  readonly size = input(20);
  /** Set only when the icon stands alone and carries meaning. */
  readonly title = input<string>();
  readonly strokeWidth = input<number>();

  private readonly sanitizer = inject(DomSanitizer);
  protected readonly icon = computed(() => LG_ICONS[this.name()]);
  // The icon bodies are static strings shipped with the app, never user data.
  protected readonly body = computed(() =>
    this.sanitizer.bypassSecurityTrustHtml(this.icon().body),
  );
}
