import { Component } from '@angular/core';
import { ContactFormComponent } from './components/contact-form/contact-form.component';

@Component({
  selector: 'app-root',
  imports: [ContactFormComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
