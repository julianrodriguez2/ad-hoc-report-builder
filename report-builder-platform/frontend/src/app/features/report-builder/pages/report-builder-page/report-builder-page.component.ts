import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { PanelModule } from 'primeng/panel';
import { SplitterModule } from 'primeng/splitter';

import { Dataset } from '../../../../core/models/dataset.model';
import { Field } from '../../../../core/models/field.model';
import { FilterDefinition } from '../../../../core/models/filter-definition.model';
import { PreviewResult } from '../../../../core/models/preview-result.model';
import { ReportDefinition } from '../../../../core/models/report-definition.model';
import { DatasetService } from '../../../../core/services/dataset.service';
import { ReportService } from '../../../../core/services/report.service';
import { FieldSelectorComponent } from '../../components/field-selector/field-selector.component';
import { FilterBuilderComponent } from '../../components/filter-builder/filter-builder.component';
import { GroupingBuilderComponent } from '../../components/grouping-builder/grouping-builder.component';
import { SummaryBuilderComponent } from '../../components/summary-builder/summary-builder.component';
import { LayoutEditorComponent } from '../../../report-layout/components/layout-editor/layout-editor.component';
import { TemplateSelectorComponent } from '../../../report-layout/components/template-selector/template-selector.component';
import { PreviewGridComponent } from '../../../report-preview/components/preview-grid/preview-grid.component';

@Component({
  selector: 'app-report-builder-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    DropdownModule,
    PanelModule,
    SplitterModule,
    FieldSelectorComponent,
    FilterBuilderComponent,
    GroupingBuilderComponent,
    SummaryBuilderComponent,
    PreviewGridComponent,
    LayoutEditorComponent,
    TemplateSelectorComponent
  ],
  templateUrl: './report-builder-page.component.html',
  styleUrl: './report-builder-page.component.scss'
})
export class ReportBuilderPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  protected datasets: Dataset[] = [];
  protected availableFields: Field[] = [];
  protected selectedFields: Field[] = [];
  protected selectedDatasetId: string | null = null;

  protected isDatasetsLoading = false;
  protected isFieldsLoading = false;
  protected datasetsLoadError: string | null = null;
  protected fieldsLoadError: string | null = null;
  protected previewLoading = false;
  protected previewError: string | null = null;
  protected previewResult: PreviewResult | null = null;
  protected hasPreviewRun = false;

  protected reportDefinition: ReportDefinition = {
    datasetId: null,
    fields: [],
    filters: [],
    grouping: [],
    summaries: [],
    layoutSettings: {}
  };

  constructor(
    private readonly datasetService: DatasetService,
    private readonly reportService: ReportService
  ) {}

  ngOnInit(): void {
    this.loadDatasets();
  }

  protected onDatasetChange(datasetId: string | null): void {
    this.selectedDatasetId = datasetId;
    this.fieldsLoadError = null;
    this.availableFields = [];
    this.selectedFields = [];
    this.reportDefinition = {
      ...this.reportDefinition,
      datasetId,
      fields: [],
      filters: []
    };
    this.resetPreviewState();

    if (!datasetId) {
      return;
    }

    this.loadDatasetFields(datasetId);
  }

  protected onSelectedFieldsChanged(selectedFields: Field[]): void {
    this.selectedFields = selectedFields;

    this.reportDefinition = {
      ...this.reportDefinition,
      fields: selectedFields.map((field) => ({
        fieldName: field.fieldName,
        displayName: field.displayName
      }))
    };
  }

  protected onFiltersChanged(filters: FilterDefinition[]): void {
    this.reportDefinition = {
      ...this.reportDefinition,
      filters
    };
  }

  protected runPreview(): void {
    this.hasPreviewRun = true;
    this.previewError = null;

    if (!this.reportDefinition.datasetId) {
      this.previewError = 'Select a dataset before running preview.';
      this.previewResult = null;
      return;
    }

    if (this.reportDefinition.fields.length === 0) {
      this.previewError = 'Select at least one field before running preview.';
      this.previewResult = null;
      return;
    }

    this.previewLoading = true;

    this.reportService
      .previewReport(this.reportDefinition)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.previewLoading = false;
        })
      )
      .subscribe({
        next: (result) => {
          this.previewResult = result;
          this.previewError = null;
        },
        error: (error: unknown) => {
          this.previewResult = null;
          this.previewError = this.toPreviewErrorMessage(error);
        }
      });
  }

  private loadDatasets(): void {
    this.isDatasetsLoading = true;
    this.datasetsLoadError = null;

    this.datasetService
      .getDatasets()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isDatasetsLoading = false;
        })
      )
      .subscribe({
        next: (datasets) => {
          this.datasets = datasets;

          if (datasets.length > 0) {
            const defaultDatasetId = datasets[0].id;
            this.onDatasetChange(defaultDatasetId);
          }
        },
        error: () => {
          this.datasetsLoadError = 'Unable to load datasets.';
        }
      });
  }

  private loadDatasetFields(datasetId: string): void {
    this.isFieldsLoading = true;

    this.datasetService
      .getDatasetFields(datasetId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isFieldsLoading = false;
        })
      )
      .subscribe({
        next: (fields) => {
          this.availableFields = fields;
        },
        error: () => {
          this.fieldsLoadError = 'Unable to load fields for the selected dataset.';
        }
      });
  }

  private resetPreviewState(): void {
    this.previewLoading = false;
    this.previewError = null;
    this.previewResult = null;
    this.hasPreviewRun = false;
  }

  private toPreviewErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Unable to run preview due to an unexpected error.';
    }

    const payload = error.error as {
      message?: string;
      errors?: string[];
    } | null;

    if (payload?.errors && payload.errors.length > 0) {
      return payload.errors.join(' ');
    }

    if (payload?.message) {
      return payload.message;
    }

    return 'Unable to run preview due to an unexpected error.';
  }
}
