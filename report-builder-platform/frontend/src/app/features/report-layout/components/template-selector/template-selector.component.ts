import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PanelModule } from 'primeng/panel';

import { ReportTemplate } from '../../../../core/models/report-template.model';

@Component({
  selector: 'app-template-selector',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './template-selector.component.html',
  styleUrl: './template-selector.component.scss'
})
export class TemplateSelectorComponent {
  @Input() templates: ReportTemplate[] = [];

  @Input() selectedTemplateId: string | null = null;

  @Output() templateChanged = new EventEmitter<string>();

  protected selectTemplate(templateId: string): void {
    if (!templateId || templateId === this.selectedTemplateId) {
      return;
    }

    this.templateChanged.emit(templateId);
  }

  protected isSelected(templateId: string): boolean {
    return this.selectedTemplateId === templateId;
  }

  protected trackByTemplateId(_: number, template: ReportTemplate): string {
    return template.id;
  }
}
