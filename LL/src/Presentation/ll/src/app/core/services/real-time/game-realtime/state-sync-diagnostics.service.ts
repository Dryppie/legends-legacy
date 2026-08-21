import { Injectable } from '@angular/core';

export interface StateSyncMutationTrace {
  method: string;
  url: string;
  timestamp: number;
  revisions: Readonly<Record<string, number>>;
  handledScopes: readonly string[];
  candidateFollowUpGets: string[];
  refreshCallbacks: Array<{ scope: string; key: string }>;
}

export interface StateSyncDiagnosticSnapshot {
  mutationCount: number;
  candidateFollowUpGetCount: number;
  refreshCallbackCount: number;
  mutations: StateSyncMutationTrace[];
}

@Injectable({ providedIn: 'root' })
export class StateSyncDiagnostics {
  private readonly maxMutations = 100;
  private readonly followUpWindowMs = 2_000;
  private readonly mutations: StateSyncMutationTrace[] = [];
  private candidateFollowUpGetCount = 0;
  private refreshCallbackCount = 0;

  recordMutation(
    method: string,
    url: string,
    revisions: Readonly<Record<string, number>>,
    handledScopes: readonly string[],
  ): void {
    this.mutations.push({
      method,
      url,
      timestamp: Date.now(),
      revisions: { ...revisions },
      handledScopes: [...handledScopes],
      candidateFollowUpGets: [],
      refreshCallbacks: [],
    });
    while (this.mutations.length > this.maxMutations) this.mutations.shift();
  }

  recordGet(url: string): void {
    const mutation = this.latestActiveMutation();
    if (!mutation) return;
    mutation.candidateFollowUpGets.push(url);
    this.candidateFollowUpGetCount += 1;
  }

  recordRefresh(scope: string, key: string): void {
    const mutation = this.latestActiveMutation();
    if (!mutation) return;
    mutation.refreshCallbacks.push({ scope, key });
    this.refreshCallbackCount += 1;
  }

  snapshot(): StateSyncDiagnosticSnapshot {
    return {
      mutationCount: this.mutations.length,
      candidateFollowUpGetCount: this.candidateFollowUpGetCount,
      refreshCallbackCount: this.refreshCallbackCount,
      mutations: this.mutations.map((mutation) => ({
        ...mutation,
        revisions: { ...mutation.revisions },
        handledScopes: [...mutation.handledScopes],
        candidateFollowUpGets: [...mutation.candidateFollowUpGets],
        refreshCallbacks: [...mutation.refreshCallbacks],
      })),
    };
  }

  print(): void {
    console.table(
      this.mutations.map((mutation) => ({
        mutation: `${mutation.method} ${mutation.url}`,
        scopes: Object.keys(mutation.revisions).length,
        handled: mutation.handledScopes.length,
        followUpGets: mutation.candidateFollowUpGets.length,
        refreshCallbacks: mutation.refreshCallbacks.length,
      })),
    );
  }

  clear(): void {
    this.mutations.length = 0;
    this.candidateFollowUpGetCount = 0;
    this.refreshCallbackCount = 0;
  }

  private latestActiveMutation(): StateSyncMutationTrace | undefined {
    const mutation = this.mutations[this.mutations.length - 1];
    return mutation && Date.now() - mutation.timestamp <= this.followUpWindowMs
      ? mutation
      : undefined;
  }
}
