import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DropdownModule } from 'primeng/dropdown';
import { PanelModule } from 'primeng/panel';

import { Dataset } from '../../../../core/models/dataset.model';

@Component({
  selector: 'app-dataset-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule, PanelModule],
  templateUrl: './dataset-selector.component.html',
  styleUrl: './dataset-selector.component.scss'
})
export class DatasetSelectorComponent {
  protected readonly datasets: Dataset[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Sales Dataset',
      description: 'Transactions and revenue metrics',
      viewName: 'vw_sales_summary'
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Customers Dataset',
      description: 'Customer profile and lifecycle data',
      viewName: 'vw_customers'
    },
    {
      id: '33333333-3333-3333-3333-333333333333',
      name: 'Subscriptions Dataset',
      description: 'Plan, renewal, and churn metrics',
      viewName: 'vw_subscriptions'
    }
  ];

  protected selectedDatasetId: string | null = this.datasets[0]?.id ?? null;
}
