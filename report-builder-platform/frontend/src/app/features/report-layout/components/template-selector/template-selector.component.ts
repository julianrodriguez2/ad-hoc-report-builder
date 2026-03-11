import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';

@Component({
  selector: 'app-template-selector',
  standalone: true,
  imports: [CommonModule, ButtonModule, PanelModule],
  templateUrl: './template-selector.component.html',
  styleUrl: './template-selector.component.scss'
})
export class TemplateSelectorComponent {
  protected readonly templates = ['Tabular', 'Matrix', 'Executive'];
}
