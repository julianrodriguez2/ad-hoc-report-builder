import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DropdownModule } from 'primeng/dropdown';
import { PanelModule } from 'primeng/panel';
import { SplitterModule } from 'primeng/splitter';

import { Dataset } from '../../../../core/models/dataset.model';
import { Field } from '../../../../core/models/field.model';
import { ReportDefinition } from '../../../../core/models/report-definition.model';
import { DatasetService } from '../../../../core/services/dataset.service';
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

  protected reportDefinition: ReportDefinition = {
    datasetId: null,
    fields: [],
    filters: [],
    grouping: [],
    summaries: [],
    layoutSettings: {}
  };

  constructor(private readonly datasetService: DatasetService) {}

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
      fields: []
    };

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
}
