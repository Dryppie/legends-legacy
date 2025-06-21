import {
  Component,
  inject,
  OnInit,
  HostListener,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { OverlayRef } from '@angular/cdk/overlay';
import { HelpService } from './help.service';
import { HELP_PAGE_ID } from './help.tokens';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

interface Guide {
  title: string;
  sections: { heading: string; body: string }[];
}

@Component({
  selector: 'app-help-drawer',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      @keyframes slide-in {
        from {
          transform: translateX(100%);
        }
        to {
          transform: translateX(0);
        }
      }
    `,
  ],
  template: `
    <aside
      class="animate-slide-in flex h-full flex-col bg-texture text-white shadow-xl sm:border-r sm:border-primary/30"
    >
      <header
        class="flex items-center justify-between border-b border-light_gray/30 p-4"
      >
        <h2 class="text-lg font-semibold text-primary">
          {{ (guide$ | async)?.title }}
        </h2>
        <button (click)="close()" aria-label="Close" class="text-xl">✕</button>
      </header>

      <section
        class="flex-1 space-y-6 overflow-y-auto p-4"
        *ngIf="guide$ | async as guide"
      >
        <article *ngFor="let s of guide.sections">
          <h3 class="mb-1 font-medium">{{ s.heading }}</h3>
          <p [innerHTML]="s.body"></p>
        </article>
      </section>
    </aside>
  `,
})
export class HelpDrawerComponent implements OnInit {
  private pageId = inject(HELP_PAGE_ID);
  private help = inject(HelpService);
  private ovrRef = inject(OverlayRef);

  guide$!: Observable<Guide>;

  ngOnInit() {
    this.guide$ = this.help
      .load<Guide>(`assets/help/guides/${this.pageId}.json`) // new helper below
      .pipe(map((g) => g || { title: 'Help', sections: [] }));
  }

  @HostListener('keydown.escape') close() {
    this.ovrRef.dispose();
  }
}
