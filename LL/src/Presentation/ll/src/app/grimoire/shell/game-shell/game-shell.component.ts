import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EnvironmentInjector,
  Signal,
  afterNextRender,
  computed,
  contentChild,
  effect,
  forwardRef,
  inject,
  input,
  model,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { FocusTrap, FocusTrapFactory } from '@angular/cdk/a11y';
import { LgToastOutletComponent } from '../../primitives/toast/toaster.service';
import {
  LG_SHELL,
  LgChatLayout,
  LgChroniclePosition,
  LgShellApi,
  lgCx,
  lgUniqueId,
} from '../../core/grimoire-core';

/** What a GameShell tells its regions. */
export abstract class LgShellFrame {
  abstract readonly chatLayout: Signal<LgChatLayout>;
  /** The rail is open as a drawer (narrow screens). */
  abstract readonly railOpen: Signal<boolean>;
  /** The frame paints a backdrop (D-107). */
  abstract readonly hasBackdrop: Signal<boolean>;
  abstract readonly hasFolio: Signal<boolean>;
  abstract readonly hasChat: Signal<boolean>;
  /** The floating Chronicle has been moved: px from the shell's left and bottom edges. */
  abstract readonly chroniclePlace: Signal<LgChroniclePosition | null>;
}

/**
 * The shell's rail: a NavRail. Under 60rem it is a drawer the TopBar's menu opens, sliding in from its edge; open, it
 * holds focus until it closes.
 */
@Component({
  selector: 'lg-shell-rail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-shell__rail',
    '[class.is-open]': 'frame.railOpen()',
    '[class.on-backdrop]': 'frame.hasBackdrop()',
  },
  template: '<ng-content />',
  styles: `
    :host { display: block; grid-area: rail; min-height: 0; position: relative; z-index: var(--lg-z-chrome); }
    /* Over the frame's backdrop the compact rail's header band takes the same rule as the TopBar (lg-nav-rail-header). */
    :host(.on-backdrop) { --lg-rail-header-rule: var(--lg-border-hairline) solid var(--lg-line); }
    @container lg-shell (max-width: 59.9375rem) {
      :host {
        position: fixed; top: 0; bottom: 0; left: 0; width: min(84vw, 18.75rem); z-index: var(--lg-z-overlay);
        /* Spatial: the drawer slides in from its edge over duration-base on ease-enter and leaves over duration-fast on
           ease-exit. Once it has left it is hidden, so nothing off-screen can take focus. */
        transform: translateX(-102%); visibility: hidden; box-shadow: var(--lg-shadow-panel);
        transition: transform var(--lg-duration-fast) var(--lg-ease-exit), visibility 0s linear var(--lg-duration-fast);
      }
      :host(.is-open) {
        transform: none; visibility: visible;
        transition: transform var(--lg-duration-base) var(--lg-ease-enter), visibility 0s;
      }
    }
  `,
})
export class LgShellRailComponent {
  protected readonly frame = inject(LgShellFrame);
}

/** The shell's top band: a TopBar, over the top of the stage (or, over a backdrop, a band of the frame above it). */
@Component({
  selector: 'lg-shell-top',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-shell__top', '[class.on-backdrop]': 'frame.hasBackdrop()' },
  template: '<ng-content />',
  styles: `
    :host { display: block; position: absolute; top: 0; left: 0; right: 0; z-index: var(--lg-z-chrome); }
    /* The frame lines up (D-115). Over a backdrop the TopBar is a band of the frame, like the rail: Level 1's surface
       with a hairline under it, and the stage starts below it, so nothing scrolls beneath a translucent band (D-102). */
    :host(.on-backdrop) {
      position: relative; grid-row: 1;
      --lg-topbar-bg: var(--lg-surface); --lg-topbar-rule: var(--lg-border-hairline) solid var(--lg-line);
    }
  `,
})
export class LgShellTopComponent {
  protected readonly frame = inject(LgShellFrame);
}

/** The shell's key hints: a KeyHints at the stage's bottom end corner, on wide screens only. */
@Component({
  selector: 'lg-shell-hints',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-shell__hints',
    '[class.beside-chat]': "frame.hasChat() && frame.chatLayout() === 'floating'",
  },
  template: '<ng-content />',
  styles: `
    :host { display: block; position: absolute; right: var(--lg-space-6); bottom: var(--lg-space-4); z-index: var(--lg-z-chrome); }
    /* The floating Chronicle takes the end corner. */
    :host(.beside-chat) { right: auto; left: var(--lg-space-6); }
    @container lg-shell (max-width: 59.9375rem) {
      :host { display: none; }
    }
  `,
})
export class LgShellHintsComponent {
  protected readonly frame = inject(LgShellFrame);
}

/** The shell's Folio column, at the right. Under 60rem it follows the stage. */
@Component({
  selector: 'lg-shell-folio',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-shell__folio', '[attr.inert]': "frame.railOpen() ? '' : null" },
  template: '<ng-content />',
  styles: `
    :host { display: block; grid-area: folio; min-height: 0; position: relative; z-index: var(--lg-z-folio); }
    @container lg-shell (max-width: 59.9375rem) {
      :host { --lg-folio-height: auto; }
    }
  `,
})
export class LgShellFolioComponent {
  protected readonly frame = inject(LgShellFrame);
}

/**
 * The shell's Chronicle, placed by the chat layout: docked in the right-hand column, or a floating drawer over the
 * stage, draggable by its grip. Under 60rem both become a bottom dock. It sets the Chronicle's --lg-chronicle-*
 * properties for each place.
 */
@Component({
  selector: 'lg-shell-chronicle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-shell__chronicle',
    '[class.is-docked]': "frame.chatLayout() !== 'floating'",
    '[class.is-floating]': "frame.chatLayout() === 'floating'",
    '[class.is-placed]': 'placed()',
    '[class.beside-folio]': 'frame.hasFolio()',
    '[class.on-backdrop]': 'frame.hasBackdrop()',
    '[style.--lg-float-left]': 'placed() ? frame.chroniclePlace()!.left + "px" : null',
    '[style.--lg-float-bottom]': 'placed() ? frame.chroniclePlace()!.bottom + "px" : null',
    '[attr.inert]': "frame.railOpen() ? '' : null",
  },
  template: '<ng-content />',
  styleUrl: './shell-chronicle.component.css',
})
export class LgShellChronicleComponent {
  protected readonly frame = inject(LgShellFrame);
  protected readonly placed = computed(
    () => this.frame.chatLayout() === 'floating' && !!this.frame.chroniclePlace(),
  );
}

/**
 * The screen frame for every in-game screen. Its regions are components:
 *
 *   <lg-game-shell [chatLayout]="chatLayout()" [backdrop]="backdrop">
 *     <lg-shell-rail><lg-nav-rail>…</lg-nav-rail></lg-shell-rail>
 *     <lg-shell-top><lg-top-bar heading="Aldric" showMenu>…</lg-top-bar></lg-shell-top>
 *     <lg-shell-folio><lg-folio>…</lg-folio></lg-shell-folio>
 *     <lg-shell-hints><lg-key-hints [hints]="hints" /></lg-shell-hints>
 *     <lg-shell-chronicle><lg-chronicle … /></lg-shell-chronicle>
 *     …the stage: a Stage or a Page…
 *   </lg-game-shell>
 *
 * `chatLayout` is the player's Chat layout setting: `docked` puts the Chronicle on the right (its own column at
 * 96rem+, under the Folio below that); `floating` makes it a draggable drawer over the stage. Under 60rem both become
 * a bottom dock, and the rail becomes a drawer: a TopBar with `showMenu` inside the shell opens it. The drawer holds
 * focus while it is open (the CDK's focus trap), makes the rest inert, closes on Escape and returns focus.
 */
@Component({
  selector: 'lg-game-shell',
  imports: [LgToastOutletComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: LG_SHELL, useExisting: forwardRef(() => LgGameShellComponent) },
    { provide: LgShellFrame, useExisting: forwardRef(() => LgGameShellComponent) },
  ],
  host: { class: 'lg-shellhost', '[style.height]': 'cssHeight()' },
  template: `
    <div #shell [class]="shellClass()" [style.--lg-shell-backdrop]="backdropUrl()">
      <a class="lg-skip" [attr.href]="'#' + mainId">Skip to content</a>
      <ng-content select="lg-shell-rail" />
      <div class="lg-shell__scrim" aria-hidden="true" (click)="closeRail()"></div>
      <main class="lg-shell__main" [id]="mainId" tabindex="-1" [attr.inert]="railOpen() ? '' : null">
        <ng-content select="lg-shell-top" />
        <div class="lg-shell__stage">
          <lg-toast-outlet class="lg-shell__toasts" />
          <ng-content />
          <ng-content select="lg-shell-hints" />
        </div>
      </main>
      <ng-content select="lg-shell-folio" />
      <ng-content select="lg-shell-chronicle" />
    </div>
  `,
  styleUrl: './game-shell.component.css',
})
export class LgGameShellComponent implements LgShellApi, LgShellFrame {
  readonly chatLayout = input<LgChatLayout>('docked');
  /** Floating drawer position (px from the shell's bottom-left). Two-way bindable to persist it. */
  readonly chroniclePosition = model<LgChroniclePosition | null>(null);
  /** CSS height (a number is px); defaults to 100vh. */
  readonly height = input<string | number>();
  /** The frame's backdrop (D-107): an image URL painted at Level 0 behind the rail, the stage and the docked Chronicle. */
  readonly backdrop = input<string>();

  private readonly railRegion = contentChild(LgShellRailComponent, { read: ElementRef });
  private readonly topRegion = contentChild(LgShellTopComponent);
  private readonly folioRegion = contentChild(LgShellFolioComponent);
  private readonly chronicleRegion = contentChild(LgShellChronicleComponent, { read: ElementRef });
  private readonly shellRef = viewChild.required<ElementRef<HTMLElement>>('shell');
  private readonly environment = inject(EnvironmentInjector);
  private readonly focusTraps = inject(FocusTrapFactory);

  protected readonly mainId = lgUniqueId('lgmain');
  readonly railOpen = signal(false);
  readonly hasBackdrop = computed(() => !!this.backdrop());
  readonly hasFolio = computed(() => !!this.folioRegion());
  readonly hasChat = computed(() => !!this.chronicleRegion());
  readonly chroniclePlace = computed(() => (this.chatLayout() === 'floating' ? this.chroniclePosition() : null));
  private readonly chatCollapsed = signal(false);
  private opener: HTMLElement | null = null;

  protected readonly cssHeight = computed(() => {
    const h = this.height();
    return h == null ? null : typeof h === 'number' ? h + 'px' : h;
  });
  protected readonly shellClass = computed(() => {
    const chat = this.hasChat();
    const layout = this.chatLayout() === 'floating' ? 'floating' : 'docked';
    return lgCx(
      'lg-shell',
      this.hasFolio() && 'has-folio',
      !!this.topRegion() && 'has-top',
      chat && 'has-chat',
      chat && 'is-chat-' + layout,
      chat && this.chatCollapsed() && 'is-chat-collapsed',
      this.railOpen() && 'is-rail-open',
      this.hasBackdrop() && 'has-backdrop',
    );
  });
  protected readonly backdropUrl = computed(() => {
    const b = this.backdrop();
    return b ? `url("${b}")` : null;
  });

  constructor() {
    // The rail drawer holds focus while it is open (the CDK's focus trap, from its first control), closes on Escape and
    // returns focus to its opener. An Escape something above the drawer took first (the tip, an overlay opened from it)
    // leaves the drawer open: the tip and the CDK overlays mark the keys they use, and hear them before the document.
    effect((onCleanup) => {
      if (!this.railOpen()) return;
      const rail = untracked(() => this.railRegion())?.nativeElement as HTMLElement | undefined;
      let trap: FocusTrap | null = null;
      if (rail) {
        trap = this.focusTraps.create(rail);
        // After the render that shows the drawer: hidden, it can't take focus.
        trap.focusInitialElementWhenReady();
      }
      const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Escape' && !e.defaultPrevented) {
          e.preventDefault();
          e.stopPropagation();
          this.railOpen.set(false);
        }
      };
      document.addEventListener('keydown', onKey);
      onCleanup(() => {
        document.removeEventListener('keydown', onKey);
        trap?.destroy();
        // After the drawer has closed and the rest is no longer inert.
        const o = this.opener;
        afterNextRender(
          () => {
            if (o && o.isConnected) o.focus();
          },
          // The environment's injector: the shell itself may be going away.
          { injector: this.environment },
        );
      });
    });
  }

  openRail(): void {
    this.opener = document.activeElement as HTMLElement | null;
    this.railOpen.set(true);
  }

  closeRail(): void {
    this.railOpen.set(false);
  }

  setChatCollapsed(collapsed: boolean): void {
    this.chatCollapsed.set(collapsed);
  }

  startChronicleDrag(event: PointerEvent): void {
    const bounds = this.bounds();
    if (!bounds) return;
    event.preventDefault();
    const dx = event.clientX - bounds.drawer.left;
    const dy = bounds.drawer.bottom - event.clientY;
    const move = (e: PointerEvent) => {
      const b = this.bounds() ?? bounds;
      this.place(e.clientX - b.shell.left - dx, b.shell.bottom - e.clientY - dy, b);
    };
    const up = () => {
      window.removeEventListener('pointermove', move);
      window.removeEventListener('pointerup', up);
    };
    window.addEventListener('pointermove', move);
    window.addEventListener('pointerup', up);
  }

  nudgeChronicle(event: KeyboardEvent): void {
    const steps: Record<string, readonly [number, number]> = {
      ArrowLeft: [-16, 0],
      ArrowRight: [16, 0],
      ArrowUp: [0, 16],
      ArrowDown: [0, -16],
    };
    const d = steps[event.key];
    const b = this.bounds();
    if (!d || !b) return;
    event.preventDefault();
    this.place(b.drawer.left - b.shell.left + d[0], b.shell.bottom - b.drawer.bottom + d[1], b);
  }

  private bounds(): { shell: DOMRect; drawer: DOMRect } | null {
    const drawer = this.chronicleRegion()?.nativeElement as HTMLElement | undefined;
    if (!drawer) return null;
    return { shell: this.shellRef().nativeElement.getBoundingClientRect(), drawer: drawer.getBoundingClientRect() };
  }

  private place(left: number, bottom: number, b: { shell: DOMRect; drawer: DOMRect }): void {
    const m = 8;
    const clamp = (v: number, lo: number, hi: number) => Math.max(lo, Math.min(hi, v));
    this.chroniclePosition.set({
      left: Math.round(clamp(left, m, b.shell.width - b.drawer.width - m)),
      bottom: Math.round(clamp(bottom, m, b.shell.height - b.drawer.height - m)),
    });
  }
}
