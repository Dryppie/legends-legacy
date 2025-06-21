import { InjectionToken } from '@angular/core';

/** Provided at runtime so HelpDrawer knows which guide to load */
export const HELP_PAGE_ID = new InjectionToken<string>('HELP_PAGE_ID');
