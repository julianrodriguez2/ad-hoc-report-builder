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
import { GroupDefinition } from '../../../../core/models/group-definition.model';
import { PreviewResult } from '../../../../core/models/preview-result.model';
import { ReportDefinition } from '../../../../core/models/report-definition.model';
import { SummaryDefinition } from '../../../../core/models/summary-definition.model';
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
  protected selectedDataset: Dataset | null = null;
  protected datasetRuleHints: string[] = [];
  protected availableFields: Field[] = [];
  protected selectedFields: Field[] = [];
  protected selectedDatasetId: string | null = null;

  protected isDatasetsLoading = false;
  protected isFieldsLoading = false;
  protected datasetsLoadError: string | null = null;
  protected fieldsLoadError: string | null = null;
  protected previewLoading = false;
  protected previewErrorTitle: string | null = null;
  protected previewErrors: string[] = [];
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
    this.selectedDataset = this.datasets.find((dataset) => dataset.id === datasetId) ?? null;
    this.datasetRuleHints = this.buildDatasetRuleHints(this.selectedDataset);
    this.fieldsLoadError = null;
    this.availableFields = [];
    this.selectedFields = [];
    this.reportDefinition = {
      ...this.reportDefinition,
      datasetId,
      fields: [],
      filters: [],
      grouping: [],
      summaries: []
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

  protected onGroupingChanged(grouping: GroupDefinition[]): void {
    const shouldClearSummaries = grouping.length === 0;

    this.reportDefinition = {
      ...this.reportDefinition,
      grouping,
      summaries: shouldClearSummaries ? [] : this.reportDefinition.summaries
    };
  }

  protected onSummariesChanged(summaries: SummaryDefinition[]): void {
    this.reportDefinition = {
      ...this.reportDefinition,
      summaries
    };
  }

  protected runPreview(): void {
    this.hasPreviewRun = true;
    this.previewErrorTitle = null;
    this.previewErrors = [];

    if (!this.reportDefinition.datasetId) {
      this.previewErrorTitle = 'Preview could not be run';
      this.previewErrors = ['Select a dataset before running preview.'];
      this.previewResult = null;
      return;
    }

    if (this.reportDefinition.fields.length === 0) {
      this.previewErrorTitle = 'Preview could not be run';
      this.previewErrors = ['Select at least one field before running preview.'];
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
          this.previewErrorTitle = null;
          this.previewErrors = [];
        },
        error: (error: unknown) => {
          this.previewResult = null;
          const previewErrorState = this.toPreviewErrorState(error);
          this.previewErrorTitle = previewErrorState.title;
          this.previewErrors = previewErrorState.errors;
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
    this.previewErrorTitle = null;
    this.previewErrors = [];
    this.previewResult = null;
    this.hasPreviewRun = false;
  }

  private toPreviewErrorState(error: unknown): { title: string; errors: string[] } {
    if (!(error instanceof HttpErrorResponse)) {
      return {
        title: 'Preview could not be run',
        errors: ['Unable to run preview due to an unexpected error.']
      };
    }

    const payload = error.error as {
      message?: string;
      errors?: string[];
    } | null;

    if (payload?.errors && payload.errors.length > 0) {
      return {
        title: payload.message ?? 'Preview could not be run',
        errors: payload.errors
      };
    }

    if (payload?.message) {
      return {
        title: 'Preview could not be run',
        errors: [payload.message]
      };
    }

    return {
      title: 'Preview could not be run',
      errors: ['Unable to run preview due to an unexpected error.']
    };
  }

  private buildDatasetRuleHints(dataset: Dataset | null): string[] {
    if (!dataset) {
      return [];
    }

    const hints: string[] = [];
    if (dataset.requireAtLeastOneFilter) {
      hints.push('At least one filter is required for this dataset.');
    }

    if (dataset.requireDateFilter) {
      hints.push('A date filter is required for this dataset.');
    }

    if (dataset.previewRowLimit) {
      hints.push(`Preview limited to ${dataset.previewRowLimit} rows.`);
    }

    if (dataset.timeoutSeconds) {
      hints.push(`Preview timeout is ${dataset.timeoutSeconds} seconds.`);
    }

    return hints;
  }
}
