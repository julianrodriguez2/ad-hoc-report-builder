import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { PanelModule } from 'primeng/panel';

@Component({
  selector: 'app-layout-editor',
  standalone: true,
  imports: [CommonModule, PanelModule],
  templateUrl: './layout-editor.component.html',
  styleUrl: './layout-editor.component.scss'
})
export class LayoutEditorComponent {
  protected readonly layoutSections = [
    'Header',
    'Report Body',
    'Footer'
  ];
}
