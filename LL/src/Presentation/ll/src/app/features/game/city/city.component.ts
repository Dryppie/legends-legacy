import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-city',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './city.component.html',
  styleUrl: './city.component.css',
})
export class CityComponent {}
