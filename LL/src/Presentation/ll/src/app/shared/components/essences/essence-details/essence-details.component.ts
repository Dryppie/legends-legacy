import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { EssenceDescriptionComponent } from '../essence-description/essence-description.component';
import { TicksToSecondsPipe } from '../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';
import { AttributeTypeFormatPipe } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { NgFor, NgIf } from '@angular/common';
import { AttributeDisplayPipe } from '../../../pipes/attributes/attribute-display/attribute-display.pipe';
import { AttributeTooltipDirective } from '../../../directives/attribute-tooltip/attribute-tooltip.directive';

@Component({
    selector: 'app-essence-details',
    imports: [
        EssenceDescriptionComponent,
        TicksToSecondsPipe,
        AttributeTypeFormatPipe,
        AttributeValueFormatPipe,
        AttributeDisplayPipe,
        AttributeTooltipDirective,
        NgIf,
        NgFor,
    ],
    templateUrl: './essence-details.component.html'
})
export class EssenceDetailsComponent {
  @Input() essence!: Essence;
}
