import { Component, AfterViewInit } from '@angular/core';
import { ButtonComponent } from '../../button/button.component';
import { EssencesService } from '../../../../core/services/essences/essences.service';
import { BannerComponent } from '../../banner/banner.component';
import { DefaultHeaderComponent } from '../../default-header/default-header.component';
import { ModalService } from '../../../../core/services/modal/modal.service';
import { Essence } from '../../../models/essence';

@Component({
  selector: 'app-temple',
  standalone: true,
  imports: [ButtonComponent, BannerComponent, DefaultHeaderComponent],
  templateUrl: './temple.component.html',
  styleUrl: './temple.component.css',
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
    this.modalService.toggleAbsorbEssenceModal(this.inventoryEssences);
  }

  openRemoveEssenceModal() {
    this.modalService.toggleRemoveEssenceModal(this.equippedEssences);
  }
}
