import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { PanelModule } from 'primeng/panel';

import { Field } from '../../../../core/models/field.model';
import { GroupDefinition } from '../../../../core/models/group-definition.model';

interface DropdownOption<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-grouping-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, PanelModule, ButtonModule, DropdownModule, DragDropModule],
  templateUrl: './grouping-builder.component.html',
  styleUrl: './grouping-builder.component.scss'
})
export class GroupingBuilderComponent implements OnChanges {
  @Input() availableFields: Field[] = [];

  @Input() grouping: GroupDefinition[] = [];

  @Output() groupingChanged = new EventEmitter<GroupDefinition[]>();

  protected readonly sortDirectionOptions: DropdownOption<GroupDefinition['sortDirection']>[] = [
    { label: 'Ascending', value: 'asc' },
    { label: 'Descending', value: 'desc' }
  ];

  protected availableGroupingFields: Field[] = [];
  protected selectedGrouping: GroupDefinition[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['availableFields'] || changes['grouping']) {
      this.syncFromInputs();
    }
  }

  protected onDrop(event: CdkDragDrop<GroupDefinition[]>): void {
    if (event.previousContainer !== event.container) {
      return;
    }

    moveItemInArray(this.selectedGrouping, event.previousIndex, event.currentIndex);
    this.emitGrouping();
  }

  protected addGroupingField(field: Field): void {
    const alreadySelected = this.selectedGrouping.some(
      (group) => group.fieldName.toLowerCase() === field.fieldName.toLowerCase()
    );
    if (alreadySelected) {
      return;
    }

    this.selectedGrouping.push({
      fieldName: field.fieldName,
      displayName: field.displayName,
      sortDirection: 'asc',
      groupOrder: this.selectedGrouping.length + 1
    });

    this.emitGrouping();
  }

  protected removeGroupingField(fieldName: string): void {
    const existingIndex = this.selectedGrouping.findIndex(
      (group) => group.fieldName.toLowerCase() === fieldName.toLowerCase()
    );
    if (existingIndex === -1) {
      return;
    }

    this.selectedGrouping.splice(existingIndex, 1);
    this.emitGrouping();
  }

  protected onSortDirectionChanged(
    fieldName: string,
    sortDirection: GroupDefinition['sortDirection'] | null | undefined
  ): void {
    if (!sortDirection) {
      return;
    }

    this.selectedGrouping = this.selectedGrouping.map((group) =>
      group.fieldName.toLowerCase() === fieldName.toLowerCase()
        ? { ...group, sortDirection: this.normalizeSortDirection(sortDirection) }
        : group
    );

    this.emitGrouping();
  }

  protected hasGroupableFields(): boolean {
    return this.getGroupableFields().length > 0;
  }

  protected trackByFieldName(_: number, item: Field | GroupDefinition): string {
    return item.fieldName;
  }

  private syncFromInputs(): void {
    const groupableFieldMap = new Map(
      this.getGroupableFields().map((field) => [field.fieldName.toLowerCase(), field] as const)
    );

    this.selectedGrouping = this.normalizeGrouping(this.grouping)
      .filter((group) => groupableFieldMap.has(group.fieldName.toLowerCase()))
      .map((group, index) => {
        const metadataField = groupableFieldMap.get(group.fieldName.toLowerCase());

        return {
          fieldName: metadataField?.fieldName ?? group.fieldName,
          displayName: metadataField?.displayName ?? group.displayName,
          sortDirection: this.normalizeSortDirection(group.sortDirection),
          groupOrder: index + 1
        };
      });

    this.refreshAvailableGroupingFields();
  }

  private emitGrouping(): void {
    this.selectedGrouping = this.selectedGrouping.map((group, index) => ({
      ...group,
      groupOrder: index + 1
    }));

    this.refreshAvailableGroupingFields();
    this.groupingChanged.emit([...this.selectedGrouping]);
  }

  private refreshAvailableGroupingFields(): void {
    const selectedFieldNames = new Set(this.selectedGrouping.map((group) => group.fieldName.toLowerCase()));
    this.availableGroupingFields = this.getGroupableFields().filter(
      (field) => !selectedFieldNames.has(field.fieldName.toLowerCase())
    );
  }

  private getGroupableFields(): Field[] {
    return this.availableFields.filter((field) => field.isGroupable !== false);
  }

  private normalizeGrouping(grouping: GroupDefinition[]): GroupDefinition[] {
    return grouping
      .map((group, index) => ({
        ...group,
        groupOrder: Number.isFinite(group.groupOrder) ? group.groupOrder : index + 1
      }))
      .sort((left, right) => left.groupOrder - right.groupOrder);
  }

  private normalizeSortDirection(sortDirection: string | undefined): GroupDefinition['sortDirection'] {
    return sortDirection?.toLowerCase() === 'desc' ? 'desc' : 'asc';
  }
}
