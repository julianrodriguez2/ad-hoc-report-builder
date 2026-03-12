export interface PreviewColumn {
  fieldName: string;
  displayName: string;
}

export interface PreviewResult {
  columns: PreviewColumn[];
  rows: Record<string, unknown>[];
  rowCount: number;
  isTruncated: boolean;
  appliedRowLimit?: number;
  executionTimeMs?: number | null;
  debugSql?: string | null;
}
