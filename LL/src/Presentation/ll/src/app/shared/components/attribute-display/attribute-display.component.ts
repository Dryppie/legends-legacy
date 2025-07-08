import { Component, Input } from '@angular/core';
import { AttributeDto } from '../../models/Dtos/attributesDto';
import { AttributeTypeFormatPipe } from '../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
  selector: 'app-attribute-display',
  standalone: true,
  imports: [AttributeTypeFormatPipe],
  templateUrl: './attribute-display.component.html',
})
export class AttributeDisplayComponent {
  @Input() attribute!: AttributeDto;
}
