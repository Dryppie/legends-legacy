import { Component, Input, TemplateRef, ViewChild } from '@angular/core';

@Component({
    selector: 'app-tab',
    imports: [],
    templateUrl: './tab.component.html'
})
export class TabComponent {
  /** What the user sees in the header bar */
  @Input() label = '';
  @Input() dataTour = '';
  @Input() notificationCount = 0;
  @Input() notificationLabel = '';

  /** Captures the <ng-content> so TabsComponent can render it later */
  @ViewChild('panelTpl', { static: true, read: TemplateRef })
  templateRef!: TemplateRef<unknown>;
}
