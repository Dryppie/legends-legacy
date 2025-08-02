import { Component, Input, TemplateRef, ViewChild } from '@angular/core';

@Component({
  selector: 'app-tab',
  standalone: true,
  imports: [],
  templateUrl: './tab.component.html',
})
export class TabComponent {
  /** What the user sees in the header bar */
  @Input() label = '';
  @Input() dataTour = '';

  /** Captures the <ng-content> so TabsComponent can render it later */
  @ViewChild('panelTpl', { static: true, read: TemplateRef })
  templateRef!: TemplateRef<unknown>;
}
