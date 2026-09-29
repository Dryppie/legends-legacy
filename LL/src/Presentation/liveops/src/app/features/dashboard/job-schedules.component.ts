import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LiveOpsApiService } from '../../liveops-api.service';
import { JobScheduleHealth } from '../../liveops.models';

@Component({ selector: 'app-job-schedules', standalone: true, imports: [CommonModule], template: `
  <section class="status-section"><div class="status-section-heading"><div><p class="eyebrow">Actual worker schedules</p><h2>Essential jobs</h2></div><button class="secondary" (click)="load()" [disabled]="loading">{{ loading ? 'Checking schedules' : 'Refresh schedules' }}</button></div>
    <p>Read from the worker's persisted Quartz registry and retained execution records. Five minutes of scheduling delay is allowed; a worker check-in is not proof that a job completed.</p>
    @if (error) { <p class="message error" role="status">{{ error }}</p> }
    @if (checkedAt) { <p>Checked {{ checkedAt | date:'medium' }}. Refresh separately from dependency status.</p> }
    <button class="secondary" (click)="showExceptions.emit()">Recorded job exceptions</button>
    <div class="work-queue-grid">@for (row of rows; track $index) { <article class="case-form"><strong>{{ row.jobName }} · {{ row.state }}</strong><p>{{ row.message }}</p><details><summary>Schedule and execution evidence</summary><p>{{ row.schedule }}</p><p>Next: {{ row.nextFireAt ? (row.nextFireAt | date:'medium') : 'Not scheduled' }}</p><p>Last start: {{ row.lastStartedAt ? (row.lastStartedAt | date:'medium') : 'Not recorded' }}</p><p>Last completion: {{ row.lastCompletedAt ? (row.lastCompletedAt | date:'medium') : 'Not recorded for latest execution' }}</p><p>Worker check-in: {{ row.workerCheckInAt ? (row.workerCheckInAt | date:'medium') : 'Not recorded' }}</p></details></article> }</div>
  </section>
` })
export class JobSchedulesComponent implements OnInit, OnDestroy {
  @Output() readonly showExceptions = new EventEmitter<void>();
  rows: JobScheduleHealth[] = []; loading = false; error = ''; checkedAt = ''; private generation = 0;
  constructor(private api: LiveOpsApiService) {}
  ngOnInit(): void { void this.load(); } ngOnDestroy(): void { this.generation++; }
  async load(): Promise<void> { const generation = ++this.generation; this.loading = true; this.error = '';
    try { const result = await this.api.jobSchedules(); if (generation !== this.generation) return; if (!result.isSuccess || !result.data) throw new Error(); this.rows = result.data; this.checkedAt = new Date().toISOString(); }
    catch { if (generation === this.generation) this.error = 'Scheduler evidence is unavailable. No healthy or missing-job status can be inferred.' + (this.rows.length ? ' Showing the previous check below.' : ''); }
    finally { if (generation === this.generation) this.loading = false; }
  }
}
