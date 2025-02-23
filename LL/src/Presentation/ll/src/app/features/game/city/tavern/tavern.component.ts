import { Component } from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { CharacterService } from '../../../../core/services/character/character.service';
import { CharacterLeaderboardDto } from '../../../../shared/models/Dtos/characterLeaderboardDto';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-tavern',
  standalone: true,
  imports: [DefaultHeaderComponent, NgFor],
  templateUrl: './tavern.component.html',
  styleUrl: './tavern.component.css',
})
export class TavernComponent {
  constructor(private characterService: CharacterService) {
    this.getLeaderboard();
  }

  leaderboard: CharacterLeaderboardDto[] = [];

  getLeaderboard(): void {
    this.characterService.getLeaderboard().subscribe({
      next: (data) => {
        // Store the fetched data in the component property
        this.leaderboard = data;
      },
      error: (err) => {
        console.error('Failed to fetch leaderboard:', err);
      },
    });
  }
}
