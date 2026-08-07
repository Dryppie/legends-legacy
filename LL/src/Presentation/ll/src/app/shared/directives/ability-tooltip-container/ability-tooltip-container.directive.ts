import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { Directive, ElementRef, Injector, OnDestroy } from '@angular/core';
import { fromEvent, Subscription } from 'rxjs';
import { map, filter } from 'rxjs/operators';
import { EssenceAbilityData } from '../../components/custom-components/tooltips/ability-tooltip/essenceAbilityData';
import { AbilityTooltipComponent } from '../../components/custom-components/tooltips/ability-tooltip/ability-tooltip.component';
import { ESSENCE_ABILITY_DATA } from '../../components/custom-components/tooltips/ability-tooltip/essence-ability-data.token';

@Directive({
  selector: '[abilityTooltipContainer]',
  standalone: true,
})
export class AbilityTooltipContainerDirective implements OnDestroy {
  private readonly sub = new Subscription();
  private overlayRef?: OverlayRef;
  private activeTarget?: HTMLElement;

  constructor(
    private readonly host: ElementRef<HTMLElement>,
    private readonly overlay: Overlay,
    private readonly injector: Injector,
  ) {
    this.sub.add(
      fromEvent<PointerEvent>(this.host.nativeElement, 'pointerover')
        .pipe(
          map((event) => this.tooltipTarget(event.target)),
          filter((target): target is HTMLElement => target !== null),
        )
        .subscribe((target) => this.open(target)),
    );

    this.sub.add(
      fromEvent<FocusEvent>(this.host.nativeElement, 'focusin')
        .pipe(
          map((event) => this.tooltipTarget(event.target)),
          filter((target): target is HTMLElement => target !== null),
        )
        .subscribe((target) => this.open(target)),
    );

    this.sub.add(
      fromEvent<FocusEvent>(this.host.nativeElement, 'focusout').subscribe(
        (event) => this.closeIfOutside(event.relatedTarget),
      ),
    );

    this.sub.add(
      fromEvent<KeyboardEvent>(this.host.nativeElement, 'keydown')
        .pipe(filter((event) => event.key === 'Escape'))
        .subscribe(() => this.close()),
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
    this.close();
  }

  private tooltipTarget(target: EventTarget | null): HTMLElement | null {
    return target instanceof HTMLElement
      ? target.closest<HTMLElement>('.dmg, .heal, .mod, .keyword')
      : null;
  }

  private open(target: HTMLElement): void {
    if (this.activeTarget === target && this.overlayRef?.hasAttached()) return;

    const data = this.readTooltipData(target);
    if (!data) return;

    this.close();
    this.activeTarget = target;

    const positionStrategy = this.overlay
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
          offsetY: 8,
        },
      ])
      .withPush(true)
      .withViewportMargin(8);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: false,
      panelClass: 'ability-tooltip-panel',
    });

    this.overlayRef.attach(
      new ComponentPortal(
        AbilityTooltipComponent,
        undefined,
        Injector.create({
          providers: [{ provide: ESSENCE_ABILITY_DATA, useValue: data }],
          parent: this.injector,
        }),
      ),
    );

    const closeIfOutside = (event: PointerEvent) =>
      this.closeIfOutside(event.relatedTarget);
    target.onpointerout = closeIfOutside;
    this.overlayRef.overlayElement.onpointerout = closeIfOutside;
  }

  private readTooltipData(target: HTMLElement): EssenceAbilityData | null {
    const dataset = target.dataset as Record<string, string>;
    const kind = dataset['tooltipKind'] ?? 'magnitude';

    if (kind === 'keyword') {
      if (!dataset['title'] || !dataset['description']) return null;
      return {
        kind: 'keyword',
        title: dataset['title'],
        description: dataset['description'],
        detail: dataset['detail'] ?? '',
      };
    }

    const { base, bonus, display, attr, scale, attrvalue, unit, range } =
      dataset;
    if (base == null || bonus == null || display == null || attrvalue == null) {
      console.warn(
        'essence-tooltip: generated magnitude is missing required data attributes',
        target,
      );
      return null;
    }

    return {
      kind: 'magnitude',
      title: dataset['title'] ?? 'Estimated value',
      base: Number(base),
      bonus: Number(bonus),
      scale: Number(scale),
      scaleDisplay: dataset['scaleDisplay'] ?? '',
      total: display,
      attr: attr || null,
      attrValue: Number(attrvalue),
      unit: unit ?? '',
      resultLabel: dataset['resultLabel'] ?? unit ?? '',
      hasRange: range === 'true',
      rollDisplay: dataset['rollDisplay'] ?? '',
      note: dataset['note'] ?? '',
    };
  }

  private closeIfOutside(relatedTarget: EventTarget | null): void {
    const target = relatedTarget instanceof Node ? relatedTarget : null;
    if (
      target &&
      (this.activeTarget?.contains(target) ||
        this.overlayRef?.overlayElement.contains(target))
    ) {
      return;
    }
    this.close();
  }

  private close(): void {
    if (this.activeTarget) this.activeTarget.onpointerout = null;
    if (this.overlayRef) this.overlayRef.overlayElement.onpointerout = null;
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.activeTarget = undefined;
  }
}
