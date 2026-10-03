import {
  Component,
  DestroyRef,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OverlayModule, Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { finalize } from 'rxjs';
import { ChatComponent, ChatMentionSegmentsPipe } from '../dashboard/chat/chat.component';
import { ChatMentionSuggestionsService } from '../dashboard/chat/chat-mention-suggestions.service';
import { ChatMentionComponent } from '../dashboard/chat/chat-mention.component';
import { ChatEquipmentLinkComponent } from '../dashboard/chat/chat-equipment-link.component';
import { ChatComposerDirective } from '../dashboard/chat/chat-composer.directive';
import { lootHistoryLocationLabel } from '../dashboard/loot-tracker/loot-tracker.component';
import { ItemComponent } from '../../shared/components/item/item.component';
import { ChatChannelType, ChatMessageDto } from '../../core/services/ll-chat/chat-service/chat.service';
import { ChatChannelVisibilityPreferenceService } from '../../core/services/client-side/chat-channel-visibility/chat-channel-visibility-preference.service';
import { ChatLayoutPreferenceService } from '../../core/services/client-side/chat-layout/chat-layout-preference.service';
import { GameRealtimeStore } from '../../core/services/real-time/game-realtime/game-realtime-store.service';
import { LootHistoryStateService } from '../../core/services/api/loot-history/loot-history-state.service';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { NobilityService } from '../../core/services/api/nobility/nobility.service';
import { TimeSyncService } from '../../core/services/api/time-sync/time-sync.service';
import { LootHistoryEntry } from '../../shared/models/loot-history';
import {
  LgButtonComponent,
  LgChronicleAsideComponent,
  LgChronicleComponent,
  LgChronicleComposerComponent,
  LgChronicleMessage,
  LgChronicleMessageDirective,
  LgKeyComponent,
  LG_MENU,
  LG_POPOVER,
  LgCheckboxComponent,
} from '@grimoire';
import { chronicleChannelId, chronicleChannels, lootLine, toChronicleLine, withDayBreaks } from './chat-chronicle';

/**
 * The chat in the new look (decision 5: ported in one pass): the old chat's behaviour — channels, commands, whispers,
 * invites, item links, @mentions, the visible-channels setting — drawn as the Chronicle, with the Loot History as its
 * Loot channel (D-005). Authors open View Profile and Whisper; Nobles carry the crown (D-112).
 */
@Component({
  selector: 'app-chat-grimoire',
  imports: [
    NgTemplateOutlet,
    FormsModule,
    RouterLink,
    OverlayModule,
    ChatMentionSegmentsPipe,
    ChatMentionComponent,
    ChatEquipmentLinkComponent,
    ChatComposerDirective,
    ItemComponent,
    LgChronicleComponent,
    LgChronicleMessageDirective,
    LgChronicleAsideComponent,
    LgChronicleComposerComponent,
    LgButtonComponent,
    LgKeyComponent,
    LgCheckboxComponent,
    ...LG_POPOVER,
    ...LG_MENU,
  ],
  providers: [ChatMentionSuggestionsService],
  templateUrl: './chat-grimoire.component.html',
  styleUrl: './chat-grimoire.component.scss',
  host: { style: 'display: contents' },
})
export class ChatGrimoireComponent extends ChatComponent {
  private readonly visibility = inject(ChatChannelVisibilityPreferenceService);
  private readonly layout = inject(ChatLayoutPreferenceService);
  private readonly realtime = inject(GameRealtimeStore);
  private readonly lootHistory = inject(LootHistoryStateService);
  private readonly actions = inject(CharacterActionsStateService);
  private readonly nobility = inject(NobilityService);
  private readonly time = inject(TimeSyncService);
  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);

  /**
   * Open in the docked layout follows the remembered setting; the floating drawer starts collapsed, as before, and so
   * does the bottom dock on narrow screens (under 60rem), as the old phone dock did.
   */
  protected readonly chronicleOpen = signal(
    this.layout.layout() === 'docked' && window.innerWidth >= 960 ? this.layout.dockedOpen() : false,
  );
  protected readonly viewLoot = signal(false);
  protected readonly clearing = signal(false);

  // ---- Loot (D-005): the Loot History box's entries, as Loot channel lines. Held while offline progress resolves.
  private readonly lootEntries = signal<LootHistoryEntry[]>([]);
  private readonly lootFollow = effect(() => {
    if (this.actions.resolvingOfflineProgress()) return;
    const entries = this.realtime.recentLoot();
    untracked(() => this.lootEntries.set(entries));
  });
  private readonly lootLoad = this.lootHistory.reload().subscribe();
  protected readonly lootVisible = computed(() => this.visibility.isVisible('loot'));

  // ---- Nobles: one lookup per author, cached for the session's chat.
  private readonly nobles = signal<ReadonlyMap<string, boolean>>(new Map());
  private readonly asked = new Set<string>();
  private noble(id: string): boolean {
    if (!id) return false;
    if (!this.asked.has(id)) {
      this.asked.add(id);
      this.nobility.publicAppearance(id).subscribe((a) => {
        const on = !!a.showBadge && !!a.expiresAt && Date.parse(a.expiresAt) > this.time.now();
        // The lines are drawn during rendering, and a cached answer arrives at once: record it after the render.
        if (on) queueMicrotask(() => this.nobles.update((m) => new Map(m).set(id, true)));
      });
    }
    return !!this.nobles().get(id);
  }

  // ---- The Chronicle's view of the old chat's state, recomputed only when an input changes.
  private cache: { key: unknown[]; lines: LgChronicleMessage[]; sources: Map<string | number, ChatMessageDto | LootHistoryEntry> } | null = null;

  protected get activeId(): string {
    if (this.viewLoot()) return 'loot';
    return this.activeRoomKey === 'all' ? 'all' : chronicleChannelId(this.activeRoomType, this.activeRoomKey);
  }

  protected set activeId(id: string) {
    if (id === 'loot') {
      this.viewLoot.set(true);
      return;
    }
    this.viewLoot.set(false);
    const room = this.visibleRooms.find((r) => (r.contextKey === 'all' ? 'all' : chronicleChannelId(r.channelType, r.contextKey)) === id);
    if (room) this.setChannel(room.channelType, room.contextKey);
  }

  protected get channelTabs() {
    const tabs = chronicleChannels(this.visibleRooms, this.lootVisible());
    const key = tabs.map((t) => t.id).join('|');
    if (this.tabsKey !== key) {
      this.tabsKey = key;
      this.tabsValue = tabs;
    }
    return this.tabsValue;
  }
  private tabsKey = '';
  private tabsValue: ReturnType<typeof chronicleChannels> = [];

  protected get lines(): LgChronicleMessage[] {
    const loot = this.viewLoot() || this.activeRoomKey === 'all' ? this.lootEntries() : [];
    const chat = this.viewLoot() ? [] : this.filteredMessages;
    const key = [chat, loot, this.lootVisible(), this.nobles(), this.characterName(), this.activeId];
    if (this.cache && key.every((k, i) => k === this.cache!.key[i])) return this.cache.lines;

    const sources = new Map<string | number, ChatMessageDto | LootHistoryEntry>();
    type Dated = { sentAt: string | Date; line: LgChronicleMessage };
    const ctx = {
      characterId: this.characterId(),
      mentioned: (m: ChatMessageDto) => this.isMentionedByOtherPlayer(m),
      noble: (id: string) => this.noble(id),
    };
    const dated: Dated[] = chat.map((m) => {
      sources.set(m.id, m);
      return { sentAt: m.sentAt, line: toChronicleLine(m, ctx) };
    });
    if (this.viewLoot() || this.lootVisible()) {
      for (const e of loot) {
        const line = lootLine(e);
        sources.set(line.id, e);
        dated.push({ sentAt: e.receivedAt, line });
      }
    }
    dated.sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
    const lines = withDayBreaks(
      dated.map((d) => ({ sentAt: d.sentAt })),
      dated.map((d) => d.line),
      this.activeId,
    );
    this.cache = { key, lines, sources };
    return lines;
  }

  protected chatSource(m: LgChronicleMessage): ChatMessageDto | null {
    const s = this.cache?.sources.get(m.id);
    return s && 'body' in s ? s : null;
  }

  protected lootSource(m: LgChronicleMessage): LootHistoryEntry | null {
    const s = this.cache?.sources.get(m.id);
    return s && 'receivedAt' in s ? s : null;
  }

  protected lootPlace(entry: LootHistoryEntry): string {
    return lootHistoryLocationLabel(entry);
  }

  protected get prefixId(): string {
    return this.activeRoomKey === 'all' ? 'general' : chronicleChannelId(this.activeRoomType, this.activeRoomKey);
  }

  protected get prefixLabel(): string {
    return this.activeRoomKey === 'all' ? 'General' : this.activeMobileRoomLabel;
  }

  protected setOpen(open: boolean): void {
    this.chronicleOpen.set(open);
    this.collapsed = !open;
    if (this.layout.layout() === 'docked') this.layout.setDockedOpen(open);
  }

  // The old chat's "expand to insert an item link" asks its host to open it; here it opens itself.
  private readonly reopen = this.collapsedChange.subscribe((collapsed) => this.setOpen(!collapsed));
  private readonly expandSub = this.expand.subscribe(() => this.setOpen(true));

  protected setLootVisible(visible: boolean): void {
    this.visibility.setVisible('loot', visible);
    if (!visible && this.viewLoot()) this.viewLoot.set(false);
  }

  protected clearLoot(): void {
    if (this.clearing() || !this.lootEntries().length) return;
    this.clearing.set(true);
    this.lootHistory
      .clear()
      .pipe(finalize(() => this.clearing.set(false)))
      .subscribe();
  }

  // ---- Player actions from an author: View Profile and Whisper, in a small Level 2 menu.
  private readonly playerMenuTpl = viewChild.required<TemplateRef<{ $implicit: string }>>('playerMenu');
  private menuRef: OverlayRef | null = null;
  protected openPlayerMenu(event: { message: LgChronicleMessage; element: HTMLElement }): void {
    const source = this.chatSource(event.message);
    if (!source) return;
    const name = source.channelType === ChatChannelType.Whisper ? this.whisperDisplayName(source) : source.senderName;
    this.closePlayerMenu();
    this.menuRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      positionStrategy: this.overlay
        .position()
        .flexibleConnectedTo(event.element)
        .withPositions([
          { originX: 'start', originY: 'bottom', overlayX: 'start', overlayY: 'top', offsetY: 4 },
          { originX: 'start', originY: 'top', overlayX: 'start', overlayY: 'bottom', offsetY: -4 },
        ])
        .withPush(true),
      scrollStrategy: this.overlay.scrollStrategies.close(),
    });
    this.menuRef.backdropClick().subscribe(() => this.closePlayerMenu());
    this.menuRef.keydownEvents().subscribe((e) => {
      if (e.key === 'Escape') {
        this.closePlayerMenu();
        event.element.focus();
      }
    });
    this.menuRef.attach(new TemplatePortal(this.playerMenuTpl(), this.vcr, { $implicit: name }));
    setTimeout(() => (this.menuRef?.overlayElement.querySelector('.lg-menu__item') as HTMLElement | null)?.focus());
  }

  protected closePlayerMenu(): void {
    this.menuRef?.dispose();
    this.menuRef = null;
  }

  protected viewProfile(name: string): void {
    this.closePlayerMenu();
    void this.routerRef.navigate(['/game/character/character-overview'], { queryParams: { characterName: name } });
  }

  protected whisper(name: string): void {
    this.closePlayerMenu();
    this.chat.prepareWhisperToName(name);
  }

  private readonly routerRef = inject(Router);
  private readonly cleanup = inject(DestroyRef).onDestroy(() => {
    this.closePlayerMenu();
    this.lootLoad.unsubscribe();
  });
}
