import { Component, Injectable } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
@Component({
  selector: 'app-character',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './character.component.html',
  styleUrl: './character.component.css',
  providers: [],
})
export class CharacterComponent {}
