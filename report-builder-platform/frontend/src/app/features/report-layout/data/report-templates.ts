import { ReportTemplate } from '../../../core/models/report-template.model';

function buildSvgPreview(primaryColor: string, secondaryColor: string, accentColor: string): string {
  const svg = `
    <svg xmlns="http://www.w3.org/2000/svg" width="240" height="140" viewBox="0 0 240 140">
      <rect width="240" height="140" rx="12" fill="${primaryColor}" />
      <rect x="16" y="14" width="208" height="20" rx="5" fill="${secondaryColor}" />
      <rect x="16" y="44" width="110" height="10" rx="4" fill="${accentColor}" />
      <rect x="16" y="60" width="208" height="8" rx="3" fill="${secondaryColor}" />
      <rect x="16" y="74" width="208" height="8" rx="3" fill="${secondaryColor}" />
      <rect x="16" y="88" width="208" height="8" rx="3" fill="${secondaryColor}" />
      <rect x="16" y="104" width="64" height="20" rx="4" fill="${accentColor}" />
      <rect x="88" y="104" width="64" height="20" rx="4" fill="${accentColor}" />
      <rect x="160" y="104" width="64" height="20" rx="4" fill="${accentColor}" />
    </svg>
  `;

  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

export const REPORT_TEMPLATES: ReportTemplate[] = [
  {
    id: 'simple-table',
    name: 'Simple Table',
    description: 'Basic table report layout',
    previewImageUrl: buildSvgPreview('#eef4ff', '#c9d9f2', '#4f46e5')
  },
  {
    id: 'executive-summary',
    name: 'Executive Summary',
    description: 'Grouped summaries with header sections',
    previewImageUrl: buildSvgPreview('#fff7ed', '#f7d7b5', '#c2410c')
  },
  {
    id: 'detailed-report',
    name: 'Detailed Report',
    description: 'Detailed row data with formatting',
    previewImageUrl: buildSvgPreview('#ecfeff', '#bfe8ee', '#0f766e')
  }
];

export const REPORT_TEMPLATE_IDS = new Set(REPORT_TEMPLATES.map((template) => template.id));
