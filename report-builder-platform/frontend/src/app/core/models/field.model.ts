export interface Field {
  id: string;
  datasetId?: string;
  fieldName: string;
  displayName: string;
  dataType: string;
  isFilterable?: boolean;
  isGroupable?: boolean;
  isSummarizable?: boolean;
}
