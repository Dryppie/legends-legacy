import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { lgFormatNumber } from './grimoire-core';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';
import { LgMeterComponent } from './meter.component';

export interface LgLevelPlateStat {
  label: string;
  value: number | string;
  icon?: LgIconName;
}

/** A large level numeral with its progress bar and up to two headline stats. */
@Component({
  selector: 'lg-level-plate',
  imports: [LgIconComponent, LgMeterComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-levelplate' },
  template: `
    <div class="lg-levelplate__row">
      <div class="lg-levelplate__level">
        <span class="lg-levelplate__kicker">{{ kicker() }}</span>
        <span class="lg-levelplate__num">{{ format(level()) }}</span>
      </div>
      @if (aside().length) {
        <ul class="lg-levelplate__aside">
          @for (stat of aside(); track stat.label) {
            <li>
              @if (stat.icon) {
                <lg-icon [name]="stat.icon" [size]="16" />
              }
              <span class="lg-levelplate__aside-label">{{ stat.label }}</span>
              <b>{{ format(stat.value) }}</b>
            </li>
          }
        </ul>
      }
    </div>
    @if (xpMax() !== undefined) {
      <div class="lg-levelplate__xp">
        <lg-meter
          tone="xp"
          [value]="xp()"
          [max]="xpMax() ?? 0"
          [showValue]="false"
          [ariaLabel]="xpUnit()"
        />
        <span class="lg-levelplate__xptext">
          {{ format(xp()) }} / {{ format(xpMax()) }} {{ xpUnit() }}
        </span>
      </div>
    }
  `,
})
export class LgLevelPlateComponent {
  readonly level = input.required<number | string>();
  readonly kicker = input('Level');
  readonly xp = input(0);
  readonly xpMax = input<number>();
  /** Unit after the progress numbers ("EXP", "Combat XP", "Essence"). */
  readonly xpUnit = input('EXP');
  readonly aside = input<readonly LgLevelPlateStat[]>([]);
  protected readonly format = lgFormatNumber;
}
