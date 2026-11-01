import { Component, inject } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/services/auth.service';
interface NavLink { title: string; path: string; icon: string; }
@Component({selector: 'app-root', standalone: true, imports: [RouterOutlet, RouterLink, RouterLinkActive, NgTemplateOutlet], templateUrl: './app.component.html'})
export class AppComponent {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  workspaceLinks(): NavLink[] {
    switch (this.auth.role()) {
      case 'Customer': return [
        { title: 'Dashboard', path: '/customer/dashboard', icon: 'bi-grid-1x2' },
        { title: 'New ticket', path: '/tickets/new', icon: 'bi-plus-circle' },
        { title: 'My tickets', path: '/tickets/my', icon: 'bi-ticket-detailed' }];
      case 'SupportExecutive': return [
        { title: 'Dashboard', path: '/support/dashboard', icon: 'bi-grid-1x2' },
        { title: 'Assigned tickets', path: '/support/tickets', icon: 'bi-ticket-detailed' }];
      case 'Administrator': return [
        { title: 'Dashboard', path: '/admin/dashboard', icon: 'bi-grid-1x2' },
        { title: 'Tickets', path: '/admin/tickets', icon: 'bi-ticket-detailed' },
        { title: 'Products', path: '/admin/products', icon: 'bi-box-seam' },
        { title: 'Categories', path: '/admin/ticket-categories', icon: 'bi-tags' }];
      default: return [];
    }
  }
  contentLinks(): NavLink[] { return this.auth.role() === 'Administrator' ? [
    { title: 'Knowledge base', path: '/admin/knowledge-base', icon: 'bi-journal-text' },
    { title: 'FAQs', path: '/admin/faqs', icon: 'bi-question-circle' }
  ] : []; }
  resourceLinks(): NavLink[] { return [
    { title: 'Help articles', path: '/knowledge-base', icon: 'bi-journal-bookmark' },
    { title: 'FAQs', path: '/faqs', icon: 'bi-patch-question' },
    { title: 'My account', path: '/account', icon: 'bi-person-circle' }
  ]; }
  roleLabel(): string { return this.auth.role() === 'SupportExecutive' ? 'Support Executive' : (this.auth.role() ?? 'User'); }
  initials(): string { return (this.auth.user()?.fullName || 'CS').split(' ').filter(Boolean).slice(0, 2).map(x=>x[0].toUpperCase()).join(''); }
  home(): string { return this.auth.home(); }
  logout(): void { this.auth.logout().subscribe(() => void this.router.navigate(['/login'])); }
}
