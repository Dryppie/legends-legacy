import { Component, ElementRef, ViewChild, OnInit } from '@angular/core';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbuttonComponent } from './navbutton/navbutton.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CharacterBadgeComponent,
    RouterLink,
    RouterLinkActive,
    NavbuttonComponent,
  ],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'], // Corrected to `styleUrls`
})
export class NavbarComponent implements OnInit {
  @ViewChild('progressBar', { static: true })
  progressBar!: ElementRef<HTMLDivElement>;
  @ViewChild('menuButton', { static: true })
  menuButton!: ElementRef<HTMLButtonElement>;
  @ViewChild('menuContent', { static: true })
  menuContent!: ElementRef<HTMLDivElement>;

  constructor(private characterActionsService: CharacterActionsService) {}

  // Duration in seconds for the progress bar to fill
  private readonly duration = 6;

  ngOnInit(): void {
    // this.startProgressBar(); // Initialize progress bar when the component is loaded
    this.setupMenu();
  }

  private setupMenu(): void {
    const menuButtonElement = this.menuButton.nativeElement; // Access the progress bar element
    const menuContentElement = this.menuContent.nativeElement; // Access the progress bar element

    menuButtonElement.addEventListener('click', function () {
      menuContentElement.classList.toggle('hidden');
    });
  }

  private startProgressBar(): void {
    // const progressBarElement = this.progressBar.nativeElement; // Access the progress bar element
    // let startTime = Date.now(); // Initialize the start time
    // const updateProgress = () => {
    //   const elapsedTime = (Date.now() - startTime) / 1000; // Convert to seconds
    //   const progress = Math.min((elapsedTime / this.duration) * 100, 100); // Calculate progress percentage
    //   progressBarElement.style.width = `${progress}%`; // Update the width of the progress bar
    //   if (progress < 100) {
    //     requestAnimationFrame(updateProgress); // Continue updating if not yet filled
    //   } else {
    //     startTime = Date.now(); // Reset start time to restart the progress bar
    //     this.characterActionsService.getCharacterAction().subscribe({
    //       next: (action) => {
    //         console.log('Fetched Character Action:', action); // Handle fetched character action
    //       },
    //       error: (error) => {
    //         console.error('Error fetching character action:', error); // Handle error
    //       },
    //     });
    //     requestAnimationFrame(updateProgress); // Restart the animation loop
    //   }
    // };
    // updateProgress(); // Start the timer
  }
}
