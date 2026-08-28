import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FinanceService } from '../finance.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class RegisterComponent {

  username = '';
  email = '';
  password = '';
  confirmPassword = '';

  // These are mutated from inside an HttpClient .subscribe()
  // callback, which runs outside anything this zoneless app's
  // change detector tracks on its own — signals are what makes
  // that async update actually reach the view.
  registerError = signal('');
  submitting = signal(false);

  constructor(
    private router: Router,
    private financeService: FinanceService
  ) {}

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
  // PASSWORD VALIDATION
  // =====================================================

  get passwordLength(): boolean {
    return this.password.length >= 8;
  }

  get hasUppercase(): boolean {
    return /[A-Z]/.test(this.password);
  }

  get hasLowercase(): boolean {
    return /[a-z]/.test(this.password);
  }

  get hasNumber(): boolean {
    return /[0-9]/.test(this.password);
  }

  get hasSpecial(): boolean {
    return /[^A-Za-z0-9]/.test(this.password);
  }


  // =====================================================
  // PASSWORD STRENGTH
  // =====================================================

  get passwordStrength(): string {

    if (!this.password) {
      return '';
    }

    let score = 0;

    if (this.passwordLength) score++;
    if (this.hasUppercase) score++;
    if (this.hasLowercase) score++;
    if (this.hasNumber) score++;
    if (this.hasSpecial) score++;

    if (score <= 2) {
      return 'Weak';
    }

    if (score <= 4) {
      return 'Medium';
    }

    return 'Strong';
  }


  // =====================================================
  // REGISTER
  // =====================================================

  register(): void {

    if (this.submitting()) {
      return;
    }

    this.registerError.set('');

    // USERNAME VALIDATION

    if (!this.username.trim()) {
      this.registerError.set(
        'Please enter a username.'
      );
      return;
    }


    // EMAIL VALIDATION

    if (!this.email.trim()) {
      this.registerError.set(
        'Please enter your email address.'
      );
      return;
    }

    if (!this.validEmail) {
      this.registerError.set(
        'Please enter a valid email address.'
      );
      return;
    }


    // PASSWORD VALIDATION

    if (!this.password) {
      this.registerError.set(
        'Please enter a password.'
      );
      return;
    }

    if (this.passwordStrength !== 'Strong') {
      this.registerError.set(
        'Please use a strong password before continuing.'
      );
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.registerError.set(
        'Passwords do not match.'
      );
      return;
    }


    // SEND REGISTER REQUEST

    this.submitting.set(true);

    this.financeService.register({
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.password
    }).subscribe({

      next: () => {

        this.submitting.set(false);

        this.router.navigate(
          ['/login'],
          { state: { registered: true } }
        );
      },

      error: (error) => {

        this.submitting.set(false);

        this.registerError.set(
          error.status === 409
            ? (error.error?.detail || 'Username or email already exists.')
            : error.status === 400
              ? (error.error?.detail || 'Please check the details you entered.')
              : 'Unable to connect to the finance server.'
        );
      }

    });
  }
}
