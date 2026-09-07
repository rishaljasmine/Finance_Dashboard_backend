import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FinanceService, AuthUser } from '../finance.service';
import { GOOGLE_CLIENT_ID } from '../google-config';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent {

  username = '';
  email = '';
  password = '';

  // The button stays hidden until a Client ID has been configured
  // in google-config.ts (see the Google Cloud Console setup steps).
  googleAvailable = GOOGLE_CLIENT_ID.length > 0;

  // These are mutated from inside an HttpClient .subscribe()
  // callback, which runs outside anything this zoneless app's
  // change detector tracks on its own — signals are what makes
  // that async update actually reach the view.
  loginError = signal('');
  registeredMessage = signal('');
  submitting = signal(false);

  // Shows a skeleton in place of the form briefly on first load,
  // Instagram-style, instead of an instant flash of content.
  pageReady = signal(false);

  constructor(
    private router: Router,
    private financeService: FinanceService
  ) {

    const navigation = this.router.getCurrentNavigation();

    if (navigation?.extras.state?.['registered']) {
      this.registeredMessage.set(
        'Account created successfully. Please log in.'
      );
    }

    setTimeout(() => {

      this.pageReady.set(true);

      // The #google-signin-button container only exists once the
      // skeleton above is swapped out for the real form, so this has
      // to wait for that DOM update rather than running from
      // ngAfterViewInit (which fires while the skeleton is still up).
      setTimeout(() => this.initializeGoogleSignIn(), 0);

    }, 700);
  }

  // =====================================================
  // GOOGLE SIGN-IN
  // =====================================================

  private initializeGoogleSignIn(): void {

    if (!this.googleAvailable) {
      return;
    }

    this.waitForGoogleScript(() => {

      google.accounts.id.initialize({
        client_id: GOOGLE_CLIENT_ID,
        callback: (response: { credential: string }) =>
          this.handleGoogleCredential(response.credential)
      });

      google.accounts.id.renderButton(
        document.getElementById('google-signin-button'),
        {
          theme: 'filled_black',
          size: 'large',
          shape: 'pill',
          width: 320
        }
      );
    });
  }


  private waitForGoogleScript(
    onReady: () => void,
    attemptsLeft = 50
  ): void {

    if (typeof google !== 'undefined' && google.accounts?.id) {
      onReady();
      return;
    }

    if (attemptsLeft <= 0) {
      return;
    }

    setTimeout(
      () => this.waitForGoogleScript(onReady, attemptsLeft - 1),
      100
    );
  }


  private handleGoogleCredential(credential: string): void {

    this.loginError.set('');
    this.submitting.set(true);

    this.financeService.loginWithGoogle(credential).subscribe({

      next: (user) => {
        this.applySession(user);
      },

      error: () => {

        this.submitting.set(false);

        this.loginError.set(
          'Could not sign in with Google. Please try again.'
        );
      }

    });
  }


  private applySession(user: AuthUser): void {

    localStorage.setItem('loggedIn', 'true');
    localStorage.setItem('username', user.username);
    localStorage.setItem('email', user.email);
    localStorage.setItem('token', user.token);

    this.router.navigate(['/dashboard']);
  }


  // =====================================================
  // EMAIL VALIDATION
  // =====================================================

  get validEmail(): boolean {

    if (!this.email.trim()) {
      return false;
    }

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(
      this.email.trim()
    );
  }


  // =====================================================
  // LOGIN
  // =====================================================

  login(): void {

    if (this.submitting()) {
      return;
    }

    this.loginError.set('');

    // USERNAME VALIDATION

    if (!this.username.trim()) {
      this.loginError.set(
        'Please enter your username.'
      );
      return;
    }


    // EMAIL VALIDATION

    if (!this.email.trim()) {
      this.loginError.set(
        'Please enter your email address.'
      );
      return;
    }

    if (!this.validEmail) {
      this.loginError.set(
        'Please enter a valid email address.'
      );
      return;
    }


    // PASSWORD VALIDATION

    if (!this.password) {
      this.loginError.set(
        'Please enter your password.'
      );
      return;
    }


    // SEND LOGIN REQUEST

    this.submitting.set(true);

    this.financeService.login({
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.password
    }).subscribe({

      next: (user) => {
        this.applySession(user);
      },

      error: (error) => {

        this.submitting.set(false);

        this.loginError.set(
          error.status === 401
            ? 'Wrong credentials. Please check your username, email and password.'
            : 'Unable to connect to the finance server.'
        );
      }

    });
  }
}