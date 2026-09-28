import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { OperatorContextService } from './operator-context.service';

export const sessionRecoveryInterceptor: HttpInterceptorFn = (request, next) => {
  const operator = inject(OperatorContextService);
  return next(request).pipe(catchError((error: unknown) => {
    if (error instanceof HttpErrorResponse && error.status === 401 && operator.session) operator.sessionExpired = true;
    return throwError(() => error);
  }));
};
