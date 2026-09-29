import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  contentChildren,
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
  lgHasSlot,
} from './grimoire-core';

/**
 * The full-screen frame for every in-game screen.
 *
 * Slots: `lgSlot="rail"` (NavRail), `lgSlot="top"` (TopBar), `lgSlot="folio"`,
 * `lgSlot="hints"` (KeyHints), `lgSlot="chronicle"` (Chronicle); the default
 * content is the stage (a Stage or a Page).
 *
 * `chatLayout` is the player's Chat layout setting: `docked` puts the Chronicle on
 * the right (its own column at 1536px+, under the Folio below that); `floating`
 * makes it a draggable drawer over the stage. Under 960px both become a bottom dock.
 */
@Component({
  selector: 'lg-game-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    { provide: LG_SHELL, useExisting: forwardRef(() => LgGameShellComponent) },
  ],
  host: {
    class: 'lg-shellhost',
    '[style.height]': 'height() ?? null',
  },
  template: `
    <div #shell [class]="shellClass()">
      @if (has('rail')) {
        <div class="lg-shell__rail"><ng-content select="[lgSlot=rail]" /></div>
      }
      <div class="lg-shell__scrim" aria-hidden="true" (click)="closeRail()"></div>
      <main class="lg-shell__main">
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
        <div class="lg-shell__folio"><ng-content select="[lgSlot=folio]" /></div>
      }
      @if (has('chronicle')) {
        <div
          #chronicleWrap
          [class]="chronicleClass()"
          [style.--lg-float-left]="floatLeft()"
          [style.--lg-float-bottom]="floatBottom()"
        >
          <ng-content select="[lgSlot=chronicle]" />
        </div>
      }
    </div>
  `,
})
export class LgGameShellComponent implements LgShellApi {
  readonly chatLayout = input<LgChatLayout>('docked');
  /** Floating drawer position (px from the shell's bottom-left). Two-way bindable to persist it. */
  readonly chroniclePosition = model<LgChroniclePosition | null>(null);
  /** CSS height; defaults to 100vh. */
  readonly height = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly shellRef = viewChild.required<ElementRef<HTMLElement>>('shell');
  private readonly chronicleRef = viewChild<ElementRef<HTMLElement>>('chronicleWrap');

  protected readonly railOpen = signal(false);
  private readonly chatCollapsed = signal(false);

  protected readonly floatingPlaced = computed(
    () => this.chatLayout() === 'floating' && !!this.chroniclePosition(),
  );

  protected readonly floatLeft = computed(() => {
    const position = this.chroniclePosition();
    return this.floatingPlaced() && position ? `${position.left}px` : null;
  });
  protected readonly floatBottom = computed(() => {
    const position = this.chroniclePosition();
    return this.floatingPlaced() && position ? `${position.bottom}px` : null;
  });

  protected readonly shellClass = computed(() => {
    const hasChat = this.has('chronicle');
    return [
      'lg-shell',
      this.has('folio') ? 'has-folio' : '',
      hasChat ? 'has-chat' : '',
      hasChat ? `is-chat-${this.chatLayout()}` : '',
      hasChat && this.chatCollapsed() ? 'is-chat-collapsed' : '',
      this.railOpen() ? 'is-rail-open' : '',
    ]
      .filter(Boolean)
      .join(' ');
  });

  protected readonly chronicleClass = computed(() =>
    this.chatLayout() === 'floating'
      ? `lg-shell__chronicle lg-shell__chronicle--floating${this.floatingPlaced() ? ' is-placed' : ''}`
      : 'lg-shell__chronicle lg-shell__chronicle--docked',
  );

  openRail(): void {
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
    const offsetX = event.clientX - bounds.drawer.left;
    const offsetY = bounds.drawer.bottom - event.clientY;
    const move = (moveEvent: PointerEvent) => {
      const current = this.bounds() ?? bounds;
      this.place(
        moveEvent.clientX - current.shell.left - offsetX,
        current.shell.bottom - moveEvent.clientY - offsetY,
        current,
      );
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
    const step = steps[event.key];
    const bounds = this.bounds();
    if (!step || !bounds) return;
    event.preventDefault();
    this.place(
      bounds.drawer.left - bounds.shell.left + step[0],
      bounds.shell.bottom - bounds.drawer.bottom + step[1],
      bounds,
    );
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }

  private bounds(): { shell: DOMRect; drawer: DOMRect } | null {
    const drawer = this.chronicleRef()?.nativeElement;
    if (!drawer) return null;
    return {
      shell: this.shellRef().nativeElement.getBoundingClientRect(),
      drawer: drawer.getBoundingClientRect(),
    };
  }

  private place(left: number, bottom: number, bounds: { shell: DOMRect; drawer: DOMRect }): void {
    const margin = 8;
    const clamp = (value: number, min: number, max: number) =>
      Math.max(min, Math.min(max, value));
    this.chroniclePosition.set({
      left: Math.round(clamp(left, margin, bounds.shell.width - bounds.drawer.width - margin)),
      bottom: Math.round(clamp(bottom, margin, bounds.shell.height - bounds.drawer.height - margin)),
    });
  }
}
