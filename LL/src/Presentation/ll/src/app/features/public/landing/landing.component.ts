import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LandingHeaderComponent } from './components/landing-header/landing-header.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterOutlet, LandingHeaderComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css',
})
export class LandingComponent {}
