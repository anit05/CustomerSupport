import { Component, input } from '@angular/core';
@Component({ selector: 'app-empty-state', standalone: true, templateUrl: './empty-state.component.html' })
export class EmptyStateComponent {
  symbol = input('📭');
  title = input('No records found');
  subtitle = input('Try updating your search or filters.');
}
