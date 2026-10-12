import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { TicketsService } from '../../services/tickets.service';
import { TicketDetails, TicketAssignment, TicketAttachment } from '../../models/ticket.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { priorityBadge, ticketBadge } from '../../../../shared/utils/status-badges';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
@Component({ selector: 'app-ticket-details', standalone: true, imports: [FormsModule, DatePipe, DecimalPipe, RouterLink, NoticeComponent, PageHeadingComponent], templateUrl: './ticket-details.component.html' })
export class TicketDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readonly ticketsApi = inject(TicketsService);
  auth = inject(AuthService);
  id = Number(this.route.snapshot.paramMap.get('id'));
  ticket = signal<TicketDetails | null>(null);
  loading = signal(true);
  busy = signal(false);
  error = signal('');
  success = signal('');
  attachments = signal<TicketAttachment[]>([]);
  assignments = signal<TicketAssignment[]>([]);
  commentText = '';
  isInternal = false;
  selectedFile: File | null = null;
  supportExecutiveId: number | null = null;
  nextStatusId = 0;
  remarks = '';
  ticketBadge = ticketBadge;
  priorityBadge = priorityBadge;
  get backUrl() { return this.auth.role() === 'Administrator' ? '/admin/tickets' : this.auth.role() === 'SupportExecutive' ? '/support/tickets' : '/tickets/my'; }
  availableTransitions() { const status = this.ticket()?.statusId; return [{ id: 3, name: 'In Progress', from: 2 }, { id: 4, name: 'Resolved', from: 3 }, { id: 3, name: 'Reopen (In Progress)', from: 4 }, { id: 5, name: 'Closed', from: 4 }].filter(x => x.from === status); }
  ngOnInit() {
    if (!Number.isInteger(this.id) || this.id < 1) {
      this.error.set('Invalid ticket ID.');
      this.loading.set(false);
      return;
    }
    this.reload();
  }
  reload() {
    this.ticketsApi.getById(this.id).subscribe({
      next: x => {
        this.ticket.set(x);
        this.attachments.set(x.attachments || []);
        this.loading.set(false);
        if (this.auth.role() !== 'Customer')
          this.ticketsApi.getAssignments(this.id).subscribe({ next: a => this.assignments.set(a), error: e => this.error.set(messageFromError(e)) });
        this.ticketsApi.getAttachments(this.id).subscribe({ next: a => this.attachments.set(a), error: e => this.error.set(messageFromError(e)) });
      }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); }
    });
  }
  run<T>(task: Observable<T>, confirmation: string, after?: () => void) { this.busy.set(true); this.error.set(''); this.success.set(''); task.subscribe({ next: () => { this.success.set(confirmation); this.busy.set(false); after?.(); this.reload(); }, error: e => { this.error.set(messageFromError(e)); this.busy.set(false); } }); }
  addComment() {
    if (!this.commentText.trim())
      return;
    this.run(this.ticketsApi.addComment(this.id, this.commentText.trim(), this.auth.role() === 'Customer' ? false : this.isInternal), 'Comment added.', () => { this.commentText = ''; this.isInternal = false; });
  }
  assign() {
    if (!this.supportExecutiveId || this.supportExecutiveId < 1)
      return;
    this.run(this.ticketsApi.assign(this.id, this.supportExecutiveId), 'Ticket assignment updated.');
  }
  changeStatus() {
    if (!this.availableTransitions().some(x => x.id === this.nextStatusId))
      return;
    this.run(this.ticketsApi.changeStatus(this.id, this.nextStatusId, this.remarks), 'Ticket status updated.', () => { this.nextStatusId = 0; this.remarks = ''; });
  }
  selectFile(event: Event) { const el = event.target as HTMLInputElement; this.selectedFile = el.files?.[0] || null; }
  upload() {
    if (!this.selectedFile)
      return;
    const file = this.selectedFile;
    this.run(this.ticketsApi.uploadAttachment(this.id, file), 'File uploaded.', () => { this.selectedFile = null; });
  }
  download(a: TicketAttachment) {
    this.error.set('');
    this.ticketsApi.downloadAttachment(this.id, a.id).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob)
          return;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = a.originalFileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 5000);
      }, error: e => this.error.set(messageFromError(e))
    });
  }
}
