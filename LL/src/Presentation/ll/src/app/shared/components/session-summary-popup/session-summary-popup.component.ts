import { NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';

@Component({
  selector: 'app-session-summary-popup',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './session-summary-popup.component.html',
})
export class SessionSummaryPopupComponent {
  constructor(public svc: SessionSummaryService) {}
  getDuration(from: string | Date, to: string | Date): string {
    const fromDate = new Date(from);
    const toDate = new Date();
    const diffMs = toDate.getTime() - fromDate.getTime();

    const totalMinutes = Math.floor(diffMs / (1000 * 60));
    const days = Math.floor(totalMinutes / (60 * 24));
    const hours = Math.floor((totalMinutes % (60 * 24)) / 60);
    const minutes = totalMinutes % 60;

    const parts: string[] = [];
    if (days) parts.push(`${days} day${days !== 1 ? 's' : ''}`);
    if (hours) parts.push(`${hours} hour${hours !== 1 ? 's' : ''}`);
    if (minutes || parts.length === 0)
      parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);
    let joinedDuration = parts.join(', ');
    if (days || hours >= 12) joinedDuration += ' (Rewards stop after 12 hours)';
    return joinedDuration;
  }
}
