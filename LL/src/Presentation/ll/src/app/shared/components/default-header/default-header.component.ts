import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../professions/profession-icon/profession-icon.component';
import { NgIf } from '@angular/common';
import { HelpLauncherComponent } from '../../help/help-launcher.component';
import { GuidePageId } from '../../help/guide-catalog';

@Component({
  selector: 'app-default-header',
  imports: [NgIf, ProfessionIconComponent, HelpLauncherComponent],
  templateUrl: './default-header.component.html',
})
export class DefaultHeaderComponent {
  @Input() title: string = '';
  @Input() text: string = '';
  @Input() icon: string = '';
  @Input() section: string = '';
  @Input() guidePageId?: GuidePageId;
}
