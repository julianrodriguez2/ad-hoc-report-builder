import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { SummaryDefinition } from '../../../../core/models/summary-definition.model';

@Component({
  selector: 'app-summary-builder',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './summary-builder.component.html',
  styleUrl: './summary-builder.component.scss'
})
export class SummaryBuilderComponent {
  protected readonly summaries: SummaryDefinition[] = [
    {
      fieldName: 'net_revenue',
      operation: 'sum',
      alias: 'Total Revenue'
    },
    {
      fieldName: 'transaction_id',
      operation: 'count',
      alias: 'Transaction Count'
    }
  ];
}
