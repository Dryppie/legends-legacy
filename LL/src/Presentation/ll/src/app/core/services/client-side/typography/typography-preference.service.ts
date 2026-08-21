import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, signal } from '@angular/core';
import { LocalStorageService } from '../local-storage/local-storage.service';

export type ReadingFont = 'default' | 'readable' | 'system';
export type ReadingFontSize = 'default' | 'large' | 'extra-large';

const READING_FONT_STORAGE_KEY = 'readingFont';
const READING_FONT_SIZE_STORAGE_KEY = 'readingFontSize';

@Injectable({ providedIn: 'root' })
export class TypographyPreferenceService {
  private readonly _readingFont = signal<ReadingFont>('default');
  readonly readingFont = this._readingFont.asReadonly();
  private readonly _readingFontSize = signal<ReadingFontSize>('default');
  readonly readingFontSize = this._readingFontSize.asReadonly();

  constructor(
    private readonly storage: LocalStorageService,
    @Inject(DOCUMENT) private readonly document: Document,
  ) {
    const storedFont = this.storage.get<ReadingFont>(READING_FONT_STORAGE_KEY);
    const storedFontSize = this.storage.get<ReadingFontSize>(
      READING_FONT_SIZE_STORAGE_KEY,
    );

    if (this.isReadingFont(storedFont)) {
      this._readingFont.set(storedFont);
    }
    if (this.isReadingFontSize(storedFontSize)) {
      this._readingFontSize.set(storedFontSize);
    }

    this.applyReadingFont(this._readingFont());
    this.applyReadingFontSize(this._readingFontSize());
  }

  setReadingFont(readingFont: ReadingFont): void {
    this._readingFont.set(readingFont);
    this.storage.set(READING_FONT_STORAGE_KEY, readingFont);
    this.applyReadingFont(readingFont);
  }

  resetReadingFont(): void {
    this._readingFont.set('default');
    this.storage.remove(READING_FONT_STORAGE_KEY);
    this.applyReadingFont('default');
  }

  setReadingFontSize(readingFontSize: ReadingFontSize): void {
    this._readingFontSize.set(readingFontSize);
    this.storage.set(READING_FONT_SIZE_STORAGE_KEY, readingFontSize);
    this.applyReadingFontSize(readingFontSize);
  }

  resetReadingFontSize(): void {
    this._readingFontSize.set('default');
    this.storage.remove(READING_FONT_SIZE_STORAGE_KEY);
    this.applyReadingFontSize('default');
  }

  resetReadingPreferences(): void {
    this.resetReadingFont();
    this.resetReadingFontSize();
  }

  private applyReadingFont(readingFont: ReadingFont): void {
    const root = this.document.documentElement;

    if (readingFont === 'default') {
      root.removeAttribute('data-reading-font');
      return;
    }

    root.setAttribute('data-reading-font', readingFont);
  }

  private isReadingFont(value: ReadingFont | null): value is ReadingFont {
    return value === 'default' || value === 'readable' || value === 'system';
  }

  private applyReadingFontSize(readingFontSize: ReadingFontSize): void {
    const root = this.document.documentElement;

    if (readingFontSize === 'default') {
      root.removeAttribute('data-reading-font-size');
      return;
    }

    root.setAttribute('data-reading-font-size', readingFontSize);
  }

  private isReadingFontSize(
    value: ReadingFontSize | null,
  ): value is ReadingFontSize {
    return value === 'default' || value === 'large' || value === 'extra-large';
  }
}
