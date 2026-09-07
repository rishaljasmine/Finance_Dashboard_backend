import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  provideHttpClient,
  withInterceptorsFromDi
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';

import { LoginComponent } from './login';
import { routes } from '../app.routes';


describe('LoginComponent', () => {

  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let httpTesting: HttpTestingController;


  beforeEach(async () => {

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent
      ],

      providers: [
        provideRouter(routes),

        provideHttpClient(
          withInterceptorsFromDi()
        ),

        provideHttpClientTesting()
      ]

    }).compileComponents();


    fixture = TestBed.createComponent(
      LoginComponent
    );

    component = fixture.componentInstance;

    httpTesting = TestBed.inject(
      HttpTestingController
    );

    await fixture.whenStable();

  });


  afterEach(() => {

    httpTesting.verify();

  });


  // =====================================================
  // 1. COMPONENT CREATION
  // =====================================================

  it('should create', () => {

    expect(component).toBeTruthy();

  });


  // =====================================================
  // 2. EMPTY USERNAME
  // =====================================================

  it('should show username error when username is empty', () => {

    component.username = '';
    component.email = 'test@gmail.com';
    component.password = '123456';

    component.login();

    expect(component.loginError()).toBe(
      'Please enter your username.'
    );

  });


  // =====================================================
  // 3. EMPTY EMAIL
  // =====================================================

  it('should show email error when email is empty', () => {

    component.username = 'rishal';
    component.email = '';
    component.password = '123456';

    component.login();

    expect(component.loginError()).toBe(
      'Please enter your email address.'
    );

  });


  // =====================================================
  // 4. INVALID EMAIL
  // =====================================================

  it('should show error when email is invalid', () => {

    component.username = 'rishal';
    component.email = 'invalid-email';
    component.password = '123456';

    component.login();

    expect(component.loginError()).toBe(
      'Please enter a valid email address.'
    );

  });


  // =====================================================
  // 5. EMPTY PASSWORD
  // =====================================================

  it('should show password error when password is empty', () => {

    component.username = 'rishal';
    component.email = 'test@gmail.com';
    component.password = '';

    component.login();

    expect(component.loginError()).toBe(
      'Please enter your password.'
    );

  });


  // =====================================================
  // 6. SUCCESSFUL LOGIN
  // =====================================================

  it('should login successfully with valid credentials', () => {

    component.username = 'rishal';
    component.email = 'test@gmail.com';
    component.password = '12345678';

    component.login();


    const request = httpTesting.expectOne(
      'http://127.0.0.1:8000/api/auth/login'
    );


    expect(request.request.method).toBe(
      'POST'
    );


    expect(request.request.body).toEqual({

      username: 'rishal',
      email: 'test@gmail.com',
      password: '12345678'

    });


    request.flush({

      success: true,
      message: 'Login successful.',
      id: 1,
      username: 'rishal',
      email: 'test@gmail.com',
      token: 'test-token'

    });


    expect(component.loginError()).toBe('');

  });


  // =====================================================
  // 7. WRONG CREDENTIALS
  // =====================================================

  it('should show wrong credentials error when login returns 401', () => {

    component.username = 'rishal';
    component.email = 'test@gmail.com';
    component.password = 'wrongpassword';

    component.login();


    const request = httpTesting.expectOne(
      'http://127.0.0.1:8000/api/auth/login'
    );


    expect(request.request.method).toBe(
      'POST'
    );


    request.flush(

      {
        detail: 'Invalid username/email or password.'
      },

      {
        status: 401,
        statusText: 'Unauthorized'
      }

    );


    expect(component.loginError()).toBe(
      'Wrong credentials. Please check your username, email and password.'
    );


    expect(component.submitting()).toBe(false);

  });


  // =====================================================
  // 8. VALID EMAIL
  // =====================================================

  it('should return true for a valid email', () => {

    component.email = 'test@gmail.com';

    expect(component.validEmail).toBe(true);

  });


  // =====================================================
  // 9. INVALID EMAIL
  // =====================================================

  it('should return false for an invalid email', () => {

    component.email = 'invalid-email';

    expect(component.validEmail).toBe(false);

  });

});