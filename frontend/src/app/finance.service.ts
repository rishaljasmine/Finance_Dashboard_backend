import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpParams, HttpRequest } from '@angular/common/http';
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

// The session token is deliberately absent: it lives only in an HttpOnly
// cookie set by .NET, so JavaScript never receives it.
export interface AuthUser {
  id: number;
  username: string;
  email: string;
}


// =====================================================
// DASHBOARD SUMMARY (from GET /api/dashboard)
// =====================================================

export interface MonthlySummary {
  month: number;
  income: number;
  expense: number;
}

export interface CategoryExpense {
  category: string;
  amount: number;
}

export interface DashboardSummary {
  totalIncome: number;
  totalExpense: number;
  balance: number;
  monthlySummary: MonthlySummary[];
  expensesByCategory: CategoryExpense[];
}


// =====================================================
// TRANSACTION QUERY FILTERS (for GET /api/transactions)
// =====================================================

export interface TransactionQuery {
  type?: 'income' | 'expense';
  category?: string;
  minAmount?: number;
  maxAmount?: number;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: 'date' | 'amount' | 'category' | 'type';
  sortOrder?: 'asc' | 'desc';
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
  //
  // Relative on purpose. api-backend.ts resolves '/api/...' to the .NET
  // origin and attaches the session cookie, in the browser and during SSR.

  private readonly baseUrl =
    '/api';


  private readonly transactionsUrl =
    `${this.baseUrl}/transactions`;


  private readonly transactionYearsUrl =
    `${this.baseUrl}/transactions/years`;


  private readonly dashboardUrl =
    `${this.baseUrl}/dashboard`;


  private readonly loginUrl =
    `${this.baseUrl}/auth/login`;


  private readonly registerUrl =
    `${this.baseUrl}/register`;


  private readonly googleLoginUrl =
    `${this.baseUrl}/auth/google`;


  private readonly logoutUrl =
    `${this.baseUrl}/auth/logout`;


  private readonly meUrl =
    `${this.baseUrl}/me`;


  private readonly filesUrl =
    `${this.baseUrl}/files`;


  constructor(
    private http: HttpClient
  ) {}


  // ===================================================
  // Authentication is the HttpOnly session cookie, attached by
  // api-backend.ts, so no per-call header handling is needed here.
  // ===================================================
  // GET TRANSACTION YEARS
  // ===================================================

  getTransactionYears(): Observable<number[]> {

    return this.http.get<number[]>(
      this.transactionYearsUrl
    );
  }


  // ===================================================
  // GET TRANSACTIONS FOR A YEAR (with optional filter/sort)
  // ===================================================

  getTransactions(
    year: number,
    query: TransactionQuery = {}
  ): Observable<Transaction[]> {

    let params = new HttpParams().set('year', year);

    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, value);
      }
    }

    return this.http.get<Transaction[]>(
      this.transactionsUrl,
      { params }
    );
  }


  // ===================================================
  // GET DASHBOARD SUMMARY FOR A YEAR
  // ===================================================

  getDashboard(year: number): Observable<DashboardSummary> {

    return this.http.get<DashboardSummary>(
      this.dashboardUrl,
      { params: new HttpParams().set('year', year) }
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
      transaction
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
  // CURRENT USER (validates the session cookie)
  // ===================================================

  getCurrentUser(): Observable<AuthUser> {

    return this.http.get<AuthUser>(
      this.meUrl
    );
  }


  // ===================================================
  // LOGOUT (.NET expires the session cookie)
  // ===================================================

  logout(): Observable<void> {

    return this.http.post<void>(
      this.logoutUrl,
      null
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
        responseType: 'blob'
      }
    );
  }

}