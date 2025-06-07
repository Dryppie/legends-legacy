import { NgClass, NgFor, NgIf, NgStyle } from '@angular/common';
import { Component } from '@angular/core';

type ToastPosition = 't' | 'tl' | 'tr' | 'b' | 'bl' | 'br';

interface Toast {
  id: number;
  title: string;
  message: string;
  type: 'success' | 'warning' | 'error';
  progress: number;
  position: ToastPosition;
}

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [NgClass, NgFor, NgStyle, NgIf],
  templateUrl: './toast.component.html',
})
export class ToastComponent {
  toasts: Toast[] = [];
  private readonly duration = 3;

  addToast(
    title: string,
    message: string,
    type: 'success' | 'warning' | 'error',
    position: ToastPosition = 'tr',
  ) {
    const id = Date.now();
    const newToast: Toast = {
      id,
      title,
      message,
      type,
      progress: 100,
      position,
    };
    this.toasts.push(newToast);

    this.startToastTimer(newToast);
  }

  private startToastTimer(toast: Toast): void {
    const startTime = Date.now();

    const updateProgress = () => {
      const elapsedTime = (Date.now() - startTime) / 1000;
      const progress = Math.max(100 - (elapsedTime / this.duration) * 100, 0);

      toast.progress = progress;

      if (progress > 0) {
        requestAnimationFrame(updateProgress);
      } else {
        this.removeToast(toast.id);
      }
    };

    requestAnimationFrame(updateProgress);
  }

  removeToast(id: number) {
    this.toasts = this.toasts.filter((toast) => toast.id !== id);
  }

  getPositionClass(position: ToastPosition): string {
    switch (position) {
      case 't':
        return 'fixed top-4 left-1/2 transform -translate-x-1/2';
      case 'tl':
        return 'fixed top-4 left-4';
      case 'tr':
        return 'fixed top-4 right-4';
      case 'b':
        return 'fixed bottom-4 left-1/2 transform -translate-x-1/2';
      case 'bl':
        return 'fixed bottom-4 left-4';
      case 'br':
        return 'fixed bottom-4 right-4';
      default:
        return 'fixed top-4 right-4'; // Default to top-right
    }
  }

  getProgressBarColor(type: 'success' | 'warning' | 'error'): string {
    switch (type) {
      case 'success':
        return '#F9DCA0';
      case 'warning':
        return '#FFD21E';
      case 'error':
        return '#D72E34';
      default:
        return '#D72E34';
    }
  }
}
