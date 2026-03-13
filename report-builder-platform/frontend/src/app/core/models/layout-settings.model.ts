export type PageOrientation = 'portrait' | 'landscape';

export type PageSize = 'A4' | 'Letter';

export interface LayoutSettings {
  templateId: string;
  reportTitle: string;
  subtitle?: string;
  logoUrl?: string;
  headerText?: string;
  footerText?: string;
  pageOrientation: PageOrientation;
  pageSize: PageSize;
  showGeneratedDate: boolean;
  showPageNumbers: boolean;
}

export const DEFAULT_TEMPLATE_ID = 'simple-table';

export function createDefaultLayoutSettings(): LayoutSettings {
  return {
    templateId: DEFAULT_TEMPLATE_ID,
    reportTitle: 'New Report',
    subtitle: '',
    logoUrl: '',
    headerText: '',
    footerText: '',
    pageOrientation: 'portrait',
    pageSize: 'A4',
    showGeneratedDate: true,
    showPageNumbers: true
  };
}

export function normalizeLayoutSettings(value: unknown): LayoutSettings {
  const defaults = createDefaultLayoutSettings();
  if (!value || typeof value !== 'object') {
    return defaults;
  }

  const candidate = value as Partial<LayoutSettings>;
  return {
    templateId: normalizeTemplateId(candidate.templateId, defaults.templateId),
    reportTitle: normalizeRequiredText(candidate.reportTitle, defaults.reportTitle),
    subtitle: normalizeOptionalText(candidate.subtitle),
    logoUrl: normalizeOptionalText(candidate.logoUrl),
    headerText: normalizeOptionalText(candidate.headerText),
    footerText: normalizeOptionalText(candidate.footerText),
    pageOrientation: normalizePageOrientation(candidate.pageOrientation),
    pageSize: normalizePageSize(candidate.pageSize),
    showGeneratedDate: typeof candidate.showGeneratedDate === 'boolean' ? candidate.showGeneratedDate : defaults.showGeneratedDate,
    showPageNumbers: typeof candidate.showPageNumbers === 'boolean' ? candidate.showPageNumbers : defaults.showPageNumbers
  };
}

function normalizeTemplateId(value: string | undefined, fallback: string): string {
  if (!value || typeof value !== 'string') {
    return fallback;
  }

  const trimmed = value.trim();
  return trimmed || fallback;
}

function normalizeRequiredText(value: string | undefined, fallback: string): string {
  if (!value || typeof value !== 'string') {
    return fallback;
  }

  const trimmed = value.trim();
  return trimmed || fallback;
}

function normalizeOptionalText(value: string | undefined): string {
  if (!value || typeof value !== 'string') {
    return '';
  }

  return value.trim();
}

function normalizePageOrientation(value: PageOrientation | undefined): PageOrientation {
  return value === 'landscape' ? 'landscape' : 'portrait';
}

function normalizePageSize(value: PageSize | undefined): PageSize {
  return value === 'Letter' ? 'Letter' : 'A4';
}
