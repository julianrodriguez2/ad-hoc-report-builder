import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ReportDefinition } from '../models/report-definition.model';
import {
  CreateSavedReportRequest,
  SavedReport,
  SavedReportApiResponse,
  SavedReportSummary,
  UpdateSavedReportRequest
} from '../models/saved-report.model';

@Injectable({
  providedIn: 'root'
})
export class SavedReportsService {
  private readonly apiBaseUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  getSavedReports(): Observable<SavedReportSummary[]> {
    return this.http.get<SavedReportSummary[]>(`${this.apiBaseUrl}/saved-reports`);
  }

  getSavedReport(id: string): Observable<SavedReport> {
    return this.http
      .get<SavedReportApiResponse>(`${this.apiBaseUrl}/saved-reports/${id}`)
      .pipe(map((response) => this.mapSavedReportResponse(response)));
  }

  createSavedReport(request: CreateSavedReportRequest): Observable<SavedReport> {
    return this.http
      .post<SavedReportApiResponse>(`${this.apiBaseUrl}/saved-reports`, request)
      .pipe(map((response) => this.mapSavedReportResponse(response)));
  }

  updateSavedReport(id: string, request: UpdateSavedReportRequest): Observable<SavedReport> {
    return this.http
      .put<SavedReportApiResponse>(`${this.apiBaseUrl}/saved-reports/${id}`, request)
      .pipe(map((response) => this.mapSavedReportResponse(response)));
  }

  deleteSavedReport(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/saved-reports/${id}`);
  }

  private mapSavedReportResponse(response: SavedReportApiResponse): SavedReport {
    const definition = this.parseDefinitionJson(response.definitionJson);

    return {
      id: response.id,
      name: response.name,
      description: response.description ?? null,
      datasetId: response.datasetId,
      definitionJson: response.definitionJson,
      definition,
      createdAt: response.createdAt,
      updatedAt: response.updatedAt ?? null
    };
  }

  private parseDefinitionJson(definitionJson: string): ReportDefinition {
    const fallback: ReportDefinition = {
      datasetId: null,
      fields: [],
      filters: [],
      grouping: [],
      summaries: [],
      layoutSettings: {}
    };

    if (!definitionJson) {
      return fallback;
    }

    try {
      const parsed = JSON.parse(definitionJson) as Partial<ReportDefinition>;
      if (!parsed || typeof parsed !== 'object') {
        return fallback;
      }

      return {
        datasetId: typeof parsed.datasetId === 'string' ? parsed.datasetId : null,
        fields: Array.isArray(parsed.fields) ? parsed.fields : [],
        filters: Array.isArray(parsed.filters) ? parsed.filters : [],
        grouping: Array.isArray(parsed.grouping) ? parsed.grouping : [],
        summaries: Array.isArray(parsed.summaries) ? parsed.summaries : [],
        layoutSettings:
          parsed.layoutSettings && typeof parsed.layoutSettings === 'object' ? parsed.layoutSettings : {}
      };
    } catch {
      return fallback;
    }
  }
}
