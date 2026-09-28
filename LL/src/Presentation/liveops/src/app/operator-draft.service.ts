import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { LiveOpsApiService } from './liveops-api.service';

export interface DraftHandle {
  key: string; data: Record<string, string>; version: string; saved: string;
  loaded: boolean; dirty: boolean; saving: boolean; conflict: boolean; error: string; updatedAt: string;
  pending?: Promise<void>; timer?: ReturnType<typeof setTimeout>;
}
const emptyVersion = '00000000-0000-0000-0000-000000000000';

@Injectable({ providedIn: 'root' })
export class OperatorDraftService {
  private scope = '';
  private handles = new Map<string, DraftHandle>();
  constructor(private readonly api: LiveOpsApiService) {}
  initialize(subject: string, environment: string): void {
    const scope = JSON.stringify([subject, environment]);
    if (scope !== this.scope) { this.clear(); this.scope = scope; }
  }
  clear(): void { for (const handle of this.handles.values()) clearTimeout(handle.timer); this.handles.clear(); this.scope = ''; }
  get unsaved(): DraftHandle[] { return [...this.handles.values()].filter(x => x.dirty || x.saving); }
  get failures(): DraftHandle[] { return [...this.handles.values()].filter(x => !!x.error); }
  async open(key: string): Promise<DraftHandle> {
    const existing = this.handles.get(key);
    if (existing) { await existing.pending; return existing; }
    const handle: DraftHandle = { key, data: {}, version: emptyVersion, saved: '{}', loaded: false, dirty: false, saving: false, conflict: false, error: '', updatedAt: '' };
    this.handles.set(key, handle);
    await this.reload(handle);
    return handle;
  }
  async reload(handle: DraftHandle): Promise<void> {
    if (handle.saving) await handle.pending;
    clearTimeout(handle.timer);
    handle.loaded = false;
    const scope = this.scope;
    const pending = (async () => {
      try {
        const response = await this.api.operatorDraft(handle.key);
        if (scope !== this.scope) return;
        if (!response.isSuccess || !response.data) throw new Error(response.errorMessage || 'Draft could not be loaded.');
        const content = JSON.parse(response.data.content);
        if (!content || Array.isArray(content) || Object.values(content).some(x => typeof x !== 'string')) throw new Error('Saved draft is invalid.');
        handle.data = content; handle.saved = JSON.stringify(content); handle.version = response.data.version;
        handle.updatedAt = response.data.updatedAt; handle.loaded = true; handle.dirty = false; handle.conflict = false; handle.error = '';
      } catch (error) { handle.error = this.message(error); }
    })();
    handle.pending = pending; await pending;
  }
  update(handle: DraftHandle, data: Record<string, string>): void {
    handle.data = { ...data }; handle.dirty = JSON.stringify(handle.data) !== handle.saved;
    clearTimeout(handle.timer);
    if (handle.dirty && handle.loaded && !handle.conflict) handle.timer = setTimeout(() => void this.save(handle), 500);
  }
  async save(handle: DraftHandle): Promise<void> {
    clearTimeout(handle.timer);
    if (handle.saving) { await handle.pending; if (handle.error) return; }
    if (!handle.loaded || handle.conflict || !handle.dirty) return;
    const content = JSON.stringify(handle.data), version = handle.version, scope = this.scope;
    handle.saving = true; handle.error = '';
    const pending = (async () => {
      try {
        const response = await this.api.saveOperatorDraft(handle.key, version, content);
        if (scope !== this.scope) return;
        if (!response.isSuccess || !response.data) throw new Error(response.errorMessage || 'Draft save failed.');
        handle.version = response.data.version; handle.saved = content; handle.updatedAt = response.data.updatedAt;
        handle.dirty = JSON.stringify(handle.data) !== content;
      } catch (error) { handle.error = this.message(error); handle.conflict = error instanceof HttpErrorResponse && error.status === 409; }
      finally { handle.saving = false; }
    })();
    handle.pending = pending; await pending;
    if (handle.dirty && !handle.error && scope === this.scope) await this.save(handle);
  }
  async flush(): Promise<boolean> {
    await Promise.all([...this.handles.values()].map(x => this.save(x)));
    return this.unsaved.length === 0;
  }
  private message(error: unknown): string {
    return error instanceof HttpErrorResponse ? error.error?.errorMessage ?? 'Draft storage is unavailable. Your text is still in this tab.' : error instanceof Error ? error.message : 'Draft storage is unavailable.';
  }
}
