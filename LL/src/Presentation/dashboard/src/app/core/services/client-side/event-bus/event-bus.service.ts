import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class EventBusService {
  private logoutSubject = new Subject<void>();
  private currentActionSubject = new Subject<void>();

  // Exposed as an observable so others can subscribe
  get logout$(): Observable<void> {
    return this.logoutSubject.asObservable();
  }

  get currentActionSubject$(): Observable<void> {
    return this.currentActionSubject.asObservable();
  }

  emitLogout() {
    this.logoutSubject.next();
    this.logoutSubject.complete();
  }

  emitFetchCurrentAction() {
    this.currentActionSubject.next();
  }
}
