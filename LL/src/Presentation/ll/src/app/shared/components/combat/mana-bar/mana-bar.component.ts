import { Component, ElementRef, Input, ViewChild } from '@angular/core';

@Component({
  selector: 'app-mana-bar',
  standalone: true,
  imports: [],
  templateUrl: './mana-bar.component.html',
})
export class ManaBarComponent {
  @ViewChild('manaBar', { static: true })
  manaBar!: ElementRef<HTMLDivElement>;

  @Input() mana: number = 100; // Current health
  @Input() maxMana: number = 100; // Max health

  ngAfterViewInit(): void {
    this.updateManaBar();
  }

  ngOnChanges(): void {
    this.updateManaBar();
  }

  updateManaBar() {
    if (this.manaBar) {
      const manaPercentage = (this.mana / this.maxMana) * 100;
      this.manaBar.nativeElement.style.width = `${manaPercentage}%`;
    }
  }
}
