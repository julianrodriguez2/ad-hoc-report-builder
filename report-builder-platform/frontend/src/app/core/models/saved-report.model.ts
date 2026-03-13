import { ReportDefinition } from './report-definition.model';

export interface SavedReportSummary {
  id: string;
  name: string;
  description?: string | null;
  datasetId: string;
  createdAt: string;
}

export interface SavedReportApiResponse {
  id: string;
  name: string;
  description?: string | null;
  datasetId: string;
  definitionJson: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface SavedReport {
  id: string;
  name: string;
  description?: string | null;
  datasetId: string;
  definitionJson: string;
  definition: ReportDefinition;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateSavedReportRequest {
  name: string;
  description?: string | null;
  datasetId: string;
  definition: ReportDefinition;
}

export interface UpdateSavedReportRequest {
  name: string;
  description?: string | null;
  definition: ReportDefinition;
}
