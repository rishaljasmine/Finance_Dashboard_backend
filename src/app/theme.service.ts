import { Injectable, effect, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {

  isLight = signal(this.readStoredTheme());

  constructor() {

    effect(() => {

      document.documentElement.classList.toggle(
        'light',
        this.isLight()
      );

      localStorage.setItem(
        'theme',
        this.isLight() ? 'light' : 'dark'
      );
    });
  }


  toggle(): void {
    this.isLight.set(!this.isLight());
  }


  private readStoredTheme(): boolean {
    return localStorage.getItem('theme') === 'light';
  }

}
