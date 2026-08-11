import { NgIf } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { EssenceItemViewService } from '../../../../core/services/api/essences/essence-item-view.service';
import { Essence } from '../../../models/essence';
import { EssenceDefinitionDto } from '../../../models/essence-system';
import { PopoverComponent } from '../../custom-components/popover/popover.component';
import { EssenceDetailsComponent } from '../essence-details/essence-details.component';

@Component({
  selector: 'app-essence-preview',
  imports: [NgIf, PopoverComponent, EssenceDetailsComponent],
  templateUrl: './essence-preview.component.html',
})
export class EssencePreviewComponent {
  readonly definition = input<EssenceDefinitionDto | null | undefined>(null);
  readonly essence = input<Essence | null | undefined>(null);
  readonly originClass = input('relative inline-block');

  readonly details = computed(() => {
    const essence = this.essence();
    if (essence) return essence;

    const definition = this.definition();
    return definition ? this.essenceItemView.fromDefinition(definition) : null;
  });

  constructor(private readonly essenceItemView: EssenceItemViewService) {}
}
