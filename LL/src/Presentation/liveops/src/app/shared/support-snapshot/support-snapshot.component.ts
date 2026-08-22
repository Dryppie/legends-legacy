import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  PlayerSupportSnapshot,
  PlayerTransferHistory,
  TransferConversationPage,
  TransferConversationStatus,
} from '../../liveops.models';

@Component({
  selector: 'app-support-snapshot',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './support-snapshot.component.html',
})
export class SupportSnapshotComponent {
  @Input() snapshot: PlayerSupportSnapshot | null = null;
  @Input() loading = false;
  @Input() error = '';
  @Input() transferLoading = false;
  @Input() transferError = '';
  @Input() selectedTransfer: PlayerTransferHistory | null = null;
  @Input() transferConversation: TransferConversationPage | null = null;
  @Input() transferConversationLoading = false;
  @Input() transferConversationError = '';
  @Output() refresh = new EventEmitter<void>();
  @Output() loadMoreTransfers = new EventEmitter<void>();
  @Output() inspectTransferConversation = new EventEmitter<PlayerTransferHistory>();
  @Output() loadMoreTransferConversation = new EventEmitter<void>();
  @Output() closeTransferConversation = new EventEmitter<void>();
  @Output() copyIdentifier = new EventEmitter<{ value: string; label: string }>();

  copy(value: string, label: string): void {
    this.copyIdentifier.emit({ value, label });
  }

  conversationLabel(status: TransferConversationStatus): string {
    switch (status) {
      case 'EstablishedConversation': return 'Established chat';
      case 'OneWayConversation': return 'One-way chat';
      case 'SharedChannelActivity': return 'Shared channel';
      case 'NoRecordedConversation': return 'No recorded chat';
      default: return 'Chat unavailable';
    }
  }

  conversationTimeline(): Array<{
    id: string;
    kind: 'Message' | 'Transfer';
    occurredAt: string;
    sender: string;
    body: string;
  }> {
    if (!this.selectedTransfer || !this.transferConversation) return [];
    const messages = this.transferConversation.messages.map((message) => ({
      id: message.id,
      kind: 'Message' as const,
      occurredAt: message.sentAt,
      sender: message.senderName,
      body: message.body,
    }));
    const transfer = this.selectedTransfer;
    return [
      ...messages,
      {
        id: `transfer-${transfer.transferId}`,
        kind: 'Transfer' as const,
        occurredAt: transfer.occurredAtUtc,
        sender: `${transfer.senderCharacterName} → ${transfer.recipientCharacterName}`,
        body: `${transfer.quantity.toLocaleString()} × ${transfer.assetName}`,
      },
    ].sort((left, right) => Date.parse(right.occurredAt) - Date.parse(left.occurredAt));
  }
}
