import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { FilterDefinition } from '../../../../core/models/filter-definition.model';

@Component({
  selector: 'app-filter-builder',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './filter-builder.component.html',
  styleUrl: './filter-builder.component.scss'
})
export class FilterBuilderComponent {
  protected readonly filters: FilterDefinition[] = [
    {
      fieldName: 'region',
      operator: 'IN',
      value: ['North America', 'Europe']
    },
    {
      fieldName: 'transaction_date',
      operator: '>=',
      value: '2025-01-01'
    }
  ];
}
