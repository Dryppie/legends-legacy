import { Component, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ToastService } from './core/services/client-side/toast/toast.service';
import { CharacterActionsService } from './core/services/api/character-actions/character-actions.service';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';
import { AuthService } from './core/services/api/auth/auth.service';
import { CharacterManagerService } from './core/services/client-side/character-manager/character-manager.service';
import { forkJoin, switchMap, take } from 'rxjs';
import { InventoryService } from './core/services/api/inventory/inventory.service';
import { CharacterService } from './core/services/api/character/character.service';
import { EquipmentService } from './core/services/api/equipment/equipment.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent, ModalContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'll';
  @ViewChild('toast') toastComponent!: ToastComponent;

  constructor(
    private authService: AuthService,
    private characterActionsService: CharacterActionsService,
    private toastService: ToastService,
    private inventoryService: InventoryService,
    private equipmentService: EquipmentService,
  ) {}

  ngOnInit(): void {
    this.authService.isAuthenticated$
      .pipe(
        switchMap((isAuthenticated) => {
          if (isAuthenticated) {
            this.characterActionsService.init();
            return this.loadInitialCharacterData();
          }
          return [];
        }),
        take(1),
      )
      .subscribe();
  }

  loadInitialCharacterData() {
    return forkJoin({
      inventory: this.inventoryService.getInventory(),
      equipment: this.equipmentService.getEquipment(),
    });
  }

  ngOnDestroy(): void {}

  ngAfterViewInit() {
    this.toastService.register(this.toastComponent);
  }
}
