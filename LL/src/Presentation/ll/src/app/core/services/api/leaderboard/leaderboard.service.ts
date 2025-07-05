import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { Leaderboard } from '../../../../shared/models/Dtos/leaderboard/leaderboard';

@Injectable({
  providedIn: 'root',
})
export class LeaderboardService {
  constructor(private api: ApiService) {}

  getLeaderboard(): Observable<Leaderboard> {
    return this.api.get('Leaderboard');
  }
}
