import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { LeaderboardBoard } from '../../../../shared/models/Dtos/leaderboard/leaderboard';

@Injectable({
  providedIn: 'root',
})
export class LeaderboardService {
  constructor(private api: ApiService) {}

  getLeaderboard(
    boardKey: string,
    cursor: string | null = null,
    search: string | null = null,
  ): Observable<LeaderboardBoard> {
    let params = new HttpParams().set('limit', 50);
    if (cursor) params = params.set('cursor', cursor);
    if (search) params = params.set('search', search);

    return this.api.get(`Leaderboard/${encodeURIComponent(boardKey)}`, params);
  }
}
