import { ReportTemplate } from '../../../core/models/report-template.model';

export const REPORT_TEMPLATES: ReportTemplate[] = [
  {
    id: 'simple-table',
    name: 'Simple Table',
    description: 'Basic table report layout'
  },
  {
    id: 'executive-summary',
    name: 'Executive Summary',
    description: 'Grouped summaries with header sections'
  },
  {
    id: 'detailed-report',
    name: 'Detailed Report',
    description: 'Detailed row data with formatting'
  }
];

export const REPORT_TEMPLATE_IDS = new Set(REPORT_TEMPLATES.map((template) => template.id));
