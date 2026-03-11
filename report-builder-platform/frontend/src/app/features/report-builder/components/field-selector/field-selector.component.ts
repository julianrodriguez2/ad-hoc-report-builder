import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { Field } from '../../../../core/models/field.model';

@Component({
  selector: 'app-field-selector',
  standalone: true,
  imports: [CommonModule, DragDropModule, PanelModule],
  templateUrl: './field-selector.component.html',
  styleUrl: './field-selector.component.scss'
})
export class FieldSelectorComponent {
  protected readonly fields: Field[] = [
    {
      id: 1,
      datasetId: 1,
      fieldName: 'transaction_date',
      displayName: 'Transaction Date',
      dataType: 'date',
      isFilterable: true,
      isGroupable: true,
      isSummarizable: false
    },
    {
      id: 2,
      datasetId: 1,
      fieldName: 'region',
      displayName: 'Region',
      dataType: 'string',
      isFilterable: true,
      isGroupable: true,
      isSummarizable: false
    },
    {
      id: 3,
      datasetId: 1,
      fieldName: 'net_revenue',
      displayName: 'Net Revenue',
      dataType: 'decimal',
      isFilterable: true,
      isGroupable: false,
      isSummarizable: true
    }
  ];

  protected drop(event: CdkDragDrop<Field[]>): void {
    moveItemInArray(this.fields, event.previousIndex, event.currentIndex);
  }
}
