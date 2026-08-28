import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard';
import { FinanceService, Transaction } from '../finance.service';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

describe('DashboardComponent', () => {

  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;

  const transactions: Transaction[] = [
    {
      id: 1,
      type: 'income',
      category: 'Salary',
      amount: 50000,
      date: '2026-01-10'
    },
    {
      id: 2,
      type: 'expense',
      category: 'Food',
      amount: 5000,
      date: '2026-01-15'
    }
  ];

  beforeEach(async () => {

    const financeService = {
      getTransactionYears: () => of([2026]),

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

    localStorage.setItem('loggedIn', 'true');
    localStorage.setItem('username', 'rishal');
    localStorage.setItem('email', 'test@gmail.com');

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