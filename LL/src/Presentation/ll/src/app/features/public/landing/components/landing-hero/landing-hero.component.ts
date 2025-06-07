import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing-hero',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing-hero.component.html',
})
export class LandingHeroComponent {}
