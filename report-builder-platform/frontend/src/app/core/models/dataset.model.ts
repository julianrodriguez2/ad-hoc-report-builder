export interface Dataset {
  id: string;
  name: string;
  description: string;
  viewName?: string;
  createdAt?: string;
  previewRowLimit?: number | null;
  maxExecutionRowLimit?: number | null;
  requireAtLeastOneFilter?: boolean;
  requireDateFilter?: boolean;
  largeDatasetThreshold?: number | null;
  timeoutSeconds?: number | null;
}
