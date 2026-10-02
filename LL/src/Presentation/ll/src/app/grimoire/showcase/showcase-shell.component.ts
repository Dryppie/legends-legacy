import {
  ApplicationRef,
  ChangeDetectionStrategy,
  Component,
  ComponentRef,
  DestroyRef,
  EnvironmentInjector,
  ViewEncapsulation,
  Injector,
  afterNextRender,
  computed,
  createComponent,
  effect,
  inject,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, map } from 'rxjs';
import { SHOWCASE_ENTRIES } from './showcase.registry';
import { ShowcaseEntryComponent } from './showcase-story.directive';
import {
  SHOWCASE_DEFAULTS,
  ShowcaseDensity,
  ShowcaseFrame,
  ShowcaseGround,
  ShowcaseMotion,
  ShowcaseOptions,
  ShowcaseStore,
  ShowcaseTextSize,
} from './showcase.store';
import { SHOWCASE_TIERS, showcaseSlug } from './showcase.types';

const READING_SIZE_ATTR = 'data-reading-font-size';

function pick<T extends string>(
  value: string | null,
  allowed: readonly T[],
  fallback: T,
): T {
  return value !== null && (allowed as readonly string[]).includes(value)
    ? (value as T)
    : fallback;
}

/**
 * The Grimoire showcase (ANGULAR_DESIGN_SYSTEM_PLAN.md, step 3): every design-system component on its own, outside
 * the game, at /grimoire. Development builds only; production builds replace its routes with none.
 *
 * It creates every entry component once, off-screen, to read its stories; the pages render the stories' templates.
 */
@Component({
  selector: 'sc-showcase-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './showcase-shell.component.html',
  // The showcase's chrome and the stories' layout helpers are one global stylesheet (every class is prefixed sc-),
  // so the helpers reach markup declared in the entry components.
  encapsulation: ViewEncapsulation.None,
  styleUrl: './showcase.css',
})
export class ShowcaseShellComponent {
  protected readonly store = inject(ShowcaseStore);
  protected readonly tiers = SHOWCASE_TIERS;

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly document = inject(DOCUMENT);
  private readonly refs: ComponentRef<ShowcaseEntryComponent>[] = [];

  private readonly query = toSignal(this.route.queryParamMap, {
    requireSync: true,
  });

  /** The entry the current URL shows (`/grimoire/<tier>/<slug>/…`), so the nav can list its stories. */
  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  protected readonly current = computed(() => {
    const parts = this.url().split('?')[0].split('/').filter(Boolean);
    return parts[0] === 'grimoire' ? `${parts[1] ?? ''}/${parts[2] ?? ''}` : '';
  });

  protected readonly options = computed<ShowcaseOptions>(() => {
    const q = this.query();
    return {
      density: pick<ShowcaseDensity>(
        q.get('density'),
        ['comfortable', 'standard', 'compact'],
        '',
      ),
      text: pick<ShowcaseTextSize>(
        q.get('text'),
        ['large', 'extra-large'],
        'default',
      ),
      motion: pick<ShowcaseMotion>(q.get('motion'), ['reduced'], 'full'),
      frame: pick<ShowcaseFrame>(q.get('frame'), ['page'], 'bare'),
      ground: pick<ShowcaseGround>(
        q.get('ground'),
        ['surface', 'backdrop'],
        'ground',
      ),
    };
  });

  constructor() {
    const env = inject(EnvironmentInjector);
    const injector = inject(Injector);
    const appRef = inject(ApplicationRef);
    const root = this.document.documentElement;
    const textBefore = root.getAttribute(READING_SIZE_ATTR);

    effect(() => this.store.options.set(this.options()));

    // Text size is the game's own root setting (D-096); the showcase sets it while it is open and puts it back.
    effect(() => {
      const text = this.options().text;
      if (text === 'default') root.removeAttribute(READING_SIZE_ATTR);
      else root.setAttribute(READING_SIZE_ATTR, text);
    });

    afterNextRender(() => {
      const loaded = SHOWCASE_ENTRIES.map((entry) => {
        const ref = createComponent(entry.component, {
          environmentInjector: env,
          elementInjector: injector,
        });
        appRef.attachView(ref.hostView);
        ref.changeDetectorRef.detectChanges();
        this.refs.push(ref);
        const stories = ref.instance.stories();
        const slugs = stories.map((s) => s.slug());
        const twice = slugs.filter((s, i) => slugs.indexOf(s) !== i);
        if (twice.length)
          console.warn(
            `[showcase] ${entry.name}: two stories share the slug "${twice[0]}".`,
          );
        if (entry.slug !== showcaseSlug(entry.slug))
          console.warn(
            `[showcase] ${entry.name}: slug "${entry.slug}" is not kebab-case.`,
          );
        return { entry, stories };
      });
      this.store.entries.set(loaded);
      this.store.ready.set(true);
    });

    inject(DestroyRef).onDestroy(() => {
      for (const ref of this.refs) {
        appRef.detachView(ref.hostView);
        ref.destroy();
      }
      if (textBefore === null) root.removeAttribute(READING_SIZE_ATTR);
      else root.setAttribute(READING_SIZE_ATTR, textBefore);
    });
  }

  protected setOption(key: keyof ShowcaseOptions, value: string): void {
    const tree = this.router.parseUrl(this.router.url);
    const query = { ...tree.queryParams };
    if (value === SHOWCASE_DEFAULTS[key]) delete query[key];
    else query[key] = value;
    tree.queryParams = query;
    void this.router.navigateByUrl(tree, { replaceUrl: true });
  }
}
