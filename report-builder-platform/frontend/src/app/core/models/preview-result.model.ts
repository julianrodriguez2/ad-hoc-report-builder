export interface PreviewColumn {
  fieldName: string;
  displayName: string;
}

export interface PreviewResult {
  columns: PreviewColumn[];
  rows: Record<string, unknown>[];
  rowCount: number;
  isTruncated: boolean;
  debugSql?: string | null;
}
