import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  contentChildren,
  input,
} from '@angular/core';
import { LgHeadingComponent } from '../../primitives/heading/heading.component';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LgPresenceComponent } from '../presence/presence.component';

/** One fact in a ProfileIdentity: `<div lgProfileFact label="Guild">…</div>`. The value is the content. */
@Component({
  selector: 'div[lgProfileFact]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<dt>{{ label() }}</dt><dd><ng-content /></dd>`,
  styles: `
    :host { display: flex; flex-wrap: wrap; align-items: center; gap: var(--lg-space-2); min-width: 0; }
    dt { color: var(--lg-ink-muted); }
    dd { margin: 0; display: inline-flex; flex-wrap: wrap; align-items: center; gap: var(--lg-space-2); color: var(--lg-ink); font-variant-numeric: tabular-nums; }
  `,
})
export class LgProfileFactComponent {
  readonly label = input.required<string>();
}

export interface LgProfilePresence {
  online: boolean;
  lastSeen?: string | null;
}

/**
 * Who a player is, at the head of a profile (D-099): an eyebrow, the name with the Nobility crown and, for another
 * player, Presence; then the facts — Guild, Essences, Achievement Points, Nobility — as `div[lgProfileFact]` children.
 * It sits in a Banner's body.
 */
@Component({
  selector: 'lg-profile-identity',
  imports: [LgHeadingComponent, LgIconComponent, LgPresenceComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-identity' },
  template: `
    @if (eyebrow()) {
      <span class="lg-identity__eyebrow">{{ eyebrow() }}</span>
    }
    <div class="lg-identity__name">
      @if (noble()) {
        <span class="lg-identity__noble" role="img" aria-label="Noble" title="Active Nobility"
          ><lg-icon name="nobility" [size]="16"
        /></span>
      }
      @switch (headingTag()) {
        @case ('h1') {
          <h1 lgHeading="screen" [attr.id]="headingId() ?? null">{{ name() }}</h1>
        }
        @case ('h3') {
          <h3 lgHeading="screen" [attr.id]="headingId() ?? null">{{ name() }}</h3>
        }
        @default {
          <h2 lgHeading="screen" [attr.id]="headingId() ?? null">{{ name() }}</h2>
        }
      }
      @if (presence(); as p) {
        <lg-presence [online]="p.online" [lastSeen]="p.lastSeen" />
      }
    </div>
    @if (facts().length) {
      <dl class="lg-identity__facts"><ng-content /></dl>
    }
  `,
  styleUrl: './profile-identity.component.css',
})
export class LgProfileIdentityComponent {
  /** "Combat Profile"; "Viewing player" on someone else's. */
  readonly eyebrow = input<string>();
  readonly name = input.required<string>();
  /** Active Nobility: the crown before the name (D-066). */
  readonly noble = input(false, { transform: booleanAttribute });
  /** Another player's: Presence after the name. */
  readonly presence = input<LgProfilePresence | null>();
  /** The name's heading element. Default h2. */
  readonly headingTag = input<'h1' | 'h2' | 'h3'>('h2', { alias: 'as' });
  readonly headingId = input<string>();

  protected readonly facts = contentChildren(LgProfileFactComponent);
}
