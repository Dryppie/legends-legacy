// ability-tooltip-container.directive.ts
import { Directive, ElementRef, Injector, OnDestroy } from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { fromEvent, Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { EssenceAbilityData } from '../../components/tooltips/ability-tooltip/essenceAbilityData';
import { AbilityTooltipComponent } from '../../components/tooltips/ability-tooltip/ability-tooltip.component';
import { ESSENCE_ABILITY_DATA } from '../../components/tooltips/ability-tooltip/essence-ability-data.token';

@Directive({
  selector: '[abilityTooltipContainer]',
  standalone: true,
})
export class AbilityTooltipContainerDirective implements OnDestroy {
  private overlayRef?: OverlayRef;
  private sub = new Subscription();

  constructor(
    private host: ElementRef<HTMLElement>,
    private overlay: Overlay,
    private injector: Injector,
  ) {
    // delegate pointer‑enter to any .dmg / .heal child
    this.sub.add(
      fromEvent<PointerEvent>(this.host.nativeElement, 'pointerover')
        .pipe(
          filter(
            (ev) => (ev.target as HTMLElement).closest('.dmg, .heal') !== null,
          ),
        )
        .subscribe((ev) =>
          this.open(
            (ev.target as HTMLElement).closest('.dmg, .heal') as HTMLElement,
          ),
        ),
    );

    // close when leaving that element
    this.sub.add(
      fromEvent<PointerEvent>(
        this.host.nativeElement,
        'pointerleave',
      ).subscribe(() => this.close()),
    );
  }

  private open(target: HTMLElement): void {
    /* ─── 1. Read & validate the data-* attributes ───────────────────── */

    const { base, bonus, display, attr, scale, attrvalue } =
      target.dataset as Record<string, string>;

    if (base == null || bonus == null || display == null || attrvalue == null) {
      console.warn(
        'essence‑tooltip: <span> is missing required data-* attributes',
        target,
      );
      return; // silently fail instead of throwing in prod
    }

    const data: EssenceAbilityData = {
      base: Number(base),
      bonus: Number(bonus),
      scale: Number(scale),
      total: display,
      attr: attr ?? null,
      attrValue: Number(attrvalue),
    };
    /* ─── 2. Position the overlay relative to the span  ──────────────── */

    const posStrategy = this.overlay
      .position()
      .flexibleConnectedTo(target)
      .withPositions([
        {
          originX: 'center',
          originY: 'top',
          overlayX: 'center',
          overlayY: 'bottom',
          offsetY: -8,
        },
        {
          originX: 'center',
          originY: 'bottom',
          overlayX: 'center',
          overlayY: 'top',
          offsetY: 8, // fallback if not enough space above
        },
      ]);

    /* ─── 3. Re‑use or create the overlay  ───────────────────────────── */

    if (this.overlayRef) {
      this.overlayRef.updatePositionStrategy(posStrategy);
    } else {
      this.overlayRef = this.overlay.create({
        positionStrategy: posStrategy,
        scrollStrategy: this.overlay.scrollStrategies.reposition(),
        hasBackdrop: false,
        panelClass: 'ability-tooltip-panel', // style hook
      });
    }

    /* ─── 4. Attach the component portal  ────────────────────────────── */

    if (!this.overlayRef.hasAttached()) {
      const portal = new ComponentPortal(
        AbilityTooltipComponent,
        undefined,
        Injector.create({
          providers: [{ provide: ESSENCE_ABILITY_DATA, useValue: data }],
          parent: this.injector,
        }),
      );
      this.overlayRef.attach(portal);
    }

    /* ─── 5. Close when pointer leaves BOTH span and tooltip ─────────── */

    // remove any previous listeners
    this.overlayRef.overlayElement.onpointerover = null;
    this.overlayRef.overlayElement.onpointerout = null;

    const closeIfOutside = (ev: PointerEvent) => {
      const to = ev.relatedTarget as HTMLElement | null;
      if (
        !to ||
        (!target.contains(to) && !this.overlayRef!.overlayElement.contains(to))
      ) {
        this.close();
      }
    };

    target.onpointerout = closeIfOutside;
    this.overlayRef.overlayElement.onpointerout = closeIfOutside;
  }

  private close() {
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
  }

  ngOnDestroy() {
    this.sub.unsubscribe();
    this.close();
  }
}
