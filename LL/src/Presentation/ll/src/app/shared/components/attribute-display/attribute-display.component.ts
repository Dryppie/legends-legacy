import { Component, Input } from '@angular/core';
import { AttributeDto } from '../../models/Dtos/attributesDto';
import { AttributeTypeFormatPipe } from '../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-attribute-display',
  standalone: true,
  imports: [AttributeTypeFormatPipe, AttributeValueFormatPipe],
  templateUrl: './attribute-display.component.html',
})
export class AttributeDisplayComponent {
  @Input() attribute!: AttributeDto;
}
