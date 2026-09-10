import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { EquipmentInstance } from '../../../../shared/models/item';
import { formatEquipmentLink } from '../../../../shared/utils/chat/chat-equipment-links';

@Injectable({ providedIn: 'root' })
export class ChatEquipmentLinkService {
  private readonly requests = new Subject<string>();
  readonly draftRequests$ = this.requests.asObservable();

  prepare(item: EquipmentInstance): void {
    this.requests.next(
      formatEquipmentLink(item.id, item.displayName || item.itemBase.name),
    );
  }
}
