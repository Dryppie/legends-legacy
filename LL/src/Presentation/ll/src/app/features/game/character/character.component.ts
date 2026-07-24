import { Component, Injectable } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
@Component({
    selector: 'app-character',
    imports: [RouterOutlet],
    templateUrl: './character.component.html',
    providers: []
})
export class CharacterComponent {}
