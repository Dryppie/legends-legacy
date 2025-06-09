import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { LeaderboardDto } from '../../../../shared/models/Dtos/leaderboard/leaderboardDto';

@Injectable({
  providedIn: 'root',
})
export class LeaderboardService {
  constructor(private api: ApiService) {}

  getLeaderboard(): Observable<LeaderboardDto> {
    return this.api.get('Leaderboard');
  }
}
