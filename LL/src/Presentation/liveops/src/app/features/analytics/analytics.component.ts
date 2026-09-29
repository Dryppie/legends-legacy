import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AnalyticsState } from './analytics-state.service';
import { ANALYTICS_PAGES } from './analytics-pages';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, RouterOutlet],
  providers: [AnalyticsState],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.css',
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  readonly pages = ANALYTICS_PAGES;
  page: typeof ANALYTICS_PAGES[number] = ANALYTICS_PAGES[0];
  @ViewChild('heading', { static: true }) private heading!: ElementRef<HTMLHeadingElement>;
  private navigation?: Subscription;
  private focusFrame?: number;
  constructor(public readonly state: AnalyticsState, private readonly route: ActivatedRoute, private readonly router: Router) {}
  get isItemization(): boolean { return this.page.path === 'itemization'; }
  get loading(): boolean { return this.isItemization ? this.state.itemizationLoading : this.state.loading; }
  ngOnInit(): void {
    this.navigation = this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => this.updatePage());
    this.updatePage();
  }
  private updatePage(): void {
    const path = this.route.firstChild?.snapshot.data['analyticsPage'];
    if (!path) return;
    const next = this.pages.find(page => page.path === path) ?? this.pages[0];
    const changed = this.page.path !== next.path;
    this.page = next;
    if (this.isItemization) void this.state.ensureItemization(); else void this.state.ensureOverview();
    if (changed) {
      if (this.focusFrame !== undefined) cancelAnimationFrame(this.focusFrame);
      this.focusFrame = requestAnimationFrame(() => {
        window.scrollTo({ top: 0 });
        this.heading.nativeElement.focus({ preventScroll: true });
      });
    }
  }
  refresh(): void {
    if (this.isItemization) void this.state.loadItemization(); else void this.state.loadOverview();
  }
  saveAfterInteraction(): void { queueMicrotask(() => this.state.savePreferences()); }
  ngOnDestroy(): void {
    this.navigation?.unsubscribe();
    if (this.focusFrame !== undefined) cancelAnimationFrame(this.focusFrame);
  }
}
