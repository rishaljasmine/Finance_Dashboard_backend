
import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { BaseChartDirective } from 'ng2-charts';
import {
  ChartConfiguration,
  ChartType
} from 'chart.js';

import {
  FinanceService,
  Transaction
} from '../finance.service';

type DashboardChartType =
  | 'bar'
  | 'line'
  | 'pie'
  | '3dline';

const CATEGORY_COLORS = [
  '#6366f1',
  '#ef4444',
  '#f59e0b',
  '#22c55e',
  '#06b6d4',
  '#ec4899',
  '#a855f7',
  '#84cc16',
  '#f97316',
  '#14b8a6'
];

@Component({
  selector: 'app-dashboard',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    BaseChartDirective
  ],

  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {

  // =====================================================
  // BASIC VARIABLES
  // =====================================================

  Math = Math;

  username = '';
  userEmail = '';

  // =====================================================
  // SIDEBAR
  // =====================================================

  sidebarCollapsed = false;
  mobileSidebarOpen = false;


  // =====================================================
  // PROFILE
  // =====================================================

  showProfile = false;


  // =====================================================
  // YEAR / CHART
  // =====================================================

  currentCalendarYear = new Date().getFullYear();

  selectedYear = this.currentCalendarYear;

  selectedChart: DashboardChartType = 'bar';

  // Populated from the years that actually have transactions for
  // this user (falls back to the current year if there are none yet).
  years = signal<number[]>([this.currentCalendarYear]);


  // =====================================================
  // MONTHS
  // =====================================================

  months: string[] = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec'
  ];


  // =====================================================
  // CHART SWITCHER
  // =====================================================

  chartTypes: {
    type: DashboardChartType;
    name: string;
    icon: string;
  }[] = [

    {
      type: 'bar',
      name: 'Bar Chart',
      icon: '▥'
    },

    {
      type: 'line',
      name: 'Line Chart',
      icon: '⌁'
    },

    {
      type: 'pie',
      name: 'Pie Chart',
      icon: '◉'
    },

    {
      type: '3dline',
      name: '3D Line',
      icon: '〽'
    }

  ];


  // =====================================================
  // TRANSACTIONS (from the database)
  // =====================================================
  //
  // Mutated from inside HttpClient .subscribe() callbacks —
  // this app is zoneless (no zone.js), so plain fields set
  // asynchronously never reach the view. Signals do.

  transactions = signal<Transaction[]>([]);

  loadingTransactions = signal(false);

  loadError = signal('');


  // =====================================================
  // ADD TRANSACTION FORM
  // =====================================================

  showAddForm = signal(false);
  addSubmitting = signal(false);
  addError = signal('');

  newType: 'income' | 'expense' = 'income';
  newCategory = '';
  newAmount: number | null = null;
  newDate = this.toIsoDate(new Date());


  // =====================================================
  // CONSTRUCTOR
  // =====================================================

  constructor(
    private router: Router,
    private financeService: FinanceService
  ) {}


  // =====================================================
  // INITIALIZATION
  // =====================================================

  ngOnInit(): void {

    const loggedIn =
      localStorage.getItem('loggedIn');

    if (loggedIn !== 'true') {

      this.router.navigate(['/login']);

      return;
    }

    this.username =
      localStorage.getItem('username') || 'User';

    this.userEmail =
      localStorage.getItem('email') ||
      localStorage.getItem('gmail') ||
      '';

    this.loadYears();
  }


  // =====================================================
  // LOAD THE YEARS THAT HAVE DATA
  // =====================================================

  loadYears(): void {

    this.financeService.getTransactionYears().subscribe({

      next: (years) => {

        if (years.length > 0) {

          this.years.set(years);
          this.selectedYear = years[0];

        } else {

          this.years.set([this.currentCalendarYear]);
          this.selectedYear = this.currentCalendarYear;
        }

        this.loadTransactions();
      },

      error: (error) => {

        if (error.status === 401) {
          this.logout();
          return;
        }

        // Fall back to the current year so the dashboard still loads.
        this.years.set([this.currentCalendarYear]);
        this.selectedYear = this.currentCalendarYear;
        this.loadTransactions();
      }

    });
  }


  // =====================================================
  // REFRESH THE YEARS LIST WITHOUT CHANGING selectedYear
  // =====================================================

  private refreshYearsList(): void {

    this.financeService.getTransactionYears().subscribe({

      next: (years) => {

        this.years.set(
          years.length > 0
            ? years
            : [this.currentCalendarYear]
        );
      }

    });
  }


  // =====================================================
  // LOAD TRANSACTIONS FOR THE SELECTED YEAR
  // =====================================================

  loadTransactions(): void {

    this.loadingTransactions.set(true);
    this.loadError.set('');

    this.financeService.getTransactions(
      this.selectedYear
    ).subscribe({

      next: (transactions) => {

        this.transactions.set(transactions);
        this.loadingTransactions.set(false);
      },

      error: (error) => {

        this.loadingTransactions.set(false);

        if (error.status === 401) {
          this.logout();
          return;
        }

        this.loadError.set(
          'Could not load your transactions.'
        );
      }

    });
  }


  // =====================================================
  // SIDEBAR TOGGLE
  // =====================================================

  toggleSidebar(): void {

    if (window.matchMedia('(max-width: 900px)').matches) {
      this.mobileSidebarOpen = !this.mobileSidebarOpen;
      return;
    }

    this.sidebarCollapsed = !this.sidebarCollapsed;

  }


  // =====================================================
  // CLOSE SIDEBAR
  // =====================================================

  closeSidebar(): void {

    this.sidebarCollapsed = true;

  }


  closeMobileSidebar(): void {

    this.mobileSidebarOpen = false;

  }


  openDashboard(event: Event): void {

    event.preventDefault();

    if (window.matchMedia('(max-width: 900px)').matches) {
      this.mobileSidebarOpen = !this.mobileSidebarOpen;
    } else {
      this.sidebarCollapsed = !this.sidebarCollapsed;
    }

    this.router.navigate(['/dashboard']);

  }


  // =====================================================
  // DISPLAY NAME
  // =====================================================

  get displayName(): string {

    return this.username || 'User';

  }


  // =====================================================
  // PROFILE INITIAL
  // =====================================================

  get profileInitial(): string {

    const name =
      this.username ||
      this.userEmail ||
      'U';

    return name
      .charAt(0)
      .toUpperCase();

  }


  // =====================================================
  // PROFILE TOGGLE
  // =====================================================

  toggleProfile(): void {

    this.showProfile =
      !this.showProfile;

  }


  // =====================================================
  // EXPENSE BY CATEGORY (derived from real transactions)
  // =====================================================

  private get expenseByCategory(): Record<string, number> {

    const totals: Record<string, number> = {};

    for (const transaction of this.transactions()) {

      if (transaction.type !== 'expense') {
        continue;
      }

      totals[transaction.category] =
        (totals[transaction.category] || 0) +
        transaction.amount;
    }

    return totals;
  }


  get currentExpenseCategories(): string[] {

    return Object.keys(this.expenseByCategory);

  }


  get currentExpenseValues(): number[] {

    return Object.values(this.expenseByCategory);

  }


  // =====================================================
  // TOTAL INCOME / EXPENSES / SAVINGS
  // =====================================================

  get currentTotalIncome(): number {

    return this.transactions()
      .filter(transaction => transaction.type === 'income')
      .reduce((total, transaction) => total + transaction.amount, 0);

  }


  get currentTotalExpenses(): number {

    return this.transactions()
      .filter(transaction => transaction.type === 'expense')
      .reduce((total, transaction) => total + transaction.amount, 0);

  }


  get currentNetSavings(): number {

    return (
      this.currentTotalIncome -
      this.currentTotalExpenses
    );

  }


  // =====================================================
  // RECENT TRANSACTIONS (display shape)
  // =====================================================

  get currentTransactions(): {
    id: number;
    name: string;
    date: string;
    type: 'Income' | 'Expense';
    amount: number;
  }[] {

    return this.transactions().map(transaction => ({

      id: transaction.id,

      name: transaction.category,

      date: this.formatDisplayDate(transaction.date),

      type: transaction.type === 'income'
        ? 'Income'
        : 'Expense',

      amount: transaction.type === 'income'
        ? transaction.amount
        : -transaction.amount

    }));

  }


  private formatDisplayDate(isoDate: string): string {

    return new Date(`${isoDate}T00:00:00`)
      .toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });

  }


  private toIsoDate(date: Date): string {

    return date.toISOString().slice(0, 10);

  }


  // =====================================================
  // MONTHLY INCOME / EXPENSE SERIES
  // =====================================================

  private get monthlySeries(): {
    income: number[];
    expenses: number[];
  } {

    const income = new Array(12).fill(0);
    const expenses = new Array(12).fill(0);

    for (const transaction of this.transactions()) {

      const monthIndex =
        new Date(`${transaction.date}T00:00:00`).getMonth();

      if (transaction.type === 'income') {
        income[monthIndex] += transaction.amount;
      } else {
        expenses[monthIndex] += transaction.amount;
      }
    }

    return { income, expenses };
  }


  // =====================================================
  // CHART.JS TYPE
  // =====================================================

  get chartJsType(): ChartType {

    if (this.selectedChart === '3dline') {
      return 'line';
    }

    return this.selectedChart;
  }


  // =====================================================
  // SELECT CHART
  // =====================================================

  selectChart(
    type: DashboardChartType
  ): void {

    this.selectedChart =
      type;

  }


  // =====================================================
  // CHANGE YEAR
  // =====================================================

  changeYear(event: Event): void {

    const select =
      event.target as HTMLSelectElement;

    this.selectedYear =
      Number(select.value);

    this.loadTransactions();

  }


  // =====================================================
  // ADD TRANSACTION FORM
  // =====================================================

  openAddForm(): void {

    this.addError.set('');

    this.newType = 'income';
    this.newCategory = '';
    this.newAmount = null;
    this.newDate = this.toIsoDate(new Date());

    this.showAddForm.set(true);
  }


  closeAddForm(): void {

    this.showAddForm.set(false);

  }


  submitAddForm(): void {

    if (this.addSubmitting()) {
      return;
    }

    this.addError.set('');

    if (!this.newCategory.trim()) {
      this.addError.set(
        'Please enter a category or description.'
      );
      return;
    }

    if (!this.newAmount || this.newAmount <= 0) {
      this.addError.set(
        'Please enter an amount greater than 0.'
      );
      return;
    }

    if (!this.newDate) {
      this.addError.set(
        'Please pick a date.'
      );
      return;
    }

    this.addSubmitting.set(true);

    this.financeService.createTransaction({
      type: this.newType,
      category: this.newCategory.trim(),
      amount: this.newAmount,
      date: this.newDate
    }).subscribe({

      next: () => {

        this.addSubmitting.set(false);
        this.showAddForm.set(false);

        const addedYear =
          new Date(`${this.newDate}T00:00:00`).getFullYear();

        this.refreshYearsList();

        if (addedYear === this.selectedYear) {
          this.loadTransactions();
        }
      },

      error: (error) => {

        this.addSubmitting.set(false);

        if (error.status === 401) {
          this.logout();
          return;
        }

        this.addError.set(
          'Could not save the transaction.'
        );
      }

    });
  }


  // =====================================================
  // CHART DATA
  // =====================================================

  get chartData(): ChartConfiguration['data'] {

    if (this.selectedChart === 'pie') {

      return {

        labels: [
          'Total Income',
          'Total Expenses'
        ],

        datasets: [

          {
            label: 'Financial Overview',

            data: [
              this.currentTotalIncome,
              this.currentTotalExpenses
            ],

            backgroundColor: [
              '#22c55e',
              '#ef4444'
            ],

            borderColor: '#0d1420',

            borderWidth: 3,

            hoverOffset: 10
          }

        ]
      };

    }


    const { income, expenses } = this.monthlySeries;

    const is3D =
      this.selectedChart === '3dline';

    const labels =
      this.months.map(
        month =>
          month + ' ' + this.selectedYear
      );


    // ===================================================
    // BAR / LINE
    // ===================================================

    if (!is3D) {

      return {

        labels,

        datasets: [

          {

            label: 'Income',

            data: income,

            backgroundColor:
              this.selectedChart === 'bar'
                ? 'rgba(99, 102, 241, 0.72)'
                : 'rgba(99, 102, 241, 0.10)',

            borderColor: '#6366f1',

            borderWidth: 2,

            borderRadius:
              this.selectedChart === 'bar'
                ? 3
                : 0,

            pointBackgroundColor: '#6366f1',

            pointBorderColor: '#6366f1',

            pointRadius:
              this.selectedChart === 'line'
                ? 4
                : 0,

            pointHoverRadius: 6,

            tension: 0.35,

            fill:
              this.selectedChart === 'line'

          },

          {

            label: 'Expenses',

            data: expenses,

            backgroundColor:
              this.selectedChart === 'bar'
                ? 'rgba(239, 68, 68, 0.78)'
                : 'rgba(239, 68, 68, 0.08)',

            borderColor: '#ef4444',

            borderWidth: 2,

            borderRadius:
              this.selectedChart === 'bar'
                ? 3
                : 0,

            pointBackgroundColor: '#ef4444',

            pointBorderColor: '#ef4444',

            pointRadius:
              this.selectedChart === 'line'
                ? 4
                : 0,

            pointHoverRadius: 6,

            tension: 0.35,

            fill:
              this.selectedChart === 'line'

          }

        ]

      };

    }


    // ===================================================
    // 3D STYLE LINE
    // ===================================================

    return {

      labels,

      datasets: [

        {

          label: 'Income Shadow',

          data: income.map(
            value =>
              Math.max(0, value - 3500)
          ),

          borderColor:
            'rgba(67, 56, 202, 0.45)',

          backgroundColor:
            'rgba(99, 102, 241, 0.05)',

          borderWidth: 6,

          pointRadius: 0,

          tension: 0.4,

          fill: false

        },

        {

          label: 'Income',

          data: income,

          borderColor: '#818cf8',

          backgroundColor:
            'rgba(99, 102, 241, 0.16)',

          borderWidth: 4,

          pointBackgroundColor: '#818cf8',

          pointBorderColor: '#ffffff',

          pointBorderWidth: 2,

          pointRadius: 5,

          pointHoverRadius: 8,

          tension: 0.4,

          fill: true

        },

        {

          label: 'Expenses Shadow',

          data: expenses.map(
            value =>
              Math.max(0, value - 3500)
          ),

          borderColor:
            'rgba(185, 28, 28, 0.45)',

          backgroundColor:
            'rgba(239, 68, 68, 0.05)',

          borderWidth: 6,

          pointRadius: 0,

          tension: 0.4,

          fill: false

        },

        {

          label: 'Expenses',

          data: expenses,

          borderColor: '#f87171',

          backgroundColor:
            'rgba(239, 68, 68, 0.14)',

          borderWidth: 4,

          pointBackgroundColor: '#f87171',

          pointBorderColor: '#ffffff',

          pointBorderWidth: 2,

          pointRadius: 5,

          pointHoverRadius: 8,

          tension: 0.4,

          fill: true

        }

      ]

    };

  }


  // =====================================================
  // CHART OPTIONS
  // =====================================================

  get chartOptions(): ChartConfiguration['options'] {

    if (this.selectedChart === 'pie') {

      return {

        responsive: true,

        maintainAspectRatio: false,

        plugins: {

          legend: {

            position: 'right',

            labels: {

              color: '#a7b0c0',

              usePointStyle: true,

              pointStyle: 'circle',

              padding: 18,

              font: {
                size: 13
              }

            }

          },

          tooltip: {

            backgroundColor: '#111827',

            titleColor: '#ffffff',

            bodyColor: '#cbd5e1',

            borderColor: '#293548',

            borderWidth: 1,

            padding: 12,

            callbacks: {

              label: (context: any) => {

                const value =
                  Number(context.raw || 0);

                return (
                  ' ' +
                  context.label +
                  ': ₹' +
                  value.toLocaleString('en-IN')
                );

              }

            }

          }

        }

      };

    }


    const is3D =
      this.selectedChart === '3dline';

    return {

      responsive: true,

      maintainAspectRatio: false,

      interaction: {

        mode: 'index',

        intersect: false

      },

      plugins: {

        legend: {

          position: 'top',

          labels: {

            color: '#a7b0c0',

            usePointStyle: true,

            pointStyle: 'circle',

            padding: 20,

            font: {
              size: 13
            }

          }

        },

        tooltip: {

          backgroundColor: '#111827',

          titleColor: '#ffffff',

          bodyColor: '#cbd5e1',

          borderColor: '#293548',

          borderWidth: 1,

          padding: 12,

          callbacks: {

            label: (context: any) => {

              if (
                is3D &&
                (
                  context.datasetIndex === 0 ||
                  context.datasetIndex === 2
                )
              ) {
                return '';
              }

              const value =
                Number(context.raw || 0);

              return (
                ' ' +
                context.dataset.label +
                ': ₹' +
                value.toLocaleString('en-IN')
              );

            }

          }

        }

      },

      scales: {

        x: {

          ticks: {

            color: '#7c8799',

            maxRotation: 0,

            minRotation: 0,

            font: {
              size: 10
            }

          },

          grid: {

            color:
              'rgba(255,255,255,0.055)'

          }

        },

        y: {

          beginAtZero: true,

          ticks: {

            color: '#7c8799',

            font: {
              size: 10
            },

            callback: (value: any) => {

              const number =
                Number(value);

              return (
                '₹' +
                (number / 1000) +
                'K'
              );

            }

          },

          grid: {

            color:
              'rgba(255,255,255,0.055)'

          }

        }

      }

    };

  }


  // =====================================================
  // CATEGORY COLOR (for the expense legend)
  // =====================================================

  categoryColor(index: number): string {

    return CATEGORY_COLORS[index % CATEGORY_COLORS.length];

  }


  // =====================================================
  // LOGOUT
  // =====================================================

  logout(): void {

    this.showProfile = false;
    this.sidebarCollapsed = false;
    this.mobileSidebarOpen = false;

    localStorage.removeItem('loggedIn');
    localStorage.removeItem('username');
    localStorage.removeItem('email');
    localStorage.removeItem('gmail');
    localStorage.removeItem('token');

    this.router.navigate(['/login']);

  }

}
