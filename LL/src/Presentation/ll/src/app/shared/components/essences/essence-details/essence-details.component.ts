import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { EssenceDescriptionComponent } from '../essence-description/essence-description.component';
import { TicksToSecondsPipe } from '../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';
import { AbilityTagsComponent } from '../ability-tags/ability-tags.component';

@Component({
  selector: 'app-essence-details',
  imports: [
    EssenceDescriptionComponent,
    TicksToSecondsPipe,
    AbilityTagsComponent,
  ],
  templateUrl: './essence-details.component.html',
})
export class EssenceDetailsComponent {
  @Input() essence!: Essence;
}
