export interface Field {
  id: number;
  datasetId: number;
  fieldName: string;
  displayName: string;
  dataType: string;
  isFilterable: boolean;
  isGroupable: boolean;
  isSummarizable: boolean;
}
