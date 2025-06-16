import {
  Component,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import {
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { Subscription } from 'rxjs';
import { AsyncPipe, DatePipe, NgFor, NgIf } from '@angular/common';
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
    DatePipe,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent implements OnInit, OnDestroy {
  @Output() close = new EventEmitter<void>();

  channel = 'global';
  messages: ChatMessageDto[] = [];
  draft = '';
  private subscription?: Subscription;

  constructor(public chat: ChatService) {}

  async ngOnInit() {
    // await this.chat.connectAndLoad(this.channel);
    // this.subscription = this.chat.messages$.subscribe((m) => {
    //   if (m.channel === this.channel) {
    //     this.messages.push(m);
    //   }
    // });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  onDraftChange(): void {
    if (this.draft.length > 200) {
      this.draft = this.draft.slice(0, 200);
    }
  }

  async send(): Promise<void> {
    const msg = this.draft.trim();
    if (!msg || !isMessageAllowed(msg)) return;

    this.draft = '';
    await this.chat.send(this.channel, msg);
  }
}

const COMBINING_MARKS_PATTERN = /\p{M}/gu;

/**
 * ≥20 consecutive repetitions of the exact same *visible* code‑point.
 *   – Letters, Numbers, Symbols, Punctuation are considered visible.
 */
const REPEATED_CHAR_PATTERN = /([\p{L}\p{N}\p{S}\p{P}])\1{19,}/u;

/** C0 + C1 control codes (except TAB, LF, CR which we strip earlier) */
const CONTROL_CHARS_PATTERN = /[\u0000-\u001F\u007F-\u009F]/u;

/** Explicitly forbidden literals (e.g. Arabic ligature ﷽ U+FDFD) */
const FORBIDDEN_LITERALS = /\uFDFD/u;

// 2️⃣  Public API ------------------------------------------------------------

export interface FilterOptions {
  /** Reject if visible length exceeds this (after trimming whitespace). */
  maxVisibleLength?: number;
  /** Absolute number of combining marks allowed before we reject. */
  maxCombiningChars?: number;
  /** combiningCount / visibleLength > ratio triggers reject. */
  maxCombiningRatio?: number;
}

/**
 * Returns `true` if the message is acceptable, `false` if it should be blocked.
 */
export function isMessageAllowed(
  text: string,
  opts: FilterOptions = {},
): boolean {
  const {
    maxVisibleLength = 4000,
    maxCombiningChars = 20,
    maxCombiningRatio = 0.2,
  } = opts;

  // 1. Strip all whitespace so we can reason about *visible* glyphs.
  const visible = text.replace(/\s+/g, '');

  if (visible.length === 0) return false; // empty
  if (visible.length > maxVisibleLength) return false; // too long

  // 2. Block raw control chars (layout / security hazards)
  if (CONTROL_CHARS_PATTERN.test(text)) return false;

  // 3. Explicit single‑codepoint bans (﷽ etc.)
  if (FORBIDDEN_LITERALS.test(text)) return false;

  // 4. Super‑long runs of the *exact* same glyph
  if (REPEATED_CHAR_PATTERN.test(text)) return false;

  // 5. Zalgo / garbage combining mark density check
  const combiningCount = (text.match(COMBINING_MARKS_PATTERN) || []).length;
  const density = combiningCount / Math.max(1, visible.length);
  if (combiningCount > maxCombiningChars && density > maxCombiningRatio) {
    return false;
  }

  return true; // ✅ looks good
}
