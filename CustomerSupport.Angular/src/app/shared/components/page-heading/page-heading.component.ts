import { Component, input } from '@angular/core';
@Component({ selector: 'app-page-heading', standalone: true, templateUrl: './page-heading.component.html' })
export class PageHeadingComponent {
  title = input.required<string>();
  subtitle = input<string>('');
}
