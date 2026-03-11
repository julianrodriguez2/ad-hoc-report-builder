export interface SummaryDefinition {
  fieldName: string;
  operation: 'sum' | 'avg' | 'min' | 'max' | 'count';
  alias?: string;
}
