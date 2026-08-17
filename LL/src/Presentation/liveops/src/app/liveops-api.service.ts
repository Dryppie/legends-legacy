import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  ApiResponse,
  ItemCatalogEntry,
  OperatorSession,
  PlayerDetails,
  PlayerSummary,
} from './liveops.models';

@Injectable({ providedIn: 'root' })
export class LiveOpsApiService {
  private antiforgeryToken = '';

  constructor(private readonly http: HttpClient) {}

  session(): Promise<OperatorSession> {
    return firstValueFrom(this.http.get<OperatorSession>('/auth/session'));
  }

  async initializeAntiforgery(): Promise<void> {
    const result = await firstValueFrom(
      this.http.get<{ requestToken: string }>('/auth/antiforgery'),
    );
    this.antiforgeryToken = result.requestToken;
  }

  searchPlayers(query: string): Promise<ApiResponse<PlayerSummary[]>> {
    const params = new HttpParams().set('query', query).set('limit', 20);
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerSummary[]>>('/api/liveops/players', {
        params,
      }),
    );
  }

  playerDetails(characterId: string): Promise<ApiResponse<PlayerDetails>> {
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerDetails>>(
        `/api/liveops/players/${characterId}`,
      ),
    );
  }

  searchItems(query: string): Promise<ApiResponse<ItemCatalogEntry[]>> {
    const params = new HttpParams().set('query', query).set('limit', 20);
    return firstValueFrom(
      this.http.get<ApiResponse<ItemCatalogEntry[]>>('/api/liveops/items', {
        params,
      }),
    );
  }

  ban(accountId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(`/api/liveops/accounts/${accountId}/bans`, body);
  }

  unban(restrictionId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/accounts/bans/${restrictionId}/revoke`,
      body,
    );
  }

  mute(characterId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/chat/characters/${characterId}/mutes`,
      body,
    );
  }

  unmute(restrictionId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/chat/mutes/${restrictionId}/revoke`,
      body,
    );
  }

  grantItems(characterId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/characters/${characterId}/item-grants`,
      body,
    );
  }

  logout(): Promise<unknown> {
    return firstValueFrom(
      this.http.post('/auth/logout', {}, { headers: this.mutationHeaders() }),
    );
  }

  private post<T>(path: string, body: object): Promise<ApiResponse<T>> {
    return firstValueFrom(
      this.http.post<ApiResponse<T>>(path, body, {
        headers: this.mutationHeaders(),
      }),
    );
  }

  private mutationHeaders(): HttpHeaders {
    if (!this.antiforgeryToken) {
      throw new Error('The operator session is not ready. Refresh and try again.');
    }
    return new HttpHeaders({ 'X-XSRF-TOKEN': this.antiforgeryToken });
  }
}
