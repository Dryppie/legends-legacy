import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { ParityComponent } from './parity.component';

bootstrapApplication(ParityComponent, { providers: [provideRouter([])] }).catch((e) => console.error(e));
