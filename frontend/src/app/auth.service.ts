import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { AuthUser, FinanceService } from './finance.service';


// =====================================================
// AUTH SERVICE
// =====================================================
//
// Holds the signed-in user for the current page. There is no token here and
// nothing in localStorage: the session is an HttpOnly cookie owned by .NET,
// and "who am I" is always answered by GET /api/me. That works identically
// during SSR (cookie forwarded by ApiBackend) and in the browser.

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly finance = inject(FinanceService);

  readonly user = signal<AuthUser | null>(null);


  // The signed-in user, or null if there is no valid session. Only a UX
  // hint for redirects — .NET still authorises every API call itself.
  ensureUser(): Observable<AuthUser | null> {

    const current = this.user();

    if (current) {
      return of(current);
    }

    return this.finance.getCurrentUser().pipe(
      tap(user => this.user.set(user)),
      catchError(() => of(null))
    );
  }


  signedIn(user: AuthUser): void {
    this.user.set(user);
  }


  // Ends the session: .NET expires the cookie. Local state is cleared even
  // if that call fails, so the UI never stays "logged in" after Log out.
  logout(): Observable<void> {

    return this.finance.logout().pipe(
      catchError(() => of(undefined)),
      tap(() => this.user.set(null)),
      map(() => undefined)
    );
  }

}
