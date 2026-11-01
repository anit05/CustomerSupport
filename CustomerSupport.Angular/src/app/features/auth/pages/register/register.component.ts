import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
@Component({ selector: 'app-register', standalone: true, imports: [FormsModule, RouterLink, NoticeComponent], templateUrl: './register.component.html' })
export class RegisterComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  model = { firstName: '', lastName: '', email: '', phoneNumber: '', password: '', confirmPassword: '' };
  busy = signal(false);
  error = signal('');
  submit() {
    if (this.model.password !== this.model.confirmPassword)
      return;
    this.busy.set(true);
    this.error.set('');
    this.auth.register({ ...this.model, phoneNumber: this.model.phoneNumber.trim() || null }).subscribe({
      next: () => void this.router.navigateByUrl('/customer/dashboard'),
      error: e => { this.error.set(messageFromError(e)); this.busy.set(false); }
    });
  }
}
