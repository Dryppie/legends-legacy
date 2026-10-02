import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { APP_VERSION } from './app/core/app-version';
import { lgLoadStyles } from './app/grimoire/core/grimoire-styles';

// Grimoire's stylesheet is its own bundle, loaded without blocking the first render (D-096).
lgLoadStyles(APP_VERSION);

bootstrapApplication(AppComponent, appConfig).catch((err) =>
  console.error(err),
);
