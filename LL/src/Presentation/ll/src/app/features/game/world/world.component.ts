import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-world',
  imports: [RouterOutlet],
  templateUrl: './world.component.html',
  host: {
    class: 'block h-full min-h-0',
  },
})
export class WorldComponent {}
