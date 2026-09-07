import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeService } from '../theme.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    RouterLink
  ],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class HomeComponent {

  constructor(
    public themeService: ThemeService
  ) {}

}
