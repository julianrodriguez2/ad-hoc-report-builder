import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CalendarModule } from 'primeng/calendar';
import { DropdownModule } from 'primeng/dropdown';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { PanelModule } from 'primeng/panel';

import { DateRangeValue, FilterDefinition, NumberRangeValue } from '../../../../core/models/filter-definition.model';
import { Field } from '../../../../core/models/field.model';
import {
  FilterOperatorOption,
  createDefaultValue,
  getDefaultOperator,
  getOperatorsForDataType,
  isBetweenOperator,
  normalizeDataType,
  requiresValue
} from '../../utils/filter-operators.util';

interface DropdownOption<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-filter-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PanelModule,
    ButtonModule,
    DropdownModule,
    InputTextModule,
    InputNumberModule,
    CalendarModule
  ],
  templateUrl: './filter-builder.component.html',
  styleUrl: './filter-builder.component.scss'
})
export class FilterBuilderComponent {
  @Input() availableFields: Field[] = [];

  @Input() filters: FilterDefinition[] = [];

  @Output() filtersChanged = new EventEmitter<FilterDefinition[]>();

  protected readonly booleanOptions: DropdownOption<boolean>[] = [
    { label: 'True', value: true },
    { label: 'False', value: false }
  ];

  private readonly isoDateCache = new Map<string, Date>();

  protected addFilter(): void {
    if (!this.hasFilterableFields()) {
      return;
    }

    const newFilter: FilterDefinition = {
      id: this.createFilterId(),
      fieldName: '',
      displayName: undefined,
      dataType: '',
      operator: '',
      value: null
    };

    this.emitFilters([...this.filters, newFilter]);
  }

  protected removeFilter(filterId: string): void {
    this.emitFilters(this.filters.filter((filter) => filter.id !== filterId));
  }

  protected onFieldChanged(filterId: string, selectedFieldName: string | null | undefined): void {
    if (!selectedFieldName) {
      this.updateFilter(filterId, () => ({
        id: filterId,
        fieldName: '',
        displayName: undefined,
        dataType: '',
        operator: '',
        value: null
      }));
      return;
    }

    const selectedField = this.filterableFields().find((field) => field.fieldName === selectedFieldName);
    if (!selectedField) {
      return;
    }

    const defaultOperator = getDefaultOperator(selectedField.dataType);
    this.updateFilter(filterId, (currentFilter) => ({
      ...currentFilter,
      fieldName: selectedField.fieldName,
      displayName: selectedField.displayName,
      dataType: selectedField.dataType,
      operator: defaultOperator,
      value: createDefaultValue(selectedField.dataType, defaultOperator)
    }));
  }

  protected onOperatorChanged(filterId: string, selectedOperator: string | null | undefined): void {
    this.updateFilter(filterId, (currentFilter) => ({
      ...currentFilter,
      operator: selectedOperator ?? '',
      value: createDefaultValue(currentFilter.dataType, selectedOperator ?? '')
    }));
  }

  protected onStringValueChanged(filterId: string, value: string): void {
    this.updateFilter(filterId, (currentFilter) => ({
      ...currentFilter,
      value
    }));
  }

  protected onNumberValueChanged(filterId: string, value: number | null): void {
    this.updateFilter(filterId, (currentFilter) => ({
      ...currentFilter,
      value
    }));
  }

  protected onNumberRangeValueChanged(filterId: string, key: keyof NumberRangeValue, value: number | null): void {
    this.updateFilter(filterId, (currentFilter) => {
      const currentRange = this.getNumberRangeValue(currentFilter);
      return {
        ...currentFilter,
        value: {
          ...currentRange,
          [key]: value
        }
      };
    });
  }

  protected onDateValueChanged(filterId: string, value: Date | null): void {
    const newIsoString = this.dateToIsoString(value);
    const currentFilter = this.filters.find((f) => f.id === filterId);
    const currentIsoString = typeof currentFilter?.value === 'string' ? currentFilter.value : null;
    if (currentIsoString === newIsoString) {
      return;
    }
    this.updateFilter(filterId, (filter) => ({
      ...filter,
      value: newIsoString
    }));
  }

  protected onDateRangeValueChanged(filterId: string, key: keyof DateRangeValue, value: Date | null): void {
    const newIsoString = this.dateToIsoString(value);
    this.updateFilter(filterId, (currentFilter) => {
      const currentRange = this.getDateRangeValue(currentFilter);
      if (currentRange[key] === newIsoString) {
        return currentFilter;
      }
      return {
        ...currentFilter,
        value: {
          ...currentRange,
          [key]: newIsoString
        }
      };
    });
  }

  protected onBooleanValueChanged(filterId: string, value: boolean | null): void {
    this.updateFilter(filterId, (currentFilter) => ({
      ...currentFilter,
      value
    }));
  }

  protected fieldOptions(): DropdownOption<string>[] {
    return this.filterableFields().map((field) => ({
      label: field.displayName,
      value: field.fieldName
    }));
  }

  protected operatorOptions(filter: FilterDefinition): FilterOperatorOption[] {
    if (!filter.fieldName) {
      return [];
    }

    return getOperatorsForDataType(filter.dataType);
  }

  protected hasFilterableFields(): boolean {
    return this.filterableFields().length > 0;
  }

  protected operatorDisabled(filter: FilterDefinition): boolean {
    return !filter.fieldName;
  }

  protected valueDisabled(filter: FilterDefinition): boolean {
    return !filter.fieldName || !filter.operator || !requiresValue(filter.operator);
  }

  protected requiresValueForOperator(operator: string): boolean {
    return requiresValue(operator);
  }

  protected isBetween(filter: FilterDefinition): boolean {
    return isBetweenOperator(filter.operator);
  }

  protected isStringFilter(filter: FilterDefinition): boolean {
    return normalizeDataType(filter.dataType) === 'string';
  }

  protected isNumberFilter(filter: FilterDefinition): boolean {
    return normalizeDataType(filter.dataType) === 'number';
  }

  protected isDateFilter(filter: FilterDefinition): boolean {
    return normalizeDataType(filter.dataType) === 'date';
  }

  protected isBooleanFilter(filter: FilterDefinition): boolean {
    return normalizeDataType(filter.dataType) === 'boolean';
  }

  protected stringValue(filter: FilterDefinition): string {
    return typeof filter.value === 'string' ? filter.value : '';
  }

  protected numberValue(filter: FilterDefinition): number | null {
    return typeof filter.value === 'number' ? filter.value : null;
  }

  protected numberRangeValue(filter: FilterDefinition, key: keyof NumberRangeValue): number | null {
    return this.getNumberRangeValue(filter)[key];
  }

  protected dateValue(filter: FilterDefinition): Date | null {
    if (typeof filter.value !== 'string') {
      return null;
    }

    return this.isoStringToDate(filter.value);
  }

  protected dateRangeValue(filter: FilterDefinition, key: keyof DateRangeValue): Date | null {
    const range = this.getDateRangeValue(filter);
    return this.isoStringToDate(range[key]);
  }

  protected booleanValue(filter: FilterDefinition): boolean | null {
    return typeof filter.value === 'boolean' ? filter.value : null;
  }

  protected trackByFilterId(_: number, filter: FilterDefinition): string {
    return filter.id;
  }

  private updateFilter(filterId: string, update: (filter: FilterDefinition) => FilterDefinition): void {
    let changed = false;
    const nextFilters = this.filters.map((filter) => {
      if (filter.id !== filterId) {
        return filter;
      }
      const updated = update(filter);
      if (updated !== filter) {
        changed = true;
      }
      return updated;
    });
    if (changed) {
      this.emitFilters(nextFilters);
    }
  }

  private emitFilters(filters: FilterDefinition[]): void {
    this.filtersChanged.emit(filters);
  }

  private filterableFields(): Field[] {
    return this.availableFields.filter((field) => field.isFilterable !== false);
  }

  private getNumberRangeValue(filter: FilterDefinition): NumberRangeValue {
    if (
      typeof filter.value === 'object' &&
      filter.value !== null &&
      'min' in filter.value &&
      'max' in filter.value
    ) {
      return filter.value as NumberRangeValue;
    }

    return { min: null, max: null };
  }

  private getDateRangeValue(filter: FilterDefinition): DateRangeValue {
    if (
      typeof filter.value === 'object' &&
      filter.value !== null &&
      'start' in filter.value &&
      'end' in filter.value
    ) {
      return filter.value as DateRangeValue;
    }

    return { start: null, end: null };
  }

  private createFilterId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }

    return `filter-${Date.now()}-${Math.random().toString(16).slice(2)}`;
  }

  private dateToIsoString(value: Date | null): string | null {
    if (!value) {
      return null;
    }

    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private isoStringToDate(value: string | null): Date | null {
    if (!value) {
      return null;
    }

    const cachedDate = this.isoDateCache.get(value);
    if (cachedDate) {
      return cachedDate;
    }

    const parts = value.split('-').map((part) => Number(part));
    if (parts.length !== 3 || parts.some((part) => Number.isNaN(part))) {
      return null;
    }

    const [year, month, day] = parts;
    const parsedDate = new Date(year, month - 1, day);
    this.isoDateCache.set(value, parsedDate);
    return parsedDate;
  }
}
