import { Injectable, effect, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {

  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  isLight = signal(this.readStoredTheme());

  constructor() {

    effect(() => {

      if (!this.isBrowser) {
        return;
      }

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
    return this.isBrowser && localStorage.getItem('theme') === 'light';
  }

}
