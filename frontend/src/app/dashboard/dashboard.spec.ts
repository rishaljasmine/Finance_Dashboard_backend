import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard';
import { FinanceService, Transaction, DashboardSummary } from '../finance.service';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../auth.service';

describe('DashboardComponent', () => {

  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;

  const transactions: Transaction[] = [
    {
      id: 1,
      type: 'income',
      category: 'Salary',
      amount: 50000,
      date: '2026-01-10',
      files: []
    },
    {
      id: 2,
      type: 'expense',
      category: 'Food',
      amount: 5000,
      date: '2026-01-15',
      files: []
    }
  ];

  const dashboardSummary: DashboardSummary = {
    totalIncome: 50000,
    totalExpense: 5000,
    balance: 45000,
    monthlySummary: [
      { month: 1, income: 50000, expense: 5000 }
    ],
    expensesByCategory: [
      { category: 'Food', amount: 5000 }
    ]
  };

  beforeEach(async () => {

    const financeService = {
      getTransactionYears: () => of([2026]),

      getDashboard: () => of(dashboardSummary),

      getTransactions: () => of(transactions)
    };

    await TestBed.configureTestingModule({
      imports: [DashboardComponent],

      providers: [
        provideRouter([]),

        {
          provide: FinanceService,
          useValue: financeService
        }
      ]
    }).compileComponents();

    // The session is a cookie validated by authGuard; the component just
    // reads the user the guard put on AuthService.
    TestBed.inject(AuthService).signedIn({
      id: 1,
      username: 'rishal',
      email: 'test@gmail.com'
    });

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;

    fixture.detectChanges();
  });


  // TEST CASE 1
  it('should create dashboard component', () => {
    expect(component).toBeTruthy();
  });


  // TEST CASE 2
  it('should calculate total income correctly', () => {
    expect(component.currentTotalIncome).toBe(50000);
  });

});
