import { NgClass, NgFor, NgIf, NgStyle } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';

type ToastPosition = 't' | 'tl' | 'tr' | 'b' | 'bl' | 'br';

interface Toast {
  id: number;
  title: string;
  message: string;
  type: 'success' | 'warning' | 'error';
  progress: number;
  position: ToastPosition;
  paused: boolean;
  lastFrameTime?: number;
  animationFrameId?: number;
}

@Component({
  selector: 'app-toast',
  imports: [NgClass, NgFor, NgStyle, NgIf],
  templateUrl: './toast.component.html',
})
export class ToastComponent implements OnDestroy {
  toasts: Toast[] = [];
  private readonly durationMs = 8000;

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
      paused: false,
    };
    this.toasts.push(newToast);

    this.startToastTimer(newToast);
  }

  private startToastTimer(toast: Toast): void {
    const updateProgress = (timestamp: number) => {
      const previousTimestamp = toast.lastFrameTime ?? timestamp;
      const elapsed = timestamp - previousTimestamp;
      toast.lastFrameTime = timestamp;

      if (!toast.paused) {
        toast.progress = Math.max(
          toast.progress - (elapsed / this.durationMs) * 100,
          0,
        );
      }

      if (toast.progress > 0) {
        toast.animationFrameId = requestAnimationFrame(updateProgress);
      } else {
        this.removeToast(toast.id);
      }
    };

    toast.animationFrameId = requestAnimationFrame(updateProgress);
  }

  removeToast(id: number): void {
    const toast = this.toasts.find((candidate) => candidate.id === id);
    if (toast?.animationFrameId) cancelAnimationFrame(toast.animationFrameId);
    this.toasts = this.toasts.filter((toast) => toast.id !== id);
  }

  pauseToast(toast: Toast): void {
    toast.paused = true;
  }

  resumeToast(toast: Toast): void {
    toast.paused = false;
  }

  getLiveRole(type: Toast['type']): 'alert' | 'status' {
    return type === 'error' ? 'alert' : 'status';
  }

  trackToast(_index: number, toast: Toast): number {
    return toast.id;
  }

  ngOnDestroy(): void {
    this.toasts.forEach((toast) => {
      if (toast.animationFrameId) cancelAnimationFrame(toast.animationFrameId);
    });
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
        return 'var(--ll-color-success)';
      case 'warning':
        return 'var(--ll-color-warning)';
      case 'error':
        return 'var(--ll-color-danger)';
      default:
        return 'var(--ll-color-primary)';
    }
  }
}
