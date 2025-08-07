import { Component, OnInit } from '@angular/core';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'hello-world';
  appName = environment.appName;
  appVersion = environment.appVersion;
  isProduction = environment.production;
  sslEnabled = environment.sslEnabled;
  
  ngOnInit() {
    console.log('Environment:', {
      appName: this.appName,
      appVersion: this.appVersion,
      production: this.isProduction,
      sslEnabled: this.sslEnabled
    });
  }
}
