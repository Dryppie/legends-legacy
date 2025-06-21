import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../profession-icon/profession-icon.component';
import { ProfessionActionComponent } from '../profession-action/profession-action.component';
import { HelpTooltipDirective } from '../../../help/help-tooltip.directive';
import { HelpLauncherComponent } from '../../../help/help-launcher.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-profession-header',
  standalone: true,
  imports: [
    ProfessionIconComponent,
    ProfessionActionComponent,
    HelpLauncherComponent,
    NgIf,
  ],
  templateUrl: './profession-header.component.html',
})
export class ProfessionHeaderComponent {
  @Input() title: string = '';
  @Input() icon: string = '';
  @Input() level: number = 1;
  @Input() experience: number = 0;
  @Input() experienceUntilNextLevel: number = 0;
  @Input() guidePageId: string = '';
}
