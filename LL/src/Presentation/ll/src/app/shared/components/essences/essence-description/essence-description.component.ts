import { Component, Input, OnChanges } from '@angular/core';
import { SafeHtml, DomSanitizer } from '@angular/platform-browser';
import { AbilityTooltipContainerDirective } from '../../../directives/ability-tooltip-container/ability-tooltip-container.directive';

@Component({
  selector: 'app-essence-description',
  standalone: true,
  imports: [AbilityTooltipContainerDirective],
  templateUrl: './essence-description.component.html',
})
export class EssenceDescriptionComponent implements OnChanges {
  @Input() description = '';
  safeDescription!: SafeHtml;

  constructor(private sanitizer: DomSanitizer) {}

  ngOnChanges(): void {
    this.safeDescription = this.sanitizer.bypassSecurityTrustHtml(
      this.description,
    );
  }
}
