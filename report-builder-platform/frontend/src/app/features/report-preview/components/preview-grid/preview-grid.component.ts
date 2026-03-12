import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';

import { PreviewColumn } from '../../../../core/models/preview-result.model';

@Component({
  selector: 'app-preview-grid',
  standalone: true,
  imports: [CommonModule, PanelModule, TableModule, ProgressSpinnerModule, MessageModule],
  templateUrl: './preview-grid.component.html',
  styleUrl: './preview-grid.component.scss'
})
export class PreviewGridComponent {
  @Input() columns: PreviewColumn[] = [];

  @Input() rows: Record<string, unknown>[] = [];

  @Input() loading = false;

  @Input() errorTitle: string | null = null;

  @Input() errorMessages: string[] = [];

  @Input() isTruncated = false;

  @Input() appliedRowLimit: number | null = null;

  @Input() executionTimeMs: number | null = null;

  @Input() hasAttemptedPreview = false;

  @Input() debugSql: string | null = null;

  protected trackByColumn(_: number, column: PreviewColumn): string {
    return column.fieldName;
  }

  protected readCellValue(row: Record<string, unknown>, fieldName: string): unknown {
    return row[fieldName] ?? '';
  }

  protected hasErrors(): boolean {
    return this.errorMessages.length > 0;
  }
}
