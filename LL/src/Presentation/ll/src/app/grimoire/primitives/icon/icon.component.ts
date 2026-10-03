import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { LG_ICON_MARKERS, LG_ICONS, LgIconName } from '../../core/grimoire-icons';

/** Icon sizes (Foundations · Iconography): line icons at 16, 20 or 24px; 12px only for solid inline markers. */
export type LgIconSize = 12 | 16 | 20 | 24;

const ICON_SIZES = [12, 16, 20, 24];
const warned = new Set<string>();
function iconWarn(key: string, message: string): void {
  if (warned.has(key)) return;
  warned.add(key);
  console.warn(`lg-icon: ${message} (Foundations · Iconography).`);
}

/**
 * One of the game's icons, redrawn in currentColor. The host is the icon's box, `size` square (as rem, so it scales
 * with the reading size). An unknown name draws nothing and takes no room: the label beside it is the fallback, so a
 * missing icon never leaves a hole.
 */
@Component({
  selector: 'lg-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.lg-icon]': '!!icon()',
    '[class.is-missing]': '!icon()',
    '[style.--lg-icon-size]': "size() / 16 + 'rem'",
    '[attr.role]': "label() ? 'img' : null",
    '[attr.aria-label]': 'label() || null',
    '[attr.aria-hidden]': "label() ? null : 'true'",
  },
  template: `
    @if (icon(); as d) {
      <svg
        [attr.width]="size()"
        [attr.height]="size()"
        [attr.viewBox]="d.viewBox"
        fill="none"
        stroke="currentColor"
        [attr.stroke-width]="strokeWidth() ?? d.strokeWidth"
        stroke-linecap="round"
        stroke-linejoin="round"
        aria-hidden="true"
        focusable="false"
        [innerHTML]="body()"
      ></svg>
    }
  `,
  styleUrl: './icon.component.css',
})
export class LgIconComponent {
  readonly name = input.required<LgIconName>();
  /** In px, set as rem so the icon scales with the reading size. */
  readonly size = input<LgIconSize>(20);
  /** What the icon means, when it stands alone and carries meaning: its accessible name. Leave it unset beside words. */
  readonly label = input<string>();
  readonly strokeWidth = input<number>();

  private readonly sanitizer = inject(DomSanitizer);
  protected readonly icon = computed(() => {
    const name = this.name();
    const d = LG_ICONS[name];
    if (!d) {
      if (name) iconWarn('name:' + name, `"${name}" is not in the set; its label stands alone`);
      return null;
    }
    const s = this.size();
    if (ICON_SIZES.indexOf(s) < 0) iconWarn('size:' + s, `${s}px is off the scale; use 16, 20 or 24, or 12 for a marker`);
    else if (s < 16 && !LG_ICON_MARKERS.includes(name))
      iconWarn('small:' + name, `"${name}" is a line icon; line icons stop at 16px, and 12px is for markers`);
    return d;
  });
  // The icon bodies are static strings shipped with the app, never user data.
  protected readonly body = computed(() => {
    const d = this.icon();
    return d ? this.sanitizer.bypassSecurityTrustHtml(d.body) : '';
  });
}
