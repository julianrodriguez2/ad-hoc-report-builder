import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';

import { Dataset } from '../../../../core/models/dataset.model';
import { SavedReportSummary } from '../../../../core/models/saved-report.model';
import { DatasetService } from '../../../../core/services/dataset.service';
import { SavedReportsService } from '../../../../core/services/saved-reports.service';

@Component({
  selector: 'app-saved-reports-page',
  standalone: true,
  imports: [CommonModule, CardModule, TableModule, ButtonModule, MessageModule],
  templateUrl: './saved-reports-page.component.html',
  styleUrl: './saved-reports-page.component.scss'
})
export class SavedReportsPageComponent implements OnInit {
  protected reports: SavedReportSummary[] = [];
  protected datasets: Dataset[] = [];
  protected loading = false;
  protected errorMessage: string | null = null;
  protected successMessage: string | null = null;

  constructor(
    private readonly savedReportsService: SavedReportsService,
    private readonly datasetService: DatasetService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  protected goToBuilder(): void {
    this.router.navigate(['/report-builder']);
  }

  protected loadReport(reportId: string): void {
    this.router.navigate(['/report-builder'], {
      queryParams: { savedReportId: reportId }
    });
  }

  protected editReport(reportId: string): void {
    this.router.navigate(['/report-builder'], {
      queryParams: { savedReportId: reportId }
    });
  }

  protected deleteReport(report: SavedReportSummary): void {
    this.successMessage = null;
    this.errorMessage = null;

    const shouldDelete = window.confirm(`Delete saved report "${report.name}"?`);
    if (!shouldDelete) {
      return;
    }

    this.savedReportsService.deleteSavedReport(report.id).subscribe({
      next: () => {
        this.reports = this.reports.filter((item) => item.id !== report.id);
        this.successMessage = 'Saved report deleted successfully.';
      },
      error: () => {
        this.errorMessage = 'Unable to delete saved report.';
      }
    });
  }

  protected getDatasetName(datasetId: string): string {
    const dataset = this.datasets.find((item) => item.id === datasetId);
    return dataset?.name ?? 'Unknown Dataset';
  }

  private loadData(): void {
    this.loading = true;
    this.errorMessage = null;
    this.successMessage = null;

    forkJoin({
      reports: this.savedReportsService.getSavedReports(),
      datasets: this.datasetService.getDatasets()
    })
      .pipe(
        finalize(() => {
          this.loading = false;
        })
      )
      .subscribe({
        next: ({ reports, datasets }) => {
          this.reports = reports;
          this.datasets = datasets;
        },
        error: () => {
          this.errorMessage = 'Unable to load saved reports.';
        }
      });
  }
}
