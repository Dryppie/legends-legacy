import { Component, ElementRef, Input, OnDestroy, ViewChild } from '@angular/core';

@Component({
  selector: 'app-column-help',
  standalone: true,
  template: `
    <button #trigger type="button" [attr.aria-label]="label + ': explain column'"
      [attr.aria-describedby]="tooltipId" (click)="pin()" (focus)="show()" (blur)="scheduleHide()">
      @if (!iconOnly) { <span>{{ label }}</span> } <span class="help-icon" aria-hidden="true">?</span>
    </button>
    <div #tip [id]="tooltipId" popover="auto" role="tooltip" (toggle)="toggled($event)">
      <strong>{{ label }}</strong><span>{{ description }}</span>
    </div>
  `,
  styles: [`
    :host { display: inline-block; }
    button { display: inline-flex; align-items: center; gap: .4rem; padding: .3rem 0; border: 0; border-radius: .2rem; background: transparent; color: inherit; font: inherit; text-align: left; white-space: normal; cursor: help; }
    button:hover { color: #f0d29a; }
    button:focus-visible { outline: 2px solid #e9c27a; outline-offset: 3px; }
    .help-icon { display: inline-grid; flex-shrink: 0; place-items: center; width: 1rem; height: 1rem; border: 1px solid #7c8ba0; border-radius: 50%; font-size: .7rem; line-height: 1; }
    [popover] { position: fixed; inset: auto; margin: 0; box-sizing: border-box; width: min(21rem, calc(100vw - 24px)); max-height: calc(100vh - 24px); overflow-y: auto; padding: .85rem 1rem; border: 1px solid #75849a; border-radius: .5rem; background: #1c2837; color: #e9e4d8; box-shadow: 0 8px 24px #0008; font-size: .85rem; font-weight: 400; line-height: 1.55; letter-spacing: normal; text-align: left; white-space: normal; }
    [popover] strong { display: block; margin-bottom: .3rem; color: #f0d29a; font-weight: 600; }
  `],
  host: {
    '(mouseenter)': 'enter()',
    '(mouseleave)': 'leave()',
    '(keydown.escape)': 'hide()',
  },
})
export class ColumnHelpComponent implements OnDestroy {
  @Input({ required: true }) label = '';
  @Input({ required: true }) description = '';
  @Input() iconOnly = false;
  @ViewChild('trigger', { static: true }) private trigger!: ElementRef<HTMLButtonElement>;
  @ViewChild('tip', { static: true }) private tip!: ElementRef<HTMLElement>;
  private static nextId = 0;
  readonly tooltipId = `column-help-${++ColumnHelpComponent.nextId}`;
  private hovering = false;
  private pinned = false;
  private closeTimer?: ReturnType<typeof setTimeout>;
  private readonly followAnchor = () => this.position();

  enter(): void { this.hovering = true; this.show(); }
  leave(): void { this.hovering = false; this.scheduleHide(); }
  pin(): void {
    this.pinned = !this.pinned;
    if (this.pinned) this.show(); else this.hide();
  }
  show(): void {
    clearTimeout(this.closeTimer);
    const tip = this.tip.nativeElement;
    if (tip.matches(':popover-open')) return;
    tip.showPopover();
    this.position();
    // Keep the top-layer tooltip anchored when keyboard focus scrolls the table into view.
    document.addEventListener('scroll', this.followAnchor, true);
    window.addEventListener('resize', this.followAnchor);
  }
  private position(): void {
    const tip = this.tip.nativeElement;
    if (!tip.matches(':popover-open')) return;
    const rect = this.trigger.nativeElement.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth, viewportHeight = document.documentElement.clientHeight;
    if (rect.bottom < 0 || rect.top > viewportHeight || rect.right < 0 || rect.left > viewportWidth) { this.hide(); return; }
    const width = tip.offsetWidth, height = tip.offsetHeight;
    tip.style.left = `${Math.max(12, Math.min(rect.left, viewportWidth - width - 12))}px`;
    const below = rect.bottom + 8;
    tip.style.top = `${Math.max(12, below + height <= viewportHeight - 12 ? below : rect.top - height - 8)}px`;
  }
  scheduleHide(): void {
    clearTimeout(this.closeTimer);
    this.closeTimer = setTimeout(() => {
      if (!this.pinned && !this.hovering && document.activeElement !== this.trigger.nativeElement) this.hide();
    }, 150);
  }
  hide(): void {
    clearTimeout(this.closeTimer);
    this.pinned = false;
    this.tip.nativeElement.hidePopover();
    this.removeListeners();
  }
  toggled(event: Event): void {
    if ((event as ToggleEvent).newState === 'closed') { this.pinned = false; this.removeListeners(); }
  }
  private removeListeners(): void {
    document.removeEventListener('scroll', this.followAnchor, true);
    window.removeEventListener('resize', this.followAnchor);
  }
  ngOnDestroy(): void { clearTimeout(this.closeTimer); this.removeListeners(); }
}
