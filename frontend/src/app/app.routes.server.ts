import { RenderMode, ServerRoute } from '@angular/ssr';

// Every route is server-rendered, including the dashboard. Auth is a cookie
// that the SSR server forwards to .NET (see api-backend.ts / auth.guard.ts),
// and the browser-only pieces (Google Sign-In, the React <finova-sidebar>
// web component, chart canvases) are guarded inside the components, so no
// route needs to opt out.
//
// Pages contain per-user data, so they must never be stored by a shared
// cache or replayed to a different user.
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Server,
    headers: {
      'Cache-Control': 'private, no-store',
    },
  },
];
