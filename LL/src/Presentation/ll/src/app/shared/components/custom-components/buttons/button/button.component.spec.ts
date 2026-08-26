import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ButtonComponent } from './button.component';

describe('ButtonComponent', () => {
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ButtonComponent);
  });

  it('forwards the disabled and type inputs to the native button', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.componentRef.setInput('type', 'submit');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      'button',
    ) as HTMLButtonElement;

    expect(button.disabled).toBeTrue();
    expect(button.type).toBe('submit');
  });

  it('marks ornamental images as decorative', () => {
    fixture.detectChanges();

    const images = Array.from(
      fixture.nativeElement.querySelectorAll('img'),
    ) as HTMLImageElement[];

    expect(images.length).toBe(2);
    expect(images.every((image) => image.alt === '')).toBeTrue();
  });
});
