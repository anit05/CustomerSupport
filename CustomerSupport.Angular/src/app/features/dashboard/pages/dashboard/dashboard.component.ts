import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TicketsService } from '../../../tickets/services/tickets.service';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { TicketSummary } from '../../../tickets/models/ticket.model';
import { ticketBadge } from '../../../../shared/utils/status-badges';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { DatePipe } from '@angular/common';
import { map } from 'rxjs';
@Component({ selector: 'app-dashboard', standalone: true, imports: [RouterLink, DatePipe, PageHeadingComponent, NoticeComponent, EmptyStateComponent], templateUrl: './dashboard.component.html' })
export class DashboardComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readonly ticketsApi = inject(TicketsService);
  auth = inject(AuthService);
  mode = this.route.snapshot.data['mode'] as 'customer' | 'support' | 'admin';
  tickets = signal<TicketSummary[]>([]);
  loading = signal(true);
  error = signal('');
  ticketBadge = ticketBadge;
  heading = this.mode === 'customer' ? 'Customer dashboard' : this.mode === 'support' ? 'Support workspace' : 'Administrator overview';
  subtitle = this.mode === 'customer' ? 'Welcome back. Here is the latest activity on your support requests.' : this.mode === 'support' ? 'Stay on top of assigned tickets and customer conversations.' : 'Monitor support activity and manage your service operations.';
  listUrl = this.mode === 'customer' ? '/tickets/my' : this.mode === 'support' ? '/support/tickets' : '/admin/tickets';
  cards = computed(() => {
    const x = this.tickets();
    return [
      { label: 'Total tickets', count: x.length, description: this.mode === 'customer' ? 'All your tickets' : 'First 100 accessible tickets' },
      { label: 'Open / Assigned', count: x.filter(t => [1, 2].includes(t.statusId)).length, description: 'Awaiting progress' },
      { label: 'In progress', count: x.filter(t => t.statusId === 3).length, description: 'Being handled' },
      { label: 'Resolved / Closed', count: x.filter(t => [4, 5].includes(t.statusId)).length, description: 'Completed requests' }
    ];
  });
  ngOnInit() {
    const request = this.mode === 'customer' ? this.ticketsApi.getMyTickets() : this.ticketsApi.search({ pageSize: 100, pageNumber: 1 }).pipe(map(page => page.items));
    request.subscribe({
      next: result => {
        this.tickets.set(result);
        this.loading.set(false);
      }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); }
    });
  }
}
