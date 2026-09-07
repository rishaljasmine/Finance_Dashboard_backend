import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {

  // Injected here purely so the theme class is applied to <html>
  // the moment the app boots, before any component reads it.
  constructor(private themeService: ThemeService) {}

}