
import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';

import { AuthService } from '../../../../core/auth/services/auth.service';

import {
  CurrentUser,
  Role
} from '../../../../core/auth/models/auth.model';

import { messageFromError } from '../../../../shared/utils/http-error-message';

import { NoticeComponent } from '../../../../shared/components/notice/notice.component';

import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [
    PageHeadingComponent,
    NoticeComponent
  ],
  templateUrl: './account.component.html'
})
export class AccountComponent implements OnInit {

  private readonly auth = inject(AuthService);

  // Profile returned by GET /api/account/me.
  readonly user = signal<CurrentUser | null>(null);

  // Controls the loading indicator.
  readonly loading = signal(true);

  // Stores API error messages.
  readonly error = signal('');

  ngOnInit(): void {

    this.auth.reloadProfile().subscribe({

      next: (profile: CurrentUser) => {

        this.user.set(profile);

        this.loading.set(false);
      },

      error: (error: unknown) => {

        this.error.set(messageFromError(error));

        this.loading.set(false);
      }

    });
  }

  // Generate avatar initials from the actual user's name.
  initials(): string {

    return (this.user()?.fullName ?? '')
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }

  // Convert the stored role into a readable display name.
  roleLabel(role: Role): string {

    return role === 'SupportExecutive'
      ? 'Support Executive'
      : role;
  }
}
