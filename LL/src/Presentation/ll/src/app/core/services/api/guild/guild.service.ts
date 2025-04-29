import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { Guild } from '../../../../shared/models/Dtos/guild/guild';

@Injectable({
  providedIn: 'root',
})
export class GuildService {
  constructor(private api: ApiService) {}

  create(name: string): Observable<void> {
    return this.api.post('guild/createGuild', name).pipe(
      map((opponents) => {
        console.log('testing');
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getMyGuild(): Observable<Guild> {
    return this.api.get('guild/getMyGuild').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getAll(): Observable<Guild[]> {
    return this.api.get('guild/getAllGuilds').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  invite(guildId: string, characterId: string): Observable<void> {
    return this.api.post('guild', guildId).pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  accept(guildId: string): Observable<void> {
    return this.api.post('guild/acceptInvite', guildId).pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  leave(): Observable<void> {
    return this.api.get('guild/leave').pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }
}
