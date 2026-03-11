import { Routes } from '@angular/router';
import { ReportBuilderPageComponent } from './features/report-builder/pages/report-builder-page/report-builder-page.component';
import { SavedReportsPageComponent } from './features/saved-reports/pages/saved-reports-page/saved-reports-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'report-builder'
  },
  {
    path: 'report-builder',
    component: ReportBuilderPageComponent
  },
  {
    path: 'saved-reports',
    component: SavedReportsPageComponent
  },
  {
    path: '**',
    redirectTo: 'report-builder'
  }
];
