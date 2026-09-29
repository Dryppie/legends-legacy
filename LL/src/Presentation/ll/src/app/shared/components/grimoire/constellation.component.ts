import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  input,
  output,
} from '@angular/core';
import {
  LgSigilComponent,
  LgSigilLabelPosition,
  LgSigilSize,
} from './sigil.component';

export interface LgConstellationItem {
  id: string;
  label: string;
  value: string | number;
  x: number;
  y: number;
  labelPosition?: LgSigilLabelPosition;
  size?: LgSigilSize;
  state?: 'default' | 'ready' | 'locked';
}

export interface LgConstellationRing {
  cx: number;
  cy: number;
  r: number;
  strong?: boolean;
}

/** Sigils placed on thin orbit rings, in a fixed-aspect coordinate space. */
@Component({
  selector: 'lg-constellation',
  imports: [LgSigilComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-constellation',
    role: 'group',
    '[attr.aria-label]': 'label()',
    '[style.aspect-ratio]': "width() + ' / ' + height()",
  },
  template: `
    <svg
      class="lg-constellation__rings"
      [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
      preserveAspectRatio="xMidYMid meet"
      aria-hidden="true"
      focusable="false"
    >
      @for (ring of rings(); track $index) {
        <circle [attr.cx]="ring.cx" [attr.cy]="ring.cy" [attr.r]="ring.r" [class.is-strong]="!!ring.strong" />
      }
      @for (node of nodes(); track $index) {
        <rect
          class="lg-constellation__knot"
          [attr.x]="node.x - 4"
          [attr.y]="node.y - 4"
          width="8"
          height="8"
          [attr.transform]="'rotate(45 ' + node.x + ' ' + node.y + ')'"
        />
      }
    </svg>
    @for (item of items(); track item.id) {
      <div
        class="lg-constellation__node"
        [class]="'lg-constellation__node--' + (item.labelPosition ?? 'right') + ' lg-constellation__node--' + (item.size ?? 'md')"
        [style.left.%]="(item.x / width()) * 100"
        [style.top.%]="(item.y / height()) * 100"
      >
        <lg-sigil
          [label]="item.label"
          [value]="item.value"
          [labelPosition]="item.labelPosition ?? 'right'"
          [size]="item.size ?? 'md'"
          [state]="item.id === selectedId() ? 'selected' : (item.state ?? 'default')"
          [interactive]="selectable()"
          (activate)="select.emit(item.id)"
        />
      </div>
    }
  `,
})
export class LgConstellationComponent {
  readonly items = input.required<readonly LgConstellationItem[]>();
  readonly width = input(1000);
  readonly height = input(700);
  readonly rings = input<readonly LgConstellationRing[]>([]);
  readonly nodes = input<readonly { x: number; y: number }[]>([]);
  readonly selectedId = input<string>();
  readonly selectable = input(true, { transform: booleanAttribute });
  readonly label = input('Attributes');
  readonly select = output<string>();
}
