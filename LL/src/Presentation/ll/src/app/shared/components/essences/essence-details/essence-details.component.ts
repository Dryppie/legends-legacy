import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { EssenceDescriptionComponent } from '../essence-description/essence-description.component';
import { AbilityTagsComponent } from '../ability-tags/ability-tags.component';

@Component({
  selector: 'app-essence-details',
  imports: [EssenceDescriptionComponent, AbilityTagsComponent],
  templateUrl: './essence-details.component.html',
})
export class EssenceDetailsComponent {
  @Input() essence!: Essence;
}
