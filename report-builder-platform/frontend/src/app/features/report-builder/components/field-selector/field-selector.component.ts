import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { Field } from '../../../../core/models/field.model';

@Component({
  selector: 'app-field-selector',
  standalone: true,
  imports: [CommonModule, DragDropModule, PanelModule],
  templateUrl: './field-selector.component.html',
  styleUrl: './field-selector.component.scss'
})
export class FieldSelectorComponent implements OnChanges {
  @Input() availableFields: Field[] = [];

  @Input() selectedFields: Field[] = [];

  @Output() fieldsChanged = new EventEmitter<Field[]>();

  protected readonly availableListId = 'available-fields-list';
  protected readonly selectedListId = 'selected-fields-list';

  protected availableList: Field[] = [];
  protected selectedList: Field[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['availableFields'] || changes['selectedFields']) {
      this.syncListsFromInputs();
    }
  }

  protected onDrop(event: CdkDragDrop<Field[]>): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);

      if (event.container.id === this.selectedListId) {
        this.emitSelectedFields();
      }

      return;
    }

    const draggedField = event.previousContainer.data[event.previousIndex];
    if (!draggedField) {
      return;
    }

    if (event.container.id === this.selectedListId) {
      this.moveFieldToSelected(draggedField, event.previousIndex, event.currentIndex);
      return;
    }

    this.moveFieldToAvailable(draggedField, event.previousIndex);
  }

  protected addField(field: Field): void {
    const sourceIndex = this.availableList.findIndex((item) => item.fieldName === field.fieldName);
    if (sourceIndex === -1 || this.selectedList.some((item) => item.fieldName === field.fieldName)) {
      return;
    }

    this.availableList.splice(sourceIndex, 1);
    this.selectedList.push(field);
    this.emitSelectedFields();
  }

  protected removeField(field: Field): void {
    const selectedIndex = this.selectedList.findIndex((item) => item.fieldName === field.fieldName);
    if (selectedIndex === -1) {
      return;
    }

    const [removedField] = this.selectedList.splice(selectedIndex, 1);
    if (removedField) {
      this.insertIntoAvailableList(removedField);
    }

    this.emitSelectedFields();
  }

  protected trackByFieldName(_: number, field: Field): string {
    return field.fieldName;
  }

  private syncListsFromInputs(): void {
    const selectedFieldNames = new Set(this.selectedFields.map((field) => field.fieldName));
    this.selectedList = [...this.selectedFields];
    this.availableList = this.availableFields.filter((field) => !selectedFieldNames.has(field.fieldName));
  }

  private moveFieldToSelected(field: Field, sourceIndex: number, destinationIndex: number): void {
    if (this.selectedList.some((item) => item.fieldName === field.fieldName)) {
      return;
    }

    this.availableList.splice(sourceIndex, 1);
    this.selectedList.splice(destinationIndex, 0, field);
    this.emitSelectedFields();
  }

  private moveFieldToAvailable(field: Field, sourceIndex: number): void {
    this.selectedList.splice(sourceIndex, 1);
    this.insertIntoAvailableList(field);
    this.emitSelectedFields();
  }

  private insertIntoAvailableList(field: Field): void {
    const desiredOrder = this.availableFields.findIndex((item) => item.fieldName === field.fieldName);
    const insertionIndex = this.availableList.findIndex((item) => {
      const availableOrder = this.availableFields.findIndex((availableField) => availableField.fieldName === item.fieldName);
      return availableOrder > desiredOrder;
    });

    if (insertionIndex === -1) {
      this.availableList.push(field);
      return;
    }

    this.availableList.splice(insertionIndex, 0, field);
  }

  private emitSelectedFields(): void {
    this.fieldsChanged.emit([...this.selectedList]);
  }
}
