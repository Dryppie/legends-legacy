import { Component } from '@angular/core';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterLeaderboardDto } from '../../../../shared/models/Dtos/characterLeaderboardDto';
import { NgFor } from '@angular/common';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';

@Component({
  selector: 'app-tavern',
  standalone: true,
  imports: [BannerComponent, NgFor],
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
