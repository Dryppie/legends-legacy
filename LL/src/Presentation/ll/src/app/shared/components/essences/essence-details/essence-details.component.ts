import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { EssenceDescriptionComponent } from '../essence-description/essence-description.component';
import { TicksToSecondsPipe } from '../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';
import { AttributeTypeFormatPipe } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { NgClass, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-essence-details',
  standalone: true,
  imports: [
    EssenceDescriptionComponent,
    TicksToSecondsPipe,
    AttributeTypeFormatPipe,
    NgIf,
    NgFor,
    NgClass,
  ],
  templateUrl: './essence-details.component.html',
})
export class EssenceDetailsComponent {
  @Input() essence!: Essence;
}
