import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
@Component({ selector: 'app-error', standalone: true, imports: [RouterLink], templateUrl: './error.component.html' })
export class ErrorComponent {
  auth = inject(AuthService);
  code = inject(ActivatedRoute).snapshot.data['code'] as string;
  get title() { return this.code === '403' ? 'You do not have access' : this.code === '500' ? 'Service temporarily unavailable' : 'We could not find that page'; }
  get description() { return this.code === '403' ? 'Your current role does not have permission to open this page.' : this.code === '500' ? 'Please check that the Web API is running, then try again.' : 'The page address may have changed or was entered incorrectly.'; }
}
