import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrentDungeonComponent } from '../../shared/components/current-dungeon/current-dungeon.component';
import { CurrentRaidComponent } from '../../shared/components/current-raid/current-raid.component';
import { LgObjectiveComponent, LgTrackComponent } from '../../shared/components/grimoire';
import { QuestObjectiveGrimoireComponent } from './quest-objective-grimoire.component';

/**
 * The TopBar's centre (D-110): one "now" thing. A dungeon run's depth Track while a run is in progress, else the raid
 * the character is in, else the pinned quest. Each run links to its screen, as the old header's did.
 */
@Component({
  selector: 'app-top-center-grimoire',
  imports: [RouterLink, LgTrackComponent, LgObjectiveComponent, QuestObjectiveGrimoireComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    @if (dungeon.hasActiveDungeon()) {
      <a class="tc-run" routerLink="/game/world/dungeon" [attr.aria-label]="dungeon.statusLabel() + ': ' + dungeon.dungeonTitle() + ', ' + dungeon.progressText()">
        @if (dungeon.totalDepths() > 1) {
          <lg-track
            [steps]="dungeon.totalDepths()"
            [current]="dungeon.currentDepthNumber() - 1"
            [startLabel]="dungeon.dungeonTitle()"
            [endLabel]="dungeon.progressText()"
            [label]="dungeon.statusLabel()"
          />
        } @else {
          <lg-objective [kicker]="dungeon.statusLabel()" [title]="dungeon.dungeonTitle()" [objective]="dungeon.progressText()" />
        }
      </a>
    } @else if (raid.activeRaid(); as run) {
      <a class="tc-run" [routerLink]="['/game/world/raid', run.id]">
        <lg-objective [kicker]="raid.statusLabel()" [title]="raid.raidTitle()" [objective]="raid.progressText()" />
      </a>
    } @else {
      <app-quest-objective-grimoire />
    }
  `,
  styles: `
    .tc-run {
      display: block;
      width: 100%;
      min-width: 0;
      border-radius: var(--radius-control);
      color: inherit;
      text-decoration: none;
    }
    .tc-run:hover {
      background: var(--surface-raised);
    }
    .tc-run:focus-visible {
      outline: 2px solid transparent;
      box-shadow: var(--focus-ring);
    }
  `,
})
export class TopCenterGrimoireComponent {
  // The old header widgets' reckoning (their signals and labels), created here in this injection context and used as
  // plain state holders, so the words stay the same in both looks.
  protected readonly dungeon = new CurrentDungeonComponent();
  protected readonly raid = new CurrentRaidComponent();
}
