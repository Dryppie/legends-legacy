import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../models/Dtos/equipment-slots/equipmentSlot';
import { ModalService } from '../../../core/services/client-side/modal/modal.service';
import { ItemComponent } from '../item/item.component';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import { EquipmentType } from '../../models/enums/equipmentType';

interface EquipmentSlotGuidance {
  label: string;
  accepts: string;
}

@Component({
  selector: 'app-equipment-overview',
  imports: [NgFor, NgIf, NgClass, DecimalPipe, ItemComponent],
  templateUrl: './equipment-overview.component.html',
})
export class EquipmentOverviewComponent implements OnInit {
  @Input() inlineSelection = false;
  @Input() selectedSlotType: EquipmentSlotType | null = null;
  @Output() readonly slotSelected = new EventEmitter<EquipmentSlot>();

  readonly slotGuidance: Record<EquipmentSlotType, EquipmentSlotGuidance> = {
    [EquipmentSlotType.Head]: {
      label: 'Head',
      accepts: 'Head armor',
    },
    [EquipmentSlotType.Chest]: {
      label: 'Chest',
      accepts: 'Chest armor',
    },
    [EquipmentSlotType.Legs]: {
      label: 'Legs',
      accepts: 'Leg armor',
    },
    [EquipmentSlotType.Relic]: {
      label: 'Relic',
      accepts: 'Relics',
    },
    [EquipmentSlotType.Necklace]: {
      label: 'Necklace',
      accepts: 'Necklaces',
    },
    [EquipmentSlotType.Ring]: {
      label: 'Ring',
      accepts: 'Rings',
    },
    [EquipmentSlotType.MainHand]: {
      label: 'Main hand',
      accepts: 'One- or two-handed weapons',
    },
    [EquipmentSlotType.OffHand]: {
      label: 'Off hand',
      accepts: 'One-handed or off-hand items',
    },
    [EquipmentSlotType.Tool]: {
      label: 'Tool',
      accepts: 'Profession tools',
    },
  };

  isGhost(slot: EquipmentSlot): boolean {
    return (
      slot.equipmentSlotType === EquipmentSlotType.OffHand &&
      slot.equipmentInstance?.equipmentBase.equipmentType ===
        EquipmentType.TwoHanded
    );
  }
  constructor(
    private modalService: ModalService,
    private readonly equipmentState: EquipmentStateService,
  ) {}
  private readonly baseSlots = this.setInitialEquipmentSlots();

  slots = computed(() => {
    const stateSlots = this.equipmentState.equipmentSlots();
    return this.baseSlots.map((slot) => {
      const live = stateSlots.find(
        (s) => s.equipmentSlotType === slot.equipmentSlotType,
      );
      return {
        ...slot,
        ...live,
        iconPath: slot.iconPath, // ensure custom iconPath is preserved
      };
    });
  });

  ngOnInit(): void {
    this.equipmentState.load();
  }

  handleSlotClick(equipmentSlot: EquipmentSlot) {
    if (this.inlineSelection) {
      this.slotSelected.emit(equipmentSlot);
      return;
    }

    this.modalService.toggleOverviewEquipItemModal(
      equipmentSlot.equipmentSlotType,
    );
  }

  private setInitialEquipmentSlots(): EquipmentSlot[] {
    return [
      {
        id: '',
        iconPath: 'empty_head',
        equipmentSlotType: EquipmentSlotType.Head,
      },
      {
        id: '',
        iconPath: 'empty_chest',
        equipmentSlotType: EquipmentSlotType.Chest,
      },
      {
        id: '',
        iconPath: 'empty_legs',
        equipmentSlotType: EquipmentSlotType.Legs,
      },
      {
        id: '',
        iconPath: 'empty_relic',
        equipmentSlotType: EquipmentSlotType.Relic,
      },
      {
        id: '',
        iconPath: 'empty_necklace',
        equipmentSlotType: EquipmentSlotType.Necklace,
      },
      {
        id: '',
        iconPath: 'empty_ring',
        equipmentSlotType: EquipmentSlotType.Ring,
      },
      {
        id: '',
        iconPath: 'empty_mainhand',
        equipmentSlotType: EquipmentSlotType.MainHand,
      },
      {
        id: '',
        iconPath: 'empty_offhand',
        equipmentSlotType: EquipmentSlotType.OffHand,
      },
      {
        id: '',
        iconPath: 'empty_tool',
        equipmentSlotType: EquipmentSlotType.Tool,
      },
    ];
  }
}
