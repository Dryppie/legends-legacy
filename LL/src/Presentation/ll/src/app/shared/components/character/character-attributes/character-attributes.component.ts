import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { SecondaryAttributesPipe } from '../../../pipes/attributes/secondary-attributes/secondary-attributes.pipe';
import { AttributeDisplayComponent } from '../../attribute-display/attribute-display.component';

@Component({
  selector: 'app-character-attributes',
  standalone: true,
  imports: [NgFor, SecondaryAttributesPipe, AttributeDisplayComponent],
  templateUrl: './character-attributes.component.html',
})
export class CharacterAttributesComponent {
  @Input() attributes: AttributeDto[] = [];
}
