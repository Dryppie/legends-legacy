import { Component, Injectable } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
@Component({
  selector: 'app-character',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './character.component.html',
  styleUrl: './character.component.css',
  providers: [],
})
export class CharacterComponent {}
