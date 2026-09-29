import { DraftHandle, OperatorDraftService } from '../operator-draft.service';

/** Private preparation only. Field lists deliberately exclude previews and confirmations. */
export class PreparationDraft {
  handle?: DraftHandle;
  loading = false;
  private generation = 0;
  private defaults: Record<string, unknown> = {};
  constructor(private readonly drafts: OperatorDraftService | undefined, private readonly owner: object,
    private readonly fields: readonly string[]) {}
  get blocked(): boolean { return this.loading || !!this.handle && (!this.handle.loaded || this.handle.conflict); }
  async open(key: string): Promise<void> {
    const generation = ++this.generation;
    this.handle = undefined;
    this.defaults = Object.fromEntries(this.fields.map(key => [key, structuredClone(Reflect.get(this.owner, key))]));
    if (!this.drafts) return;
    this.loading = true;
    const handle = await this.drafts.open(key);
    if (generation !== this.generation) return;
    this.handle = handle; this.loading = false; this.restore();
  }
  restore(): void {
    if (!this.handle?.loaded) return;
    for (const key of this.fields) {
      let value = this.defaults[key];
      try { if (key in this.handle.data) value = JSON.parse(this.handle.data[key]); } catch { /* retain default */ }
      const initial = this.defaults[key];
      if (Array.isArray(initial) ? !Array.isArray(value) : initial !== null && typeof value !== typeof initial) value = initial;
      if (key === 'selectedItem' && value !== null && (typeof value !== 'object' || typeof (value as { id?: unknown }).id !== 'string' || typeof (value as { name?: unknown }).name !== 'string')) value = initial;
      Reflect.set(this.owner, key, structuredClone(value));
    }
  }
  save(): void {
    if (!this.handle?.loaded || this.loading || !this.drafts) return;
    const data: Record<string, string> = {};
    for (const key of this.fields) {
      const value = JSON.stringify(Reflect.get(this.owner, key));
      if (value !== undefined && value !== JSON.stringify(this.defaults[key])) data[key] = value;
    }
    this.drafts.update(this.handle, data);
  }
  detach(): void { this.save(); this.generation++; this.handle = undefined; this.loading = false; }
  discard(): void {
    if (!this.handle || !this.drafts || this.blocked) return;
    this.drafts.update(this.handle, {}); this.restore(); void this.drafts.save(this.handle);
  }
}
