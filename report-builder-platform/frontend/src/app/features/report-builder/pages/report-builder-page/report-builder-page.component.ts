import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { SplitterModule } from 'primeng/splitter';

import { Dataset } from '../../../../core/models/dataset.model';
import { Field } from '../../../../core/models/field.model';
import { FilterDefinition } from '../../../../core/models/filter-definition.model';
import { GroupDefinition } from '../../../../core/models/group-definition.model';
import { LayoutSettings, createDefaultLayoutSettings, normalizeLayoutSettings } from '../../../../core/models/layout-settings.model';
import { PreviewResult } from '../../../../core/models/preview-result.model';
import { ReportDefinition } from '../../../../core/models/report-definition.model';
import { ReportTemplate } from '../../../../core/models/report-template.model';
import { SavedReport } from '../../../../core/models/saved-report.model';
import { SummaryDefinition } from '../../../../core/models/summary-definition.model';
import { DatasetService } from '../../../../core/services/dataset.service';
import { ReportService } from '../../../../core/services/report.service';
import { SavedReportsService } from '../../../../core/services/saved-reports.service';
import { REPORT_TEMPLATE_IDS, REPORT_TEMPLATES } from '../../../report-layout/data/report-templates';
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
    RouterLink,
    ButtonModule,
    DropdownModule,
    DialogModule,
    InputTextModule,
    InputTextareaModule,
    MessageModule,
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
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

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
  protected exportLoading: 'pdf' | 'excel' | null = null;
  protected exportErrorTitle: string | null = null;
  protected exportErrors: string[] = [];
  protected exportSuccessMessage: string | null = null;

  protected activeSavedReportId: string | null = null;
  protected activeSavedReportName: string | null = null;
  protected activeSavedReportDescription: string | null = null;
  protected isSavedReportLoading = false;
  protected savedReportLoadError: string | null = null;

  protected isSaveDialogVisible = false;
  protected saveDialogName = '';
  protected saveDialogDescription = '';
  protected saveDialogErrors: string[] = [];
  protected saveSubmitting = false;
  protected saveSuccessMessage: string | null = null;
  protected readonly reportTemplates: ReportTemplate[] = REPORT_TEMPLATES;

  protected reportDefinition: ReportDefinition = {
    datasetId: null,
    fields: [],
    filters: [],
    grouping: [],
    summaries: [],
    layoutSettings: createDefaultLayoutSettings()
  };

  private pendingSavedReportId: string | null = null;

  constructor(
    private readonly datasetService: DatasetService,
    private readonly reportService: ReportService,
    private readonly savedReportsService: SavedReportsService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const savedReportId = params.get('savedReportId');
        if (!savedReportId) {
          this.pendingSavedReportId = null;
          return;
        }

        if (savedReportId === this.activeSavedReportId) {
          return;
        }

        if (this.datasets.length === 0) {
          this.pendingSavedReportId = savedReportId;
          return;
        }

        this.loadSavedReport(savedReportId);
      });

    this.loadDatasets();
  }

  protected onDatasetChange(datasetId: string | null): void {
    this.clearSavedReportContext();
    this.saveSuccessMessage = null;
    this.clearExportState();
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
    if (grouping.length > 0) {
      // Backend requires selectedFields to exactly match grouping fields — sync automatically
      const syncedFields = grouping
        .map((g) => this.availableFields.find((f) => f.fieldName === g.fieldName))
        .filter((f): f is Field => f !== undefined);

      this.selectedFields = syncedFields;
      this.reportDefinition = {
        ...this.reportDefinition,
        fields: syncedFields.map((f) => ({ fieldName: f.fieldName, displayName: f.displayName })),
        grouping,
        summaries: this.reportDefinition.summaries
      };
    } else {
      this.selectedFields = [];
      this.reportDefinition = {
        ...this.reportDefinition,
        fields: [],
        grouping: [],
        summaries: []
      };
    }
  }

  protected onSummariesChanged(summaries: SummaryDefinition[]): void {
    this.reportDefinition = {
      ...this.reportDefinition,
      summaries
    };
  }

  protected onTemplateChanged(templateId: string): void {
    this.onLayoutSettingsChanged({
      ...this.reportDefinition.layoutSettings,
      templateId
    });
  }

  protected onLayoutSettingsChanged(layoutSettings: LayoutSettings): void {
    this.reportDefinition = {
      ...this.reportDefinition,
      layoutSettings: this.normalizeDefinitionLayoutSettings(layoutSettings)
    };
  }

  protected runPreview(): void {
    this.saveSuccessMessage = null;
    this.clearExportState();
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

  protected openSaveDialog(): void {
    this.clearExportState();
    this.saveDialogErrors = [];
    this.saveDialogName = this.activeSavedReportName ?? '';
    this.saveDialogDescription = this.activeSavedReportDescription ?? '';
    this.isSaveDialogVisible = true;
  }

  protected exportPdf(): void {
    this.runExport('pdf');
  }

  protected exportExcel(): void {
    this.runExport('excel');
  }

  protected closeSaveDialog(): void {
    if (this.saveSubmitting) {
      return;
    }

    this.isSaveDialogVisible = false;
    this.saveDialogErrors = [];
  }

  protected saveAsNew(): void {
    this.submitSave('create');
  }

  protected updateSavedReport(): void {
    if (!this.activeSavedReportId) {
      this.submitSave('create');
      return;
    }

    this.submitSave('update');
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

          if (this.pendingSavedReportId) {
            const pendingId = this.pendingSavedReportId;
            this.pendingSavedReportId = null;
            this.loadSavedReport(pendingId);
          } else if (datasets.length > 0) {
            const defaultDatasetId = datasets[0].id;
            this.onDatasetChange(defaultDatasetId);
          }
        },
        error: () => {
          this.datasetsLoadError = 'Unable to load datasets.';
        }
      });
  }

  private loadDatasetFields(
    datasetId: string,
    onLoaded?: (fields: Field[]) => void,
    onError?: () => void
  ): void {
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
          onLoaded?.(fields);
        },
        error: () => {
          this.fieldsLoadError = 'Unable to load fields for the selected dataset.';
          onError?.();
        }
      });
  }

  private loadSavedReport(savedReportId: string): void {
    this.isSavedReportLoading = true;
    this.savedReportLoadError = null;
    this.saveSuccessMessage = null;
    this.clearExportState();

    this.savedReportsService
      .getSavedReport(savedReportId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isSavedReportLoading = false;
        })
      )
      .subscribe({
        next: (savedReport) => {
          this.applySavedReport(savedReport);
        },
        error: (error: unknown) => {
          const errorState = this.toPreviewErrorState(error);
          this.savedReportLoadError = errorState.errors.join(' ');
        }
      });
  }

  private applySavedReport(savedReport: SavedReport): void {
    const definition = savedReport.definition;
    if (!definition.datasetId) {
      this.savedReportLoadError = 'Saved report definition is invalid or missing a dataset.';
      return;
    }

    const datasetId = definition.datasetId;
    this.activeSavedReportId = savedReport.id;
    this.activeSavedReportName = savedReport.name;
    this.activeSavedReportDescription = savedReport.description ?? null;

    this.selectedDatasetId = datasetId;
    this.selectedDataset = this.datasets.find((dataset) => dataset.id === datasetId) ?? null;
    this.datasetRuleHints = this.buildDatasetRuleHints(this.selectedDataset);
    this.fieldsLoadError = null;
    this.availableFields = [];
    this.selectedFields = [];
    this.reportDefinition = {
      datasetId,
      fields: [],
      filters: [],
      grouping: [],
      summaries: [],
      layoutSettings: createDefaultLayoutSettings()
    };
    this.resetPreviewState();

    this.loadDatasetFields(
      datasetId,
      (datasetFields) => {
        const mappedSelectedFields = this.mapSelectedFieldsFromDefinition(definition, datasetFields);
        this.selectedFields = mappedSelectedFields;
        this.reportDefinition = {
          datasetId,
          fields: mappedSelectedFields.map((field) => ({
            fieldName: field.fieldName,
            displayName: field.displayName
          })),
          filters: Array.isArray(definition.filters) ? definition.filters : [],
          grouping: Array.isArray(definition.grouping) ? definition.grouping : [],
          summaries: Array.isArray(definition.summaries) ? definition.summaries : [],
          layoutSettings: this.normalizeDefinitionLayoutSettings(definition.layoutSettings)
        };
        this.saveSuccessMessage = `Loaded saved report "${savedReport.name}".`;
      },
      () => {
        this.savedReportLoadError = 'The saved report dataset could not be loaded. It may have been removed.';
      }
    );
  }

  private mapSelectedFieldsFromDefinition(definition: ReportDefinition, datasetFields: Field[]): Field[] {
    const selectedFieldsFromDefinition = Array.isArray(definition.fields) ? definition.fields : [];
    return selectedFieldsFromDefinition
      .map((selectedField) => {
        const metadataField = datasetFields.find((field) => field.fieldName === selectedField.fieldName);
        if (metadataField) {
          return metadataField;
        }

        return {
          id: selectedField.fieldName,
          fieldName: selectedField.fieldName,
          displayName: selectedField.displayName || selectedField.fieldName
        } as Field;
      })
      .filter((field) => !!field.fieldName);
  }

  private submitSave(mode: 'create' | 'update'): void {
    this.clearExportState();
    this.saveDialogErrors = [];

    const reportName = this.saveDialogName.trim();
    const reportDescription = this.saveDialogDescription.trim();
    const datasetId = this.reportDefinition.datasetId;

    if (!datasetId) {
      this.saveDialogErrors = ['Select a dataset before saving the report.'];
      return;
    }

    if (!reportName) {
      this.saveDialogErrors = ['Report name is required.'];
      return;
    }

    this.saveSubmitting = true;

    const createOrUpdate$ =
      mode === 'update' && this.activeSavedReportId
        ? this.savedReportsService.updateSavedReport(this.activeSavedReportId, {
            name: reportName,
            description: reportDescription || null,
            definition: this.reportDefinition
          })
        : this.savedReportsService.createSavedReport({
            name: reportName,
            description: reportDescription || null,
            datasetId,
            definition: this.reportDefinition
          });

    createOrUpdate$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.saveSubmitting = false;
        })
      )
      .subscribe({
        next: (savedReport) => {
          this.activeSavedReportId = savedReport.id;
          this.activeSavedReportName = savedReport.name;
          this.activeSavedReportDescription = savedReport.description ?? null;
          this.saveDialogName = savedReport.name;
          this.saveDialogDescription = savedReport.description ?? '';
          this.isSaveDialogVisible = false;
          this.saveDialogErrors = [];
          this.saveSuccessMessage =
            mode === 'update'
              ? 'Report updated successfully.'
              : 'Report saved successfully.';

          this.pendingSavedReportId = null;
          this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { savedReportId: savedReport.id },
            queryParamsHandling: 'merge'
          });
        },
        error: (error: unknown) => {
          const errorState = this.toPreviewErrorState(error);
          this.saveDialogErrors = errorState.errors;
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

  private runExport(format: 'pdf' | 'excel'): void {
    this.saveSuccessMessage = null;
    this.exportSuccessMessage = null;
    this.exportErrorTitle = null;
    this.exportErrors = [];

    if (!this.reportDefinition.datasetId) {
      this.exportErrorTitle = 'Export could not be run';
      this.exportErrors = ['Select a dataset before exporting.'];
      return;
    }

    if (this.reportDefinition.fields.length === 0) {
      this.exportErrorTitle = 'Export could not be run';
      this.exportErrors = ['Select at least one field before exporting.'];
      return;
    }

    this.exportLoading = format;
    const request$ = format === 'pdf'
      ? this.reportService.exportPdf(this.reportDefinition)
      : this.reportService.exportExcel(this.reportDefinition);

    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.exportLoading = null;
        })
      )
      .subscribe({
        next: (fileBlob) => {
          const extension = format === 'pdf' ? 'pdf' : 'xlsx';
          this.downloadFile(fileBlob, this.buildExportFileName(extension));
          this.exportSuccessMessage = format === 'pdf'
            ? 'PDF exported successfully.'
            : 'Excel exported successfully.';
        },
        error: (error: unknown) => {
          const fallbackTitle = format === 'pdf'
            ? 'PDF export could not be run'
            : 'Excel export could not be run';
          void this.applyExportErrorState(error, fallbackTitle);
        }
      });
  }

  private downloadFile(fileBlob: Blob, filename: string): void {
    const objectUrl = URL.createObjectURL(fileBlob);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(objectUrl);
  }

  private buildExportFileName(extension: 'pdf' | 'xlsx'): string {
    const reportTitle = this.reportDefinition.layoutSettings.reportTitle || 'report';
    const templateId = this.reportDefinition.layoutSettings.templateId || 'template';
    const normalizedTitle = reportTitle
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
    const normalizedTemplate = templateId
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
    const baseName = normalizedTitle || 'report';
    const currentDate = new Date().toISOString().slice(0, 10);
    return `${baseName}-${normalizedTemplate || 'template'}-${currentDate}.${extension}`;
  }

  private async applyExportErrorState(error: unknown, fallbackTitle: string): Promise<void> {
    const fallback = {
      title: fallbackTitle,
      errors: ['Unable to export report due to an unexpected error.']
    };

    if (!(error instanceof HttpErrorResponse)) {
      this.exportErrorTitle = fallback.title;
      this.exportErrors = fallback.errors;
      return;
    }

    const payload = await this.readErrorPayload(error);
    if (payload?.errors && payload.errors.length > 0) {
      this.exportErrorTitle = payload.message ?? fallbackTitle;
      this.exportErrors = payload.errors;
      return;
    }

    if (payload?.message) {
      this.exportErrorTitle = fallbackTitle;
      this.exportErrors = [payload.message];
      return;
    }

    this.exportErrorTitle = fallback.title;
    this.exportErrors = fallback.errors;
  }

  private async readErrorPayload(
    error: HttpErrorResponse
  ): Promise<{ message?: string; errors?: string[] } | null> {
    const responsePayload = error.error;

    if (responsePayload instanceof Blob) {
      try {
        const textPayload = await responsePayload.text();
        if (!textPayload) {
          return null;
        }

        try {
          const parsedPayload = JSON.parse(textPayload) as { message?: string; errors?: string[] };
          return parsedPayload;
        } catch {
          return { message: textPayload };
        }
      } catch {
        return null;
      }
    }

    if (responsePayload && typeof responsePayload === 'object') {
      return responsePayload as { message?: string; errors?: string[] };
    }

    return null;
  }

  private clearExportState(): void {
    this.exportLoading = null;
    this.exportErrorTitle = null;
    this.exportErrors = [];
    this.exportSuccessMessage = null;
  }

  private clearSavedReportContext(): void {
    if (this.activeSavedReportId) {
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { savedReportId: null },
        queryParamsHandling: 'merge'
      });
    }

    this.activeSavedReportId = null;
    this.activeSavedReportName = null;
    this.activeSavedReportDescription = null;
    this.savedReportLoadError = null;
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

  private normalizeDefinitionLayoutSettings(value: unknown): LayoutSettings {
    const normalized = normalizeLayoutSettings(value);
    if (!REPORT_TEMPLATE_IDS.has(normalized.templateId)) {
      return {
        ...normalized,
        templateId: createDefaultLayoutSettings().templateId
      };
    }

    return normalized;
  }
}
