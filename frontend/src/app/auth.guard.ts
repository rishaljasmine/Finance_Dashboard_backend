import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

// Runs on the SSR server as well as in the browser. It asks .NET who the
// caller is (GET /api/me, cookie forwarded), so an unauthenticated request for
// a protected page gets a real HTTP 302 to /login instead of a rendered
// dashboard. This is a UX redirect only — every API call is still
// authenticated and authorised by .NET.
export const authGuard: CanActivateFn = () => {

  const router = inject(Router);

  return inject(AuthService).ensureUser().pipe(
    map(user => user ? true : router.createUrlTree(['/login']))
  );
};
