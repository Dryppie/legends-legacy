import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-world',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './world.component.html',
})
export class WorldComponent {}
