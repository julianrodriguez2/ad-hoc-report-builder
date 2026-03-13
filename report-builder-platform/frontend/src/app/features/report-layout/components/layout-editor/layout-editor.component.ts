import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { InputTextModule } from 'primeng/inputtext';
import { PanelModule } from 'primeng/panel';

import { LayoutSettings, PageOrientation, PageSize, normalizeLayoutSettings } from '../../../../core/models/layout-settings.model';

interface DropdownOption<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-layout-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, PanelModule, InputTextModule, InputTextareaModule, DropdownModule, CheckboxModule],
  templateUrl: './layout-editor.component.html',
  styleUrl: './layout-editor.component.scss'
})
export class LayoutEditorComponent implements OnChanges {
  @Input({ required: true }) layoutSettings!: LayoutSettings;

  @Output() layoutSettingsChanged = new EventEmitter<LayoutSettings>();

  protected readonly orientationOptions: DropdownOption<PageOrientation>[] = [
    { label: 'Portrait', value: 'portrait' },
    { label: 'Landscape', value: 'landscape' }
  ];

  protected readonly pageSizeOptions: DropdownOption<PageSize>[] = [
    { label: 'A4', value: 'A4' },
    { label: 'Letter', value: 'Letter' }
  ];

  protected draftLayoutSettings: LayoutSettings = normalizeLayoutSettings(undefined);

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['layoutSettings']) {
      return;
    }

    this.draftLayoutSettings = normalizeLayoutSettings(this.layoutSettings);
  }

  protected onTemplateFieldChange<K extends keyof LayoutSettings>(property: K, value: LayoutSettings[K]): void {
    this.draftLayoutSettings = {
      ...this.draftLayoutSettings,
      [property]: value
    };

    this.layoutSettingsChanged.emit({ ...this.draftLayoutSettings });
  }
}
