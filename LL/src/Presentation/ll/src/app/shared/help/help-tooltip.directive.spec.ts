import { Overlay } from '@angular/cdk/overlay';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { HelpEntry, HelpService } from './help.service';
import { HelpTooltipDirective } from './help-tooltip.directive';

@Component({
  imports: [HelpTooltipDirective],
  template: '<button appHelp="scrap_mode">?</button>',
})
class TooltipHostComponent {}

describe('HelpTooltipDirective', () => {
  let fixture: ComponentFixture<TooltipHostComponent>;
  let helpEntries: Subject<Record<string, HelpEntry>>;
  let overlay: jasmine.SpyObj<Overlay>;

  beforeEach(async () => {
    helpEntries = new Subject<Record<string, HelpEntry>>();
    overlay = jasmine.createSpyObj<Overlay>('Overlay', ['create', 'position']);

    await TestBed.configureTestingModule({
      imports: [TooltipHostComponent],
      providers: [
        { provide: Overlay, useValue: overlay },
        {
          provide: HelpService,
          useValue: { load: () => helpEntries.asObservable() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TooltipHostComponent);
    fixture.detectChanges();
  });

  it('does not open after the pointer leaves while help is loading', async () => {
    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('button');

    button.dispatchEvent(new MouseEvent('mouseenter'));
    button.dispatchEvent(new MouseEvent('mouseleave'));
    helpEntries.next({
      scrap_mode: { title: 'Scrap Mode', body: 'Choose equipment to scrap.' },
    });
    helpEntries.complete();
    await fixture.whenStable();

    expect(overlay.create).not.toHaveBeenCalled();
  });
});
