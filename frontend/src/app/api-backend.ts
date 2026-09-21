import { isPlatformServer } from '@angular/common';
import { FetchBackend, HttpBackend, HttpEvent, HttpRequest } from '@angular/common/http';
import { inject, Injectable, InjectionToken, PLATFORM_ID, REQUEST } from '@angular/core';
import { Observable } from 'rxjs';


// =====================================================
// API ORIGIN
// =====================================================
//
// Where the .NET API lives. In the browser it's the page's own hostname on
// port 8002 — matching the hostname matters, because the session cookie is
// host-scoped (localhost and 127.0.0.1 are different hosts to a browser).
// The SSR server overrides this in app.config.server.ts (API_URL env var).

export const API_ORIGIN = new InjectionToken<string>('API_ORIGIN', {
  providedIn: 'root',
  factory: () => `${location.protocol}//${location.hostname}:8002`
});


// =====================================================
// API BACKEND
// =====================================================
//
// App code (FinanceService) only ever asks for relative '/api/...' URLs.
// This wraps the real HttpBackend and is the one place that turns those into
// authenticated calls to .NET:
//
//   browser -> .NET   credentials: 'include', so the browser attaches the
//                     HttpOnly session cookie (JS never sees the token).
//   SSR     -> .NET   the incoming page request's Cookie header is forwarded,
//                     so .NET authenticates the same user the browser is.
//
// It sits BELOW every interceptor on purpose: Angular's HTTP transfer cache
// keys on the URL it sees, so the server-rendered response is reused by the
// browser only if both sides log the identical '/api/...' URL.
//
// Cookies are only ever attached to '/api/' requests, i.e. only sent to
// API_ORIGIN — never to any third-party URL.

@Injectable()
export class ApiBackend implements HttpBackend {

  private readonly delegate = inject(FetchBackend);
  private readonly origin = inject(API_ORIGIN);
  private readonly isServer = isPlatformServer(inject(PLATFORM_ID));
  private readonly incomingRequest = inject(REQUEST, { optional: true });
  private readonly selfOrigin = this.incomingRequest ? new URL(this.incomingRequest.url).origin : null;

  handle(req: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {

    const apiPath = this.apiPath(req.url);

    if (apiPath === null) {
      return this.delegate.handle(req);
    }

    if (!this.isServer) {
      return this.delegate.handle(
        req.clone({ url: this.origin + apiPath, withCredentials: true })
      );
    }

    const cookie = this.incomingRequest?.headers.get('cookie');

    return this.delegate.handle(
      req.clone({
        url: this.origin + apiPath,
        setHeaders: cookie ? { Cookie: cookie } : {}
      })
    );
  }


  // The '/api/...' path if this request is meant for the .NET API, else null.
  // During SSR, Angular's built-in relative-URL transformer runs before this
  // backend and has already turned '/api/x' into '<ssr origin>/api/x', so
  // that form has to be recognised too.
  private apiPath(url: string): string | null {

    if (url.startsWith('/api/')) {
      return url;
    }

    if (this.selfOrigin && url.startsWith(`${this.selfOrigin}/api/`)) {
      return url.slice(this.selfOrigin.length);
    }

    return null;
  }

}
