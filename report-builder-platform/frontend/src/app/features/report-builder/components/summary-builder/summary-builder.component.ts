import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { PanelModule } from 'primeng/panel';

import { Field } from '../../../../core/models/field.model';
import { GroupDefinition } from '../../../../core/models/group-definition.model';
import { SummaryDefinition } from '../../../../core/models/summary-definition.model';

type NormalizedDataType = 'string' | 'number' | 'date' | 'boolean';

interface DropdownOption<T> {
  label: string;
  value: T;
}

const AGGREGATION_OPTIONS_BY_DATA_TYPE: Record<NormalizedDataType, SummaryDefinition['aggregation'][]> = {
  string: ['count', 'min', 'max'],
  number: ['count', 'sum', 'avg', 'min', 'max'],
  date: ['count', 'min', 'max'],
  boolean: ['count']
};

@Component({
  selector: 'app-summary-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, PanelModule, ButtonModule, DropdownModule, InputTextModule, DragDropModule],
  templateUrl: './summary-builder.component.html',
  styleUrl: './summary-builder.component.scss'
})
export class SummaryBuilderComponent implements OnChanges {
  @Input() availableFields: Field[] = [];

  @Input() summaries: SummaryDefinition[] = [];

  @Input() grouping: GroupDefinition[] = [];

  @Output() summariesChanged = new EventEmitter<SummaryDefinition[]>();

  protected selectedSummaries: SummaryDefinition[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['availableFields'] || changes['summaries']) {
      this.syncFromInputs();
    }
  }

  protected addSummaryForField(field: Field): void {
    if (!this.hasGrouping()) {
      return;
    }

    const defaultAggregation = this.getAllowedAggregations(field.dataType)[0];
    const baseAlias = this.buildDefaultAlias(defaultAggregation, field.fieldName);
    const alias = this.generateUniqueAlias(baseAlias);

    this.selectedSummaries.push({
      fieldName: field.fieldName,
      displayName: field.displayName,
      aggregation: defaultAggregation,
      alias,
      summaryOrder: this.selectedSummaries.length + 1
    });

    this.emitSummaries();
  }

  protected removeSummary(index: number): void {
    if (index < 0 || index >= this.selectedSummaries.length) {
      return;
    }

    this.selectedSummaries.splice(index, 1);
    this.emitSummaries();
  }

  protected onDrop(event: CdkDragDrop<SummaryDefinition[]>): void {
    if (event.previousContainer !== event.container) {
      return;
    }

    moveItemInArray(this.selectedSummaries, event.previousIndex, event.currentIndex);
    this.emitSummaries();
  }

  protected onAggregationChanged(index: number, aggregation: SummaryDefinition['aggregation'] | null | undefined): void {
    if (!aggregation) {
      return;
    }

    this.updateSummary(index, (summary) => ({
      ...summary,
      aggregation
    }));
  }

  protected onFieldChanged(index: number, fieldName: string | null | undefined): void {
    if (!fieldName) {
      return;
    }

    const metadataField = this.summarizableFields().find(
      (field) => field.fieldName.toLowerCase() === fieldName.toLowerCase()
    );
    if (!metadataField) {
      return;
    }

    this.updateSummary(index, (summary) => {
      const nextAggregation = this.getAllowedAggregations(metadataField.dataType).includes(summary.aggregation)
        ? summary.aggregation
        : this.getAllowedAggregations(metadataField.dataType)[0];
      const nextAlias = this.generateUniqueAlias(
        this.buildDefaultAlias(nextAggregation, metadataField.fieldName),
        index
      );

      return {
        ...summary,
        fieldName: metadataField.fieldName,
        displayName: metadataField.displayName,
        aggregation: nextAggregation,
        alias: nextAlias
      };
    });
  }

  protected onAliasChanged(index: number, alias: string): void {
    this.updateSummary(index, (summary) => ({
      ...summary,
      alias
    }));
  }

  protected hasSummarizableFields(): boolean {
    return this.summarizableFields().length > 0;
  }

  protected hasGrouping(): boolean {
    return this.grouping.length > 0;
  }

  protected fieldOptions(): DropdownOption<string>[] {
    return this.summarizableFields().map((field) => ({
      label: field.displayName,
      value: field.fieldName
    }));
  }

  protected availableSummaryFields(): Field[] {
    return this.summarizableFields();
  }

  protected aggregationOptions(summary: SummaryDefinition): DropdownOption<SummaryDefinition['aggregation']>[] {
    return this.getAllowedAggregations(this.getFieldDataType(summary.fieldName)).map((aggregation) => ({
      label: aggregation.toUpperCase(),
      value: aggregation
    }));
  }

  protected aliasValidationMessage(index: number): string | null {
    const current = this.selectedSummaries[index];
    if (!current) {
      return null;
    }

    const trimmedAlias = (current.alias ?? '').trim();
    if (!trimmedAlias) {
      return 'Alias is required.';
    }

    const fieldNameCollision = this.availableFields.some(
      (field) => field.fieldName.toLowerCase() === trimmedAlias.toLowerCase()
    );
    if (fieldNameCollision) {
      return 'Alias cannot match an existing field name.';
    }

    const duplicateAlias = this.selectedSummaries.some(
      (summary, summaryIndex) =>
        summaryIndex !== index && (summary.alias ?? '').trim().toLowerCase() === trimmedAlias.toLowerCase()
    );
    if (duplicateAlias) {
      return 'Alias must be unique.';
    }

    return null;
  }

  protected trackBySummary(index: number, summary: SummaryDefinition): string {
    return `${summary.fieldName}:${summary.alias}:${index}`;
  }

  protected trackByFieldName(_: number, field: Field): string {
    return field.fieldName;
  }

  private updateSummary(index: number, update: (summary: SummaryDefinition) => SummaryDefinition): void {
    if (index < 0 || index >= this.selectedSummaries.length) {
      return;
    }

    this.selectedSummaries = this.selectedSummaries.map((summary, summaryIndex) =>
      summaryIndex === index ? update(summary) : summary
    );

    this.emitSummaries();
  }

  private emitSummaries(): void {
    this.selectedSummaries = this.selectedSummaries.map((summary, index) => ({
      ...summary,
      summaryOrder: index + 1
    }));

    this.summariesChanged.emit([...this.selectedSummaries]);
  }

  private syncFromInputs(): void {
    const summarizableFieldMap = new Map(
      this.summarizableFields().map((field) => [field.fieldName.toLowerCase(), field] as const)
    );

    this.selectedSummaries = this.normalizeSummaries(this.summaries)
      .filter((summary) => summarizableFieldMap.has(summary.fieldName.toLowerCase()))
      .map((summary, index) => {
        const metadataField = summarizableFieldMap.get(summary.fieldName.toLowerCase());
        const aggregation = this.normalizeAggregation(
          summary.aggregation,
          this.getAllowedAggregations(metadataField?.dataType ?? 'string')
        );
        const alias = (summary.alias ?? '').trim() || this.buildDefaultAlias(aggregation, summary.fieldName);

        return {
          fieldName: metadataField?.fieldName ?? summary.fieldName,
          displayName: metadataField?.displayName ?? summary.displayName,
          aggregation,
          alias,
          summaryOrder: index + 1
        };
      });
  }

  private summarizableFields(): Field[] {
    return this.availableFields.filter((field) => field.isSummarizable === true);
  }

  private normalizeSummaries(summaries: SummaryDefinition[]): SummaryDefinition[] {
    return summaries
      .map((summary, index) => ({
        ...summary,
        summaryOrder: Number.isFinite(summary.summaryOrder) ? summary.summaryOrder : index + 1
      }))
      .sort((left, right) => left.summaryOrder - right.summaryOrder);
  }

  private normalizeAggregation(
    aggregation: string,
    allowed: SummaryDefinition['aggregation'][]
  ): SummaryDefinition['aggregation'] {
    const normalized = aggregation.toLowerCase() as SummaryDefinition['aggregation'];
    return allowed.includes(normalized) ? normalized : allowed[0];
  }

  private getAllowedAggregations(dataType: string): SummaryDefinition['aggregation'][] {
    return AGGREGATION_OPTIONS_BY_DATA_TYPE[this.normalizeDataType(dataType)];
  }

  private getFieldDataType(fieldName: string): string {
    const field = this.summarizableFields().find(
      (item) => item.fieldName.toLowerCase() === fieldName.toLowerCase()
    );
    return field?.dataType ?? 'string';
  }

  private normalizeDataType(rawDataType: string): NormalizedDataType {
    const normalized = rawDataType.trim().toLowerCase();
    if (normalized.includes('bool')) {
      return 'boolean';
    }

    if (normalized.includes('date') || normalized.includes('time')) {
      return 'date';
    }

    const numberTokens = ['number', 'numeric', 'int', 'decimal', 'double', 'float', 'long', 'short'];
    if (numberTokens.some((token) => normalized.includes(token))) {
      return 'number';
    }

    return 'string';
  }

  private buildDefaultAlias(aggregation: SummaryDefinition['aggregation'], fieldName: string): string {
    const normalizedFieldName = fieldName.replace(/[^A-Za-z0-9_]/g, '');
    return `${this.toPascalCase(aggregation)}${normalizedFieldName || 'Metric'}`;
  }

  private generateUniqueAlias(baseAlias: string, skipIndex?: number): string {
    const existingNames = new Set(this.availableFields.map((field) => field.fieldName.toLowerCase()));
    for (let index = 0; index < this.selectedSummaries.length; index++) {
      if (index === skipIndex) {
        continue;
      }

      existingNames.add((this.selectedSummaries[index].alias ?? '').trim().toLowerCase());
    }

    let suffix = 1;
    let candidate = this.toSqlIdentifier(baseAlias);
    while (existingNames.has(candidate.toLowerCase())) {
      suffix += 1;
      candidate = this.toSqlIdentifier(`${baseAlias}${suffix}`);
    }

    return candidate;
  }

  private toSqlIdentifier(input: string): string {
    const stripped = input.replace(/[^A-Za-z0-9_]/g, '');
    const fallback = stripped || 'SummaryMetric';
    return /^[0-9]/.test(fallback) ? `S${fallback}` : fallback;
  }

  private toPascalCase(input: string): string {
    if (!input) {
      return '';
    }

    return input.charAt(0).toUpperCase() + input.slice(1).toLowerCase();
  }
}
