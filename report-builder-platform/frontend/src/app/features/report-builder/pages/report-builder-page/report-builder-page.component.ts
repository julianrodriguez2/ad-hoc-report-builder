import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { SplitterModule } from 'primeng/splitter';

import { DatasetSelectorComponent } from '../../components/dataset-selector/dataset-selector.component';
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
    SplitterModule,
    DatasetSelectorComponent,
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
export class ReportBuilderPageComponent {}
