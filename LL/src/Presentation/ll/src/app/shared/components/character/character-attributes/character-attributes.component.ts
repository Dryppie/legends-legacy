import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { PrimaryAttributesPipe } from '../../../pipes/attributes/primary-attributes/primary-attributes.pipe';
import { SecondaryAttributesPipe } from '../../../pipes/attributes/secondary-attributes/secondary-attributes.pipe';
import { AttributeDisplayComponent } from '../../attribute-display/attribute-display.component';

@Component({
  selector: 'app-character-attributes',
  standalone: true,
  imports: [
    NgFor,
    PrimaryAttributesPipe,
    SecondaryAttributesPipe,
    AttributeDisplayComponent,
  ],
  templateUrl: './character-attributes.component.html',
  styleUrl: './character-attributes.component.css',
})
export class CharacterAttributesComponent {
  @Input() attributes: AttributeDto[] = [];
}
