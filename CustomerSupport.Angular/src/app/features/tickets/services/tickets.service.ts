import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { TicketSummary, TicketDetails, TicketFilters, CreateTicketRequest, AssignTicketRequest, ChangeTicketStatusRequest, AddTicketCommentRequest, TicketComment, TicketAssignment, TicketAttachment } from '../models/ticket.model';
import { PageResult } from '../../../shared/models/paged-result.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class TicketsService {
  private readonly api = inject(ApiClientService);
  getMyTickets() { return this.api.get<TicketSummary[]>('api/tickets/my'); }
  search(filters: TicketFilters = {}) { return this.api.get<PageResult<TicketSummary>>('api/tickets', { ...filters }); }
  getById(id: number) { return this.api.get<TicketDetails>(`api/tickets/${id}`); }
  create(body: CreateTicketRequest) { return this.api.post<TicketDetails>('api/tickets', body); }
  assign(id: number, supportExecutiveId: number) { return this.api.put<TicketDetails>(`api/tickets/${id}/assign`, { supportExecutiveId } satisfies AssignTicketRequest); }
  changeStatus(id: number, statusId: number, remarks: string) { return this.api.put<TicketDetails>(`api/tickets/${id}/status`, { statusId, remarks } satisfies ChangeTicketStatusRequest); }
  addComment(id: number, content: string, isInternal: boolean) { return this.api.post<TicketComment>(`api/tickets/${id}/comments`, { content, isInternal } satisfies AddTicketCommentRequest); }
  getAssignments(id: number) { return this.api.get<TicketAssignment[]>(`api/tickets/${id}/assignments`); }
  getAttachments(id: number) { return this.api.get<TicketAttachment[]>(`api/tickets/${id}/attachments`); }
  uploadAttachment(id: number, file: File) {
    const body = new FormData();
    body.append('file', file, file.name);
    // Do not manually set Content-Type; the browser supplies the multipart boundary.
    return this.api.post<TicketAttachment>(`api/tickets/${id}/attachments`, body);
  }
  downloadAttachment(id: number, attachmentId: number) { return this.api.download(`api/tickets/${id}/attachments/${attachmentId}/download`); }
}
