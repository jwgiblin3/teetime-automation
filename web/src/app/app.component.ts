import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './layout/navbar/navbar.component';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, NavbarComponent],
  template: `
    <app-navbar></app-navbar>
    <main>
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    main {
      min-height: calc(100vh - 64px);
    }
  `]
})
export class AppComponent implements OnInit {

  ngOnInit() {
    // This will print the status as soon as the app loads
    console.log('--- Environment Check ---');
    console.log('Is Production:', environment.production);
    console.log('API URL:', environment.apiUrl);
    console.log('-------------------------');
  }
}
