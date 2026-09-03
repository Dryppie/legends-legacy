import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EquipmentProgressionService } from '../../../../core/services/api/equipment/equipment-progression.service';
import { hasEquipmentProgressionAccess } from '../../../models/equipment-progression';

@Component({
  selector: 'app-forge-link',
  host: { '[style.display]': "available() ? 'block' : 'none'" },
  standalone: true,
  imports: [RouterLink],
  template: `@if (available()) {
    <a routerLink="/game/character/forge" class="ll-button text-primary"
      >Equipment & Forge</a
    >
  }`,
})
export class ForgeLinkComponent implements OnInit {
  private readonly api = inject(EquipmentProgressionService);
  private readonly destroyRef = inject(DestroyRef);
  readonly available = signal(false);
  ngOnInit(): void {
    this.api
      .access()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (access) => this.available.set(hasEquipmentProgressionAccess(access)),
        error: () => this.available.set(false),
      });
  }
}
