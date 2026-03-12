export type SummaryAggregation = 'count' | 'sum' | 'avg' | 'min' | 'max';

export interface SummaryDefinition {
  fieldName: string;
  displayName: string;
  aggregation: SummaryAggregation;
  alias: string;
  summaryOrder: number;
}
