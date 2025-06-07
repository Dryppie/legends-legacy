import { Component, AfterViewInit } from '@angular/core';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { Essence } from '../../../../shared/models/essence';
import { EssencesService } from '../../../../core/services/api/essences/essences.service';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';

@Component({
  selector: 'app-temple',
  standalone: true,
  imports: [ButtonComponent, BannerComponent],
  templateUrl: './temple.component.html',
})
export class TempleComponent implements AfterViewInit {
  public equippedEssences: Essence[] = [];
  public inventoryEssences: Essence[] = [];

  constructor(
    private modalService: ModalService,
    private essencesService: EssencesService,
  ) {}

  ngAfterViewInit(): void {
    this.essencesService.equippedAndInventoryEssencesSubject$.subscribe(
      (essences) => {
        this.equippedEssences = essences.equippedEssences;
        this.inventoryEssences = essences.inventoryEssences;
      },
    );

    this.essencesService.getEquippedEssencesAndInventoryEssences().subscribe();
  }

  equipEssence() {
    this.essencesService.equipEssence('00000000-0000-0000-0000-000000000001');
  }

  openAbsorbEssenceModal() {
    const filteredEssences = this.inventoryEssences.filter(
      (essence) =>
        !this.equippedEssences.some(
          (equipped) => equipped.name === essence.name,
        ),
    );
    this.modalService.toggleAbsorbEssenceModal(filteredEssences);
  }

  openRemoveEssenceModal() {
    this.modalService.toggleRemoveEssenceModal(this.equippedEssences);
  }
}
