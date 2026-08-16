import { Injectable } from '@angular/core';
import { ToastComponent } from '../../../../shared/components/toast/toast.component';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toastComponent?: ToastComponent;

  register(toastComponent: ToastComponent): void {
    this.toastComponent = toastComponent;
  }

  showToast(
    title: string,
    message: string,
    isSuccess: boolean,
    position: 't' | 'tl' | 'tr' | 'b' | 'bl' | 'br' = 'tr',
  ): void {
    this.toastComponent?.addToast(
      title,
      message,
      isSuccess ? 'success' : 'error',
      position,
    );
  }
}
