import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/services/auth.service';

interface NavLink {
  title: string;
  path: string;
  icon: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html'
})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  links(): NavLink[] {
    const knowledge: NavLink[] = [
      { title: 'Knowledge base', path: '/knowledge-base', icon: 'bi-journal-richtext' },
      { title: 'FAQs', path: '/faqs', icon: 'bi-patch-question' }
    ];
    if (this.auth.role() === 'Customer') {
      return [
        { title: 'Overview', path: '/customer/dashboard', icon: 'bi-grid-1x2' },
        { title: 'My tickets', path: '/tickets/my', icon: 'bi-ticket-perforated' },
        { title: 'New ticket', path: '/tickets/new', icon: 'bi-plus-circle' },
        ...knowledge
      ];
    }
    if (this.auth.role() === 'SupportExecutive') {
      return [
        { title: 'Overview', path: '/support/dashboard', icon: 'bi-grid-1x2' },
        { title: 'Assigned tickets', path: '/support/tickets', icon: 'bi-inboxes' },
        ...knowledge
      ];
    }
    return [
      { title: 'Overview', path: '/admin/dashboard', icon: 'bi-grid-1x2' },
      { title: 'All tickets', path: '/admin/tickets', icon: 'bi-ticket-detailed' },
      { title: 'Products', path: '/admin/products', icon: 'bi-box-seam' },
      { title: 'Ticket categories', path: '/admin/ticket-categories', icon: 'bi-tags' },
      { title: 'Knowledge base', path: '/admin/knowledge-base', icon: 'bi-journal-text' },
      { title: 'FAQs', path: '/admin/faqs', icon: 'bi-patch-question' }
    ];
  }

  roleLabel(): string {
    return this.auth.role() === 'SupportExecutive' ? 'Support Executive' : (this.auth.role() ?? 'User');
  }

  initials(): string {
    return (this.auth.user()?.fullName || 'CS').split(' ').filter(Boolean).slice(0, 2).map(x => x.charAt(0)).join('').toUpperCase();
  }

  logout(): void {
    this.auth.logout().subscribe(() => void this.router.navigate(['/login']));
  }
}
