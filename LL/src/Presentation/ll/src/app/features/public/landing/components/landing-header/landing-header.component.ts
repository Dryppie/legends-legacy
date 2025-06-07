import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-landing-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './landing-header.component.html',
})
export class LandingHeaderComponent {}
