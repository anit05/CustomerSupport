import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
@Component({ selector: 'app-login', standalone: true, imports: [FormsModule, RouterLink, NoticeComponent], templateUrl: './login.component.html' })
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  email = '';
  password = '';
  busy = signal(false);
  error = signal('');
  submit() {
    this.error.set('');
    this.busy.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        // Only navigate to a return URL when it is a local path; guards will recheck the role.
        void this.router.navigateByUrl(returnUrl?.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : this.auth.home());
      }, error: e => { this.error.set(messageFromError(e)); this.busy.set(false); }
    });
  }
}
