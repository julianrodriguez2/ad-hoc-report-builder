import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { GroupDefinition } from '../../../../core/models/group-definition.model';

@Component({
  selector: 'app-grouping-builder',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './grouping-builder.component.html',
  styleUrl: './grouping-builder.component.scss'
})
export class GroupingBuilderComponent {
  protected readonly groups: GroupDefinition[] = [
    {
      fieldName: 'transaction_date',
      direction: 'asc'
    },
    {
      fieldName: 'region',
      direction: 'asc'
    }
  ];
}
