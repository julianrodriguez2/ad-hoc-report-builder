import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PreviewResult } from '../models/preview-result.model';
import { ReportDefinition } from '../models/report-definition.model';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private readonly apiBaseUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  previewReport(definition: ReportDefinition): Observable<PreviewResult> {
    return this.http.post<PreviewResult>(`${this.apiBaseUrl}/reports/preview`, definition);
  }
}
