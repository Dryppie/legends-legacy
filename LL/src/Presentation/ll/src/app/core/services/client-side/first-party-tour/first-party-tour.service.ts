import { Injectable, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { CharacterStateService } from '../../api/character/character-state.service';
import { LocalStorageService } from '../local-storage/local-storage.service';
import {
  FirstPartyTourHistoryEntry,
  FirstPartyTourRect,
  FirstPartyTourStartOptions,
  FirstPartyTourStatePredicate,
  FirstPartyTourStep,
  FirstPartyTourStepJson,
  FirstPartyTourViewState,
} from './first-party-tour.models';
import { FirstPartyTourActionWatcherService } from './first-party-tour-action-watcher.service';

interface ActiveFirstPartyTour {
  pageId: string;
  storageKey: string;
  routePath: string;
  steps: FirstPartyTourStep[];
  stepIndex: number;
  history: FirstPartyTourHistoryEntry[];
}

@Injectable({ providedIn: 'root' })
export class FirstPartyTourService {
  private readonly _state = signal<FirstPartyTourViewState | null>(null);
  readonly state = this._state.asReadonly();

  private activeTour: ActiveFirstPartyTour | null = null;
  private actionCleanup: (() => void) | null = null;
  private layoutCleanup: (() => void) | null = null;
  private statePredicates = new Map<string, FirstPartyTourStatePredicate>();
  private isTransitioning = false;
  private startingPageId: string | null = null;
  private startingRoutePath: string | null = null;
  private startVersion = 0;
  private pendingRefreshFrame: number | null = null;

  constructor(
    private readonly storage: LocalStorageService,
    private readonly characterState: CharacterStateService,
    private readonly router: Router,
    private readonly actionWatcher: FirstPartyTourActionWatcherService,
  ) {
    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
      )
      .subscribe((event) => {
        if (this.hasLeftTourRoute(event.urlAfterRedirects)) {
          this.stop(false);
          return;
        }

        this.refreshTargetRect();
      });
  }

  async start(
    pageId: string,
    options: FirstPartyTourStartOptions = {},
  ): Promise<void> {
    const storageKey = this.getStorageKey(pageId);
    if (!options.force && this.storage.get<string>(storageKey) === 'done') {
      return;
    }

    if (this.activeTour?.pageId === pageId || this.startingPageId === pageId) {
      return;
    }

    this.stop(false);
    const startVersion = this.startVersion;
    const routePath = this.routePath(this.router.url);
    this.startingPageId = pageId;
    this.startingRoutePath = routePath;

    try {
      const steps = await this.loadSteps(pageId);
      if (
        this.startVersion !== startVersion ||
        this.startingPageId !== pageId ||
        this.startingRoutePath !== routePath
      ) {
        return;
      }

      if (steps.length === 0) {
        return;
      }

      this.activeTour = {
        pageId,
        storageKey,
        routePath,
        steps,
        stepIndex: 0,
        history: [],
      };

      this.bindLayoutWatchers();
      await this.activateStep(0);
    } finally {
      if (
        this.startVersion === startVersion &&
        this.startingPageId === pageId
      ) {
        this.startingPageId = null;
        this.startingRoutePath = null;
      }
    }
  }

  forceStart(pageId: string): Promise<void> {
    return this.start(pageId, { force: true });
  }

  stop(markDone = false): void {
    this.startVersion += 1;

    if (markDone && this.activeTour) {
      this.storage.set(this.activeTour.storageKey, 'done');
    }

    this.actionCleanup?.();
    this.layoutCleanup?.();
    if (this.pendingRefreshFrame !== null) {
      cancelAnimationFrame(this.pendingRefreshFrame);
    }

    this.actionCleanup = null;
    this.layoutCleanup = null;
    this.activeTour = null;
    this.isTransitioning = false;
    this.startingPageId = null;
    this.startingRoutePath = null;
    this.pendingRefreshFrame = null;
    this._state.set(null);
  }

  next(): void {
    const tour = this.activeTour;
    if (!tour || this.isTransitioning) {
      return;
    }

    if (tour.stepIndex >= tour.steps.length - 1) {
      this.finish();
      return;
    }

    tour.history.push({
      stepIndex: tour.stepIndex,
      route: this.router.url,
      scrollX: window.scrollX,
      scrollY: window.scrollY,
    });

    if (tour.steps[tour.stepIndex].kind === 'navigate') {
      tour.routePath = this.routePath(this.router.url);
    }

    void this.activateStep(tour.stepIndex + 1);
  }

  back(): void {
    const tour = this.activeTour;
    if (!tour || this.isTransitioning || tour.stepIndex <= 0) {
      return;
    }

    const previous = tour.history.pop();
    const nextIndex = previous?.stepIndex ?? tour.stepIndex - 1;
    void this.returnToPreviousStep(nextIndex, previous);
  }

  finish(): void {
    this.stop(true);
  }

  registerStatePredicate(
    key: string,
    predicate: FirstPartyTourStatePredicate,
  ): () => void {
    this.statePredicates.set(key, predicate);
    return () => {
      if (this.statePredicates.get(key) === predicate) {
        this.statePredicates.delete(key);
      }
    };
  }

  private async returnToPreviousStep(
    stepIndex: number,
    previous: FirstPartyTourHistoryEntry | undefined,
  ): Promise<void> {
    if (previous && this.router.url !== previous.route) {
      const tour = this.activeTour;
      if (tour) {
        tour.routePath = this.routePath(previous.route);
      }
      await this.router.navigateByUrl(previous.route);
    }

    if (previous) {
      window.scrollTo(previous.scrollX, previous.scrollY);
    }

    this.runRestoreAction(this.activeTour?.steps[stepIndex]);
    await this.activateStep(stepIndex);
  }

  private async activateStep(stepIndex: number): Promise<void> {
    const tour = this.activeTour;
    if (!tour) {
      return;
    }

    this.isTransitioning = true;
    this.actionCleanup?.();
    this.actionCleanup = null;

    const step = tour.steps[stepIndex];
    tour.stepIndex = stepIndex;
    const target = await this.findTargetElement(step);
    if (!this.isCurrentActivation(tour, stepIndex)) {
      return;
    }

    if (target) {
      this.scrollElementIntoView(target);
      await this.waitForAnimationFrame();
      if (!this.isCurrentActivation(tour, stepIndex)) {
        return;
      }
    }

    this._state.set(
      this.createViewState(tour, step, this.measureStep(step, target)),
    );
    this.actionCleanup = this.actionWatcher.watch(step, {
      advance: () => this.next(),
      statePredicates: this.statePredicates,
    });
    this.isTransitioning = false;
  }

  private isCurrentActivation(
    tour: ActiveFirstPartyTour,
    stepIndex: number,
  ): boolean {
    return this.activeTour === tour && tour.stepIndex === stepIndex;
  }

  private refreshTargetRect(): void {
    if (this.pendingRefreshFrame !== null) {
      return;
    }

    this.pendingRefreshFrame = requestAnimationFrame(() => {
      this.pendingRefreshFrame = null;
      this.updateTargetRect();
    });
  }

  private hasLeftTourRoute(url: string): boolean {
    const destinationPath = this.routePath(url);
    if (this.startingRoutePath && destinationPath !== this.startingRoutePath) {
      return true;
    }

    const tour = this.activeTour;
    if (!tour || destinationPath === tour.routePath) {
      return false;
    }

    const step = tour.steps[tour.stepIndex];
    const expectedRoute = step.advanceOn?.route ?? step.route;
    return !(
      step.kind === 'navigate' &&
      expectedRoute &&
      destinationPath === this.routePath(expectedRoute)
    );
  }

  private routePath(route: string): string {
    const normalized = route.startsWith('/') ? route : `/${route}`;
    return normalized.split(/[?#]/, 1)[0];
  }

  private updateTargetRect(): void {
    const tour = this.activeTour;
    const state = this._state();
    if (!tour || !state) {
      return;
    }

    const target = this.findReadyTarget(state.step);
    this._state.set(
      this.createViewState(
        tour,
        state.step,
        this.measureStep(state.step, target),
      ),
    );
  }

  private createViewState(
    tour: ActiveFirstPartyTour,
    step: FirstPartyTourStep,
    targetRect: FirstPartyTourRect | null,
  ): FirstPartyTourViewState {
    const isLastStep = tour.stepIndex >= tour.steps.length - 1;
    const allowsManualNext = step.kind === 'info' || step.showNext === true;

    return {
      pageId: tour.pageId,
      step,
      stepIndex: tour.stepIndex,
      stepCount: tour.steps.length,
      targetRect,
      canGoBack: tour.stepIndex > 0,
      canGoNext: allowsManualNext && !isLastStep,
      canFinish: allowsManualNext && isLastStep,
      blocksInteraction:
        !tour.pageId.startsWith('tutorial-') && this.blocksInteraction(step),
      instruction: null,
    };
  }

  private blocksInteraction(step: FirstPartyTourStep): boolean {
    if (step.allowOutsideInteraction === true) {
      return false;
    }

    return step.kind !== 'waitForState';
  }

  private async loadSteps(pageId: string): Promise<FirstPartyTourStep[]> {
    const response = await fetch(`/assets/help/tours/${pageId}.json`);
    if (!response.ok) {
      return [];
    }

    const raw = (await response.json()) as unknown;
    if (!Array.isArray(raw)) {
      return [];
    }

    return raw
      .filter((step): step is FirstPartyTourStepJson => this.isStepJson(step))
      .map((step, index) => this.normalizeStep(pageId, step, index));
  }

  private isStepJson(value: unknown): value is FirstPartyTourStepJson {
    if (!value || typeof value !== 'object') {
      return false;
    }

    const candidate = value as Partial<FirstPartyTourStepJson>;
    return (
      typeof candidate.element === 'string' &&
      typeof candidate.description === 'string'
    );
  }

  private normalizeStep(
    pageId: string,
    step: FirstPartyTourStepJson,
    index: number,
  ): FirstPartyTourStep {
    return {
      ...step,
      id: step.id ?? `${pageId}-${index + 1}`,
      kind: step.kind ?? 'info',
      position: step.position ?? 'bottom',
      alignment: step.alignment ?? 'center',
    };
  }

  private async findTargetElement(
    step: FirstPartyTourStep,
  ): Promise<HTMLElement | null> {
    const immediate = this.findReadyTarget(step);
    if (immediate) {
      return immediate;
    }

    return new Promise((resolve) => {
      const startedAt = performance.now();
      const timeoutMs = step.targetTimeoutMs ?? 2000;

      const check = () => {
        const target = this.findReadyTarget(step);
        if (target) {
          resolve(target);
          return;
        }

        if (performance.now() - startedAt >= timeoutMs) {
          resolve(null);
          return;
        }

        requestAnimationFrame(check);
      };

      requestAnimationFrame(check);
    });
  }

  private findReadyTarget(step: FirstPartyTourStep): HTMLElement | null {
    const targets = Array.from(
      document.querySelectorAll<HTMLElement>(step.element),
    );

    return (
      targets.find(
        (target) =>
          this.isElementVisible(target) && this.isTargetReady(target, step),
      ) ?? null
    );
  }

  private isTargetReady(
    target: HTMLElement | null,
    step: FirstPartyTourStep,
  ): target is HTMLElement {
    if (!target) {
      return false;
    }

    if (!step.waitForEnabled) {
      return true;
    }

    return !this.isDisabled(target);
  }

  private isElementVisible(element: HTMLElement): boolean {
    const rect = element.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) {
      return false;
    }

    const style = getComputedStyle(element);
    return style.display !== 'none' && style.visibility !== 'hidden';
  }

  private isDisabled(element: HTMLElement): boolean {
    if (
      element instanceof HTMLButtonElement ||
      element instanceof HTMLInputElement ||
      element instanceof HTMLSelectElement ||
      element instanceof HTMLTextAreaElement
    ) {
      return element.disabled;
    }

    return (
      element.getAttribute('aria-disabled') === 'true' ||
      !!element.closest(
        'button:disabled,input:disabled,select:disabled,textarea:disabled,[aria-disabled="true"]',
      )
    );
  }

  private measure(element: Element | null): FirstPartyTourRect | null {
    if (!element) {
      return null;
    }

    const rect = element.getBoundingClientRect();
    return {
      top: rect.top,
      right: rect.right,
      bottom: rect.bottom,
      left: rect.left,
      width: rect.width,
      height: rect.height,
    };
  }

  private scrollElementIntoView(element: HTMLElement): void {
    element.scrollIntoView({
      behavior: 'auto',
      block: 'center',
      inline: 'center',
    });

    let parent = element.parentElement;
    while (parent) {
      if (this.canScroll(parent)) {
        this.centerInScrollParent(element, parent);
      }

      parent = parent.parentElement;
    }
  }

  private canScroll(element: HTMLElement): boolean {
    const style = getComputedStyle(element);
    const canScrollY = /(auto|scroll)/.test(style.overflowY);
    const canScrollX = /(auto|scroll)/.test(style.overflowX);

    return (
      (canScrollY && element.scrollHeight > element.clientHeight) ||
      (canScrollX && element.scrollWidth > element.clientWidth)
    );
  }

  private centerInScrollParent(
    element: HTMLElement,
    scrollParent: HTMLElement,
  ): void {
    const elementRect = element.getBoundingClientRect();
    const parentRect = scrollParent.getBoundingClientRect();
    const offsetTop =
      elementRect.top -
      parentRect.top -
      scrollParent.clientHeight / 2 +
      elementRect.height / 2;
    const offsetLeft =
      elementRect.left -
      parentRect.left -
      scrollParent.clientWidth / 2 +
      elementRect.width / 2;

    scrollParent.scrollTop += offsetTop;
    scrollParent.scrollLeft += offsetLeft;
  }

  private measureStep(
    step: FirstPartyTourStep,
    element: Element | null,
  ): FirstPartyTourRect | null {
    const rects: FirstPartyTourRect[] = [];
    const mainRect = this.measure(element);
    if (mainRect) {
      rects.push(mainRect);
    }

    for (const selector of step.includeSelectors ?? []) {
      document.querySelectorAll(selector).forEach((included) => {
        const includedRect = this.measure(included);
        if (includedRect) {
          rects.push(includedRect);
        }
      });
    }

    return this.unionRects(rects);
  }

  private unionRects(rects: FirstPartyTourRect[]): FirstPartyTourRect | null {
    if (rects.length === 0) {
      return null;
    }

    const top = Math.min(...rects.map((rect) => rect.top));
    const right = Math.max(...rects.map((rect) => rect.right));
    const bottom = Math.max(...rects.map((rect) => rect.bottom));
    const left = Math.min(...rects.map((rect) => rect.left));

    return {
      top,
      right,
      bottom,
      left,
      width: right - left,
      height: bottom - top,
    };
  }

  private bindLayoutWatchers(): void {
    const refresh = () => this.refreshTargetRect();
    window.addEventListener('resize', refresh);
    window.addEventListener('ll-tour-layout-change', refresh);
    this.layoutCleanup = () => {
      window.removeEventListener('resize', refresh);
      window.removeEventListener('ll-tour-layout-change', refresh);
    };
  }

  private waitForAnimationFrame(): Promise<void> {
    return new Promise((resolve) => requestAnimationFrame(() => resolve()));
  }

  private runRestoreAction(step: FirstPartyTourStep | undefined): void {
    if (!step?.restoreOnBack) {
      return;
    }

    if (step.restoreOnBack.type === 'click') {
      document.querySelector<HTMLElement>(step.restoreOnBack.selector)?.click();
    }
  }

  private getStorageKey(pageId: string): string {
    const characterName = this.characterState.currentCharacter()?.name?.trim();
    if (!characterName) {
      return `first-party-tour:global:${pageId}`;
    }

    return `first-party-tour:character:${this.toStorageSlug(characterName)}:${pageId}`;
  }

  private toStorageSlug(value: string): string {
    return value.toLowerCase().replace(/[^a-z0-9_-]+/g, '-');
  }
}
