import { Component, Input } from '@angular/core';
import { FeatureIconComponent } from '../feature-icon/feature-icon.component';
import { NgIf } from '@angular/common';
import { HelpLauncherComponent } from '../../help/help-launcher.component';
import { GuidePageId } from '../../help/guide-catalog';

@Component({
  selector: 'app-default-header',
  host: { class: 'block min-w-0 w-full' },
  imports: [NgIf, FeatureIconComponent, HelpLauncherComponent],
  templateUrl: './default-header.component.html',
})
export class DefaultHeaderComponent {
  @Input() title: string = '';
  @Input() text: string = '';
  @Input() icon: string = '';
  @Input() section: string = '';
  @Input() guidePageId?: GuidePageId;
  @Input() showGuide = true;
}
