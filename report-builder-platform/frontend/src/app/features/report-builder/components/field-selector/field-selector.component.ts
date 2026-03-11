import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { Field } from '../../../../core/models/field.model';

@Component({
  selector: 'app-field-selector',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './field-selector.component.html',
  styleUrl: './field-selector.component.scss'
})
export class FieldSelectorComponent {
  @Input() availableFields: Field[] = [];

  @Input() selectedFieldNames: string[] = [];

  @Output() selectedFieldNamesChange = new EventEmitter<string[]>();

  protected isSelected(fieldName: string): boolean {
    return this.selectedFieldNames.includes(fieldName);
  }

  protected onFieldToggled(fieldName: string, event: Event): void {
    const isChecked = (event.target as HTMLInputElement).checked;
    const selected = new Set(this.selectedFieldNames);

    if (isChecked) {
      selected.add(fieldName);
    } else {
      selected.delete(fieldName);
    }

    this.selectedFieldNamesChange.emit(Array.from(selected));
  }
}
