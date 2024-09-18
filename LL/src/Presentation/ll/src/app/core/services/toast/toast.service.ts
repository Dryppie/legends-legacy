// toast.service.ts
import { Injectable } from '@angular/core';
import { ToastComponent } from '../../../shared/components/toast/toast.component';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private toastComponent!: ToastComponent;

  register(toastComponent: ToastComponent) {
    this.toastComponent = toastComponent;
  }

  showToast(
    title: string,
    message: string,
    type: 'success' | 'error' = 'success',
    position: 't' | 'tl' | 'tr' | 'b' | 'bl' | 'br' = 'tr',
  ) {
    this.toastComponent?.addToast(title, message, type, position);
  }
}
