import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, Input, NgZone, OnDestroy, OnChanges, SimpleChanges, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActionPreview, OperatorSession } from '../../liveops.models';

@Component({
  selector: 'app-action-preview',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './action-preview.component.html',
})
export class ActionPreviewComponent implements AfterViewInit, OnDestroy, OnChanges {
  @Input({ required: true }) preview!: ActionPreview;
  @Input({ required: true }) session!: OperatorSession;
  @Input() confirmation = '';
  @Input() submitting = false;
  @Input() message = '';
  @Output() confirmationChange = new EventEmitter<string>();
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() refreshPreview = new EventEmitter<void>();
  private previousFocus: HTMLElement | null = null;
  private timer?: ReturnType<typeof setInterval>;
  now = Date.now();
  private previewStarted = performance.now();
  private serverStarted = Date.now();
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['preview'] && this.preview?.serverTimeUtc) {
      this.previewStarted = performance.now(); this.serverStarted = Date.parse(this.preview.serverTimeUtc);
      this.now = this.serverStarted;
    }
  }

  constructor(private readonly element: ElementRef<HTMLElement>, private readonly zone: NgZone) {}

  ngAfterViewInit(): void {
    this.previousFocus = document.activeElement as HTMLElement;
    this.element.nativeElement.querySelector<HTMLElement>('input, .preview-actions button')?.focus();
    this.zone.runOutsideAngular(() => {
      this.timer = setInterval(() => this.zone.run(() => this.now = this.preview.serverTimeUtc ? this.serverStarted + performance.now() - this.previewStarted : Date.now()), 1000);
    });
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
    this.previousFocus?.focus();
  }

  get expired(): boolean { return Date.parse(this.preview.expiresAt) <= this.now; }
  requestCancel(): void { if (!this.submitting) this.cancel.emit(); }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') { event.preventDefault(); this.requestCancel(); return; }
    if (event.key !== 'Tab') return;
    const controls = Array.from(this.element.nativeElement.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), [tabindex="0"]'));
    const first = controls[0], last = controls[controls.length - 1];
    if (!first) { event.preventDefault(); return; }
    if (event.shiftKey && (document.activeElement === first || !this.element.nativeElement.contains(document.activeElement))) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && (document.activeElement === last || !this.element.nativeElement.contains(document.activeElement))) { event.preventDefault(); first.focus(); }
  }

  get canSubmit(): boolean {
    return !this.submitting && !this.expired &&
      (!this.preview.confirmationText || this.confirmation === this.preview.confirmationText);
  }
}
