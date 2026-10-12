import { Component, input } from '@angular/core';
@Component({ selector: 'app-notice', standalone: true, templateUrl: './notice.component.html' })
export class NoticeComponent {
  message = input<string>('');
  kind = input<'danger' | 'success' | 'info'>('danger');
}
