import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-preview-grid',
  standalone: true,
  imports: [CommonModule, PanelModule, TableModule],
  templateUrl: './preview-grid.component.html',
  styleUrl: './preview-grid.component.scss'
})
export class PreviewGridComponent {
  protected readonly columns = ['transaction_date', 'region', 'net_revenue'];

  protected readonly rows = [
    {
      transaction_date: '2025-01-03',
      region: 'North America',
      net_revenue: 43210
    },
    {
      transaction_date: '2025-01-04',
      region: 'Europe',
      net_revenue: 27110
    }
  ];
}
