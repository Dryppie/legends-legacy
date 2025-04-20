import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';

@Component({
  selector: 'app-session-summary-popup',
  standalone: true,
  imports: [NgIf],
  templateUrl: './session-summary-popup.component.html',
  styleUrl: './session-summary-popup.component.css',
})
export class SessionSummaryPopupComponent {
  constructor(public svc: SessionSummaryService) {}
}
