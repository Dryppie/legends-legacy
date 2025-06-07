import { Component, EventEmitter, Output } from '@angular/core';
import {
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { Subscription } from 'rxjs';
import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RegularButtonComponent } from '../../../shared/components/buttons/regular-button/regular-button.component';
import { StickyScrollDirective } from '../../../shared/directives/sticky-scroll/sticky-scroll.directive';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    FormsModule,
    AsyncPipe,
    RegularButtonComponent,
    StickyScrollDirective,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent {
  @Output() close = new EventEmitter<void>();

  channel = 'global';
  messages: ChatMessageDto[] = [];
  draft = '';
  private subscription?: Subscription;

  constructor(public chat: ChatService) {}

  async ngOnInit() {
    // await this.chat.connectAndLoad(this.channel);
    // this.subscription = this.chat.messages$.subscribe((m) => {
    //   if (m.channel === this.channel) this.messages.push(m);
    // });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  onDraftChange(): void {
    // Trim extra pasted input if over 200
    if (this.draft.length > 200) {
      this.draft = this.draft.slice(0, 200);
    }
  }

  async send() {
    const msg = this.draft.trim();
    if (!msg) return;
    this.draft = '';
    await this.chat.send(this.channel, msg);
  }
}
