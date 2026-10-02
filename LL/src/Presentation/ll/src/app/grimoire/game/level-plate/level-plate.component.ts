import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LgIconName } from '../../core/grimoire-icons';
import { LgMeterComponent } from '../../primitives/meter/meter.component';
import { LgValuePipe } from '../../core/grimoire-numerals';
import { LG_NBSP, lgFormatFraction, lgFormatUnit } from '../../core/grimoire-format';

export interface LgLevelPlateStat {
  label: string;
  value: number | string;
  /** A unit for a numeric value: "%", "s", "HP/5s". */
  unit?: string;
  icon?: LgIconName;
}

/** The level display: a large level numeral with its progress bar and up to two headline stats. */
@Component({
  selector: 'lg-level-plate',
  imports: [LgIconComponent, LgMeterComponent, LgValuePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div class="lg-levelplate">
      <div class="lg-levelplate__row">
        <div class="lg-levelplate__level">
          <span class="lg-levelplate__kicker">{{ kicker() || 'Level' }}</span>
          <span class="lg-levelplate__num">{{ level() }}</span>
        </div>
        @if (aside()?.length) {
          <ul class="lg-levelplate__aside">
            @for (stat of aside(); track stat.label) {
              <li>
                @if (stat.icon) {
                  <lg-icon [name]="stat.icon" [size]="16" />
                }
                <span class="lg-levelplate__aside-label">{{ stat.label }}</span>
                @let v = (stat.unit ? unit(stat.value, stat.unit) : stat.value) | lgValue;
                <b>{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></b>
              </li>
            }
          </ul>
        }
      </div>
      @if (xpMax() != null) {
        <div class="lg-levelplate__xp">
          <lg-meter tone="xp" [value]="xp() || 0" [max]="xpMax()!" [showValue]="false" [ariaLabel]="xpUnit() || 'Experience'" />
          <span class="lg-levelplate__xptext"
            >{{ fraction(xp() || 0, xpMax()) }}<span class="lg-unit">{{ nbsp }}{{ xpUnit() || 'EXP' }}</span></span
          >
        </div>
      }
    </div>
  `,
})
export class LgLevelPlateComponent {
  readonly level = input.required<number | string>();
  readonly kicker = input<string>('Level');
  readonly xp = input<number>(0);
  readonly xpMax = input<number>();
  /** Unit after the progress numbers ("EXP", "Combat XP", "Essence"). */
  readonly xpUnit = input<string>();
  readonly aside = input<readonly LgLevelPlateStat[]>();

  protected readonly nbsp = LG_NBSP;
  protected readonly fraction = lgFormatFraction;
  protected readonly unit = lgFormatUnit;
}
