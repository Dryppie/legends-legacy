import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { AttributeDisplayComponent } from '../../attribute-display/attribute-display.component';
import { GroupAttributesByCategoryPipe } from '../../../pipes/attributes/group-attributes-by-category/group-attributes-by-category.pipe';

@Component({
  selector: 'app-character-attributes',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    AttributeDisplayComponent,
    GroupAttributesByCategoryPipe,
  ],
  templateUrl: './character-attributes.component.html',
})
export class CharacterAttributesComponent {
  @Input() attributes: AttributeDto[] = [];
}
