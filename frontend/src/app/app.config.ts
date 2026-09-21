import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HttpBackend, provideHttpClient } from '@angular/common/http';
import { provideCharts, withDefaultRegisterables } from 'ng2-charts';

import { routes } from './app.routes';
import { ApiBackend } from './api-backend';
import { provideClientHydration } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    // Must come after provideHttpClient() so it replaces the default backend.
    { provide: HttpBackend, useClass: ApiBackend },
    provideCharts(withDefaultRegisterables()),
    // Also enables the HTTP transfer cache: GET responses fetched while
    // rendering on the server are embedded in the HTML and reused by the
    // browser during hydration instead of being requested again.
    provideClientHydration(),
  ],
};
