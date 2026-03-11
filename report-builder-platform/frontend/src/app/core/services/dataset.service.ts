import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Dataset } from '../models/dataset.model';
import { Field } from '../models/field.model';

@Injectable({
  providedIn: 'root'
})
export class DatasetService {
  private readonly apiBaseUrl = '/api';

  constructor(private readonly http: HttpClient) {}

  getDatasets(): Observable<Dataset[]> {
    return this.http.get<Dataset[]>(`${this.apiBaseUrl}/datasets`);
  }

  getDatasetFields(datasetId: string): Observable<Field[]> {
    return this.http.get<Field[]>(`${this.apiBaseUrl}/datasets/${datasetId}/fields`);
  }
}
