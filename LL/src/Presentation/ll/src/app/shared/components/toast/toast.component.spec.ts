import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastComponent } from './toast.component';

describe('ToastComponent', () => {
  let fixture: ComponentFixture<ToastComponent>;
  let component: ToastComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => component.ngOnDestroy());

  it('uses an assertive alert for error notifications', () => {
    component.addToast('Unable to save', 'Try again.', 'error');
    fixture.detectChanges();

    const toast = fixture.nativeElement.querySelector('[role="alert"]');
    expect(toast).not.toBeNull();
    expect(toast.getAttribute('aria-live')).toBe('assertive');
  });

  it('provides an explicit dismiss button', () => {
    component.addToast('Saved', 'Your changes were saved.', 'success');
    fixture.detectChanges();

    const dismissButton = fixture.nativeElement.querySelector(
      'button[aria-label^="Dismiss"]',
    ) as HTMLButtonElement;
    dismissButton.click();
    fixture.detectChanges();

    expect(component.toasts.length).toBe(0);
  });
});
