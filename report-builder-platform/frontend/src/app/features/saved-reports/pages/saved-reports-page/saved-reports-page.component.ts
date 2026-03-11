import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-saved-reports-page',
  standalone: true,
  imports: [CommonModule, CardModule, TableModule],
  templateUrl: './saved-reports-page.component.html',
  styleUrl: './saved-reports-page.component.scss'
})
export class SavedReportsPageComponent {
  protected readonly reports = [
    {
      id: 1,
      name: 'Monthly Revenue by Region',
      createdBy: 'system@demo.local',
      createdAt: '2026-01-15'
    },
    {
      id: 2,
      name: 'Customer Churn Overview',
      createdBy: 'analyst@demo.local',
      createdAt: '2026-02-02'
    }
  ];
}
