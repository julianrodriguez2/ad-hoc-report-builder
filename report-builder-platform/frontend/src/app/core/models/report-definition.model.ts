import { FilterDefinition } from './filter-definition.model';
import { GroupDefinition } from './group-definition.model';
import { SummaryDefinition } from './summary-definition.model';

export interface ReportDefinition {
  datasetId: number;
  fields: string[];
  filters: FilterDefinition[];
  grouping: GroupDefinition[];
  summaries: SummaryDefinition[];
  layoutSettings: Record<string, unknown>;
}
