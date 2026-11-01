import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, map } from 'rxjs';
import { TicketsService } from '../../../tickets/services/tickets.service';
import { TicketSummary } from '../../../tickets/models/ticket.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
@Component({ selector: 'app-dashboard', standalone: true, imports: [RouterLink, DatePipe, NoticeComponent], templateUrl: './dashboard.component.html' })
export class DashboardComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readonly ticketsApi = inject(TicketsService);
  mode = this.route.snapshot.data['mode'] as 'customer'|'support'|'admin';
  listUrl = this.mode === 'customer' ? '/tickets/my' : this.mode === 'support' ? '/support/tickets' : '/admin/tickets';
  workspaceLabel = this.mode === 'customer' ? 'Customer workspace' : this.mode === 'admin' ? 'Administrator workspace' : 'Support workspace';
  tickets = signal<TicketSummary[]>([]);
  loading = signal(true);
  error = signal('');
  canShowTotals = signal(false);
  totals = signal([0,0,0,0]);
  readonly cards = [
    { label: 'Total tickets', icon: 'bi-ticket-detailed text-primary' },
    { label: 'Open / assigned', icon: 'bi-inbox text-info' },
    { label: 'In progress', icon: 'bi-arrow-repeat text-warning' },
    { label: 'Resolved / closed', icon: 'bi-check-circle text-success' }
  ];
  ngOnInit(): void {
    if (this.mode === 'customer') {
      this.ticketsApi.getMyTickets().subscribe({
        next: rows => { this.tickets.set(rows.slice().sort((a,b)=>b.createdAt.localeCompare(a.createdAt)).slice(0,6)); this.totals.set([rows.length, rows.filter(x=>[1,2].includes(x.statusId)).length, rows.filter(x=>x.statusId===3).length, rows.filter(x=>[4,5].includes(x.statusId)).length]); this.canShowTotals.set(true); this.loading.set(false); },
        error: e => this.fail(e)
      });
    } else {
      const count = (statusId: number) => this.ticketsApi.search({statusId, pageNumber:1, pageSize:1}).pipe(map(x=>x.totalRecords));
      forkJoin({ recent: this.ticketsApi.search({pageNumber:1,pageSize:6}), open: count(1), assigned: count(2), progress: count(3), resolved: count(4), closed: count(5) }).subscribe({
        next: v => {this.tickets.set(v.recent.items);this.totals.set([v.recent.totalRecords,v.open+v.assigned,v.progress,v.resolved+v.closed]);this.canShowTotals.set(true);this.loading.set(false);},
        error: e => this.fail(e)
      });
    }
  }
  private fail(e:unknown): void { this.error.set(messageFromError(e));this.loading.set(false); }
}
