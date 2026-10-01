import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  contentChildren,
  effect,
  forwardRef,
  input,
  model,
  signal,
  viewChild,
} from '@angular/core';
import {
  LG_SHELL,
  LgChatLayout,
  LgChroniclePosition,
  LgShellApi,
  LgSlotDirective,
  lgCx,
  lgHasSlot,
  lgUniqueId,
} from './grimoire-core';

/**
 * The screen frame for every in-game screen.
 *
 * Slots: `lgSlot="rail"` (NavRail), `lgSlot="top"` (TopBar), `lgSlot="folio"`, `lgSlot="hints"` (KeyHints),
 * `lgSlot="chronicle"` (Chronicle); the default content is the stage (a Stage or a Page).
 *
 * `chatLayout` is the player's Chat layout setting: `docked` puts the Chronicle on the right (its own column at
 * 96rem+, under the Folio below that); `floating` makes it a draggable drawer over the stage. Under 60rem both become
 * a bottom dock, and the rail becomes a drawer: a TopBar with `showMenu` inside the shell opens it. The drawer takes
 * focus when it opens, makes the rest inert, closes on Escape and returns focus.
 */
@Component({
  selector: 'lg-game-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: LG_SHELL, useExisting: forwardRef(() => LgGameShellComponent) }],
  host: { style: 'display: contents' },
  template: `
    <div class="lg-shellhost" [style.height]="cssHeight()">
      <div #shell [class]="shellClass()" [style.--lg-shell-backdrop]="backdropUrl()">
        <a class="lg-skip" [attr.href]="'#' + mainId">Skip to content</a>
        @if (has('rail')) {
          <div #rail class="lg-shell__rail"><ng-content select="[lgSlot=rail]" /></div>
        }
        <div class="lg-shell__scrim" aria-hidden="true" (click)="closeRail()"></div>
        <main class="lg-shell__main" [id]="mainId" tabindex="-1" [attr.inert]="railOpen() ? '' : null">
          @if (has('top')) {
            <div class="lg-shell__top"><ng-content select="[lgSlot=top]" /></div>
          }
          <div class="lg-shell__stage">
            <ng-content />
            @if (has('hints')) {
              <div class="lg-shell__hints"><ng-content select="[lgSlot=hints]" /></div>
            }
          </div>
        </main>
        @if (has('folio')) {
          <div class="lg-shell__folio" [attr.inert]="railOpen() ? '' : null"><ng-content select="[lgSlot=folio]" /></div>
        }
        @if (has('chronicle')) {
          <div
            #chronicleWrap
            [class]="chronicleClass()"
            [style.--lg-float-left]="floatLeft()"
            [style.--lg-float-bottom]="floatBottom()"
            [attr.inert]="railOpen() ? '' : null"
          >
            <ng-content select="[lgSlot=chronicle]" />
          </div>
        }
      </div>
    </div>
  `,
})
export class LgGameShellComponent implements LgShellApi {
  readonly chatLayout = input<LgChatLayout>('docked');
  /** Floating drawer position (px from the shell's bottom-left). Two-way bindable to persist it. */
  readonly chroniclePosition = model<LgChroniclePosition | null>(null);
  /** CSS height (a number is px); defaults to 100vh. */
  readonly height = input<string | number>();
  /** The frame's backdrop (D-107): an image URL painted at Level 0 behind the rail, the stage and the docked Chronicle. */
  readonly backdrop = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly shellRef = viewChild.required<ElementRef<HTMLElement>>('shell');
  private readonly railRef = viewChild<ElementRef<HTMLElement>>('rail');
  private readonly chronicleRef = viewChild<ElementRef<HTMLElement>>('chronicleWrap');

  protected readonly mainId = lgUniqueId('lgmain');
  protected readonly railOpen = signal(false);
  private readonly chatCollapsed = signal(false);
  private opener: HTMLElement | null = null;

  protected readonly cssHeight = computed(() => {
    const h = this.height();
    return h == null ? null : typeof h === 'number' ? h + 'px' : h;
  });
  private readonly placed = computed(() => this.chatLayout() === 'floating' && !!this.chroniclePosition());
  protected readonly floatLeft = computed(() => (this.placed() ? this.chroniclePosition()!.left + 'px' : null));
  protected readonly floatBottom = computed(() => (this.placed() ? this.chroniclePosition()!.bottom + 'px' : null));
  protected readonly shellClass = computed(() => {
    const chat = this.has('chronicle');
    const layout = this.chatLayout() === 'floating' ? 'floating' : 'docked';
    return lgCx(
      'lg-shell',
      this.has('folio') && 'has-folio',
      chat && 'has-chat',
      chat && 'is-chat-' + layout,
      chat && this.chatCollapsed() && 'is-chat-collapsed',
      this.railOpen() && 'is-rail-open',
      !!this.backdrop() && 'has-backdrop',
    );
  });
  protected readonly backdropUrl = computed(() => {
    const b = this.backdrop();
    return b ? `url("${b}")` : null;
  });
  protected readonly chronicleClass = computed(() =>
    this.chatLayout() === 'floating'
      ? lgCx('lg-shell__chronicle', 'lg-shell__chronicle--floating', this.placed() && 'is-placed')
      : 'lg-shell__chronicle lg-shell__chronicle--docked',
  );

  constructor() {
    // The rail drawer takes focus when it opens, closes on Escape and returns focus to its opener.
    effect((onCleanup) => {
      if (!this.railOpen()) return;
      const rail = this.railRef()?.nativeElement;
      setTimeout(() => rail?.querySelector<HTMLElement>('button:not([disabled]), a[href], [tabindex="0"]')?.focus());
      const onKey = (e: KeyboardEvent) => {
        if (e.key === 'Escape') {
          e.stopPropagation();
          this.railOpen.set(false);
        }
      };
      document.addEventListener('keydown', onKey);
      onCleanup(() => {
        document.removeEventListener('keydown', onKey);
        // After the drawer has closed and the rest is no longer inert.
        const o = this.opener;
        setTimeout(() => {
          if (o && o.isConnected) o.focus();
        });
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

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }

  private bounds(): { shell: DOMRect; drawer: DOMRect } | null {
    const drawer = this.chronicleRef()?.nativeElement;
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
