import { mergeApplicationConfig, ApplicationConfig } from '@angular/core';
import { provideServerRendering, withRoutes } from '@angular/ssr';
import { appConfig } from './app.config';
import { API_ORIGIN } from './api-backend';
import { serverRoutes } from './app.routes.server';

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    // Where the SSR server reaches the .NET API (server-to-server).
    { provide: API_ORIGIN, useFactory: () => process.env['API_URL'] ?? 'http://127.0.0.1:8002' },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
