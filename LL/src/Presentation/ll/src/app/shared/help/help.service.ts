// shared/help/help.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { shareReplay } from 'rxjs/operators';
import { Observable } from 'rxjs';

export interface HelpEntry {
  title: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class HelpService {
  constructor(private http: HttpClient) {}

  private cache = new Map<string, any>();

  load(locale?: string): Record<string, HelpEntry>;
  load<T = unknown>(url: string): Observable<T>;

  // ────── single *implementation* ──────
  load<T = unknown>(param = 'en'): unknown {
    const looksLikeLocale = !param.includes('/') && !param.includes('.');

    if (looksLikeLocale) {
      // locale variant
      if (!this.cache.has(param)) {
        this.cache.set(
          param,
          this.http
            .get<Record<string, HelpEntry>>(`assets/help/i18n/${param}.json`)
            .pipe(shareReplay({ bufferSize: 1, refCount: false })),
        );
      }
      return this.cache.get(param)!;
    }

    // URL variant
    return this.http.get<T>(param);
  }
}
