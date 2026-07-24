import { Location } from '@angular/common';
import { Component } from '@angular/core';

@Component({
    selector: 'app-not-found-page',
    imports: [],
    templateUrl: './not-found-page.component.html'
})
export class NotFoundPageComponent {
  constructor(private location: Location) {}

  goBack() {
    this.location.back(); // Navigates back once through the browser
  }
}
