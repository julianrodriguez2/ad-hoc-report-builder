import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { ReportTemplate } from '../../../../core/models/report-template.model';

@Component({
  selector: 'app-template-selector',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './template-selector.component.html',
  styleUrl: './template-selector.component.scss'
})
export class TemplateSelectorComponent implements OnChanges {
  @Input() templates: ReportTemplate[] = [];

  @Input() selectedTemplateId: string | null = null;

  @Output() templateChanged = new EventEmitter<string>();

  protected activeTemplateId: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedTemplateId']) {
      this.activeTemplateId = this.selectedTemplateId;
    }
  }

  protected selectTemplate(templateId: string): void {
    if (!templateId || templateId === this.activeTemplateId) {
      return;
    }

    this.activeTemplateId = templateId;
    this.templateChanged.emit(templateId);
  }

  protected isSelected(templateId: string): boolean {
    return this.activeTemplateId === templateId;
  }

  protected trackByTemplateId(_: number, template: ReportTemplate): string {
    return template.id;
  }
}
