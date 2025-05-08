import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Essence } from '../../../../models/essence';
import { TicksToSecondsPipe } from '../../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';
import { NgFor } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AbilityTooltipContainerDirective } from '../../../../directives/ability-tooltip-container/ability-tooltip-container.directive';

@Component({
  selector: 'app-essence-modal',
  standalone: true,
  imports: [TicksToSecondsPipe, NgFor, AbilityTooltipContainerDirective],
  templateUrl: './essence-modal.component.html',
  styleUrl: './essence-modal.component.css',
})
export class EssenceModalComponent implements OnInit {
  @Input() essence!: Essence;
  @Output() close = new EventEmitter<void>();
  safeActiveDescription!: SafeHtml;
  safePassiveDescription!: SafeHtml;

  constructor(private sanitizer: DomSanitizer) {}

  ngOnInit() {
    this.safeActiveDescription = this.sanitizer.bypassSecurityTrustHtml(
      this.essence.active.description,
    );

    this.safePassiveDescription = this.sanitizer.bypassSecurityTrustHtml(
      this.essence.passive.description,
    );
  }

  onClose() {
    this.close.emit();
  }
}
