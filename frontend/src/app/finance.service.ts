import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpHeaders, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';


// =====================================================
// UPLOADED FILE
// =====================================================

export interface UploadedFileInfo {
  id: number;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAt: string;
}


// =====================================================
// TRANSACTION
// =====================================================

export interface Transaction {
  id: number;
  type: 'income' | 'expense';
  category: string;
  amount: number;
  date: string;
  files: UploadedFileInfo[];
}


// =====================================================
// NEW TRANSACTION
// =====================================================

export interface NewTransaction {
  type: 'income' | 'expense';
  category: string;
  amount: number;
  date: string;
}


// =====================================================
// LOGIN REQUEST
// =====================================================

export interface LoginRequest {
  username: string;
  email: string;
  password: string;
}


// =====================================================
// REGISTER REQUEST
// =====================================================

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}


// =====================================================
// AUTH USER
// =====================================================

export interface AuthUser {
  id: number;
  username: string;
  email: string;
  token: string;
}


// =====================================================
// FINANCE SERVICE
// =====================================================

@Injectable({
  providedIn: 'root'
})
export class FinanceService {

  // ===================================================
  // BACKEND URL
  // ===================================================

  private readonly baseUrl =
    'http://127.0.0.1:8002/api';


  private readonly transactionsUrl =
    `${this.baseUrl}/transactions`;


  private readonly transactionYearsUrl =
    `${this.baseUrl}/transactions/years`;


  private readonly loginUrl =
    `${this.baseUrl}/auth/login`;


  private readonly registerUrl =
    `${this.baseUrl}/register`;


  private readonly googleLoginUrl =
    `${this.baseUrl}/auth/google`;


  private readonly filesUrl =
    `${this.baseUrl}/files`;


  constructor(
    private http: HttpClient
  ) {}


  // ===================================================
  // AUTH HEADERS
  // ===================================================

  private authHeaders(): HttpHeaders {

    const token = localStorage.getItem('token');

    return token
      ? new HttpHeaders({ Authorization: `Bearer ${token}` })
      : new HttpHeaders();
  }


  // ===================================================
  // GET TRANSACTIONS
  // ===================================================

  getTransactions(
    year: number
  ): Observable<Transaction[]> {

    return this.http.get<Transaction[]>(
      `${this.transactionsUrl}?year=${year}`,
      { headers: this.authHeaders() }
    );
  }


  // ===================================================
  // GET TRANSACTION YEARS
  // ===================================================

  getTransactionYears(): Observable<number[]> {

    return this.http.get<number[]>(
      this.transactionYearsUrl,
      { headers: this.authHeaders() }
    );
  }


  // ===================================================
  // CREATE TRANSACTION
  // ===================================================

  createTransaction(
    transaction: NewTransaction
  ): Observable<Transaction> {

    return this.http.post<Transaction>(
      this.transactionsUrl,
      transaction,
      { headers: this.authHeaders() }
    );
  }


  // ===================================================
  // LOGIN
  // ===================================================

  login(
    request: LoginRequest
  ): Observable<AuthUser> {

    return this.http.post<AuthUser>(
      this.loginUrl,
      request
    );
  }


  // ===================================================
  // REGISTER
  // ===================================================

  register(
    request: RegisterRequest
  ): Observable<AuthUser> {

    return this.http.post<AuthUser>(
      this.registerUrl,
      request
    );
  }


  // ===================================================
  // GOOGLE LOGIN
  // ===================================================

  loginWithGoogle(
    credential: string
  ): Observable<AuthUser> {

    return this.http.post<AuthUser>(
      this.googleLoginUrl,
      { credential }
    );
  }


  // ===================================================
  // UPLOAD A FILE FOR A TRANSACTION (reports progress
  // events so the caller can drive a progress bar)
  // ===================================================

  uploadTransactionFile(
    transactionId: number,
    file: File
  ): Observable<HttpEvent<UploadedFileInfo>> {

    const formData = new FormData();
    formData.append('file', file);

    const request = new HttpRequest(
      'POST',
      `${this.transactionsUrl}/${transactionId}/files`,
      formData,
      {
        headers: this.authHeaders(),
        reportProgress: true
      }
    );

    return this.http.request<UploadedFileInfo>(request);
  }


  // ===================================================
  // DOWNLOAD FILE
  // ===================================================

  downloadFile(
    id: number
  ): Observable<Blob> {

    return this.http.get(
      `${this.filesUrl}/${id}/download`,
      {
        headers: this.authHeaders(),
        responseType: 'blob'
      }
    );
  }

}