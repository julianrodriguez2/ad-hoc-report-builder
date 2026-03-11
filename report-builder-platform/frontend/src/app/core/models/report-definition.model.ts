import { FilterDefinition } from './filter-definition.model';
import { GroupDefinition } from './group-definition.model';
import { SummaryDefinition } from './summary-definition.model';

export interface ReportSelectedField {
  fieldName: string;
  displayName: string;
}

export interface ReportDefinition {
  datasetId: string | null;
  fields: ReportSelectedField[];
  filters: FilterDefinition[];
  grouping: GroupDefinition[];
  summaries: SummaryDefinition[];
  layoutSettings: Record<string, unknown>;
}
