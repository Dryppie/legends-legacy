// shared/help/help.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { shareReplay } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { Guide } from './help.models';

export interface HelpEntry {
  title: string;
  body: string;
}

@Injectable({ providedIn: 'root' })
export class HelpService {
  constructor(
    private http: HttpClient,
  ) {}

  loadGuide(pageId: string): Observable<Guide> {
    return this.load<Guide>(`assets/help/guides/${pageId}.json`);
  }

  private cache = new Map<string, any>();

  load(locale?: string): Observable<Record<string, HelpEntry>>;
  load<T = unknown>(url: string): Observable<T>;
  load<T = unknown>(param = 'en'): Observable<any> {
    const looksLikeLocale = !param.includes('/') && !param.includes('.');

    if (looksLikeLocale) {
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

    if (!this.cache.has(param)) {
      this.cache.set(
        param,
        this.http
          .get<T>(param)
          .pipe(shareReplay({ bufferSize: 1, refCount: false })),
      );
    }

    return this.cache.get(param)!;
  }
}
