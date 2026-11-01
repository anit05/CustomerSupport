import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { TicketComment } from '../models/ticket.model';
import { TicketAIAnalysisResult, TicketAIDraftResult } from '../models/ticket-ai.model';

/** The backend chooses OpenAI or Ollama. Angular uses identical endpoints for both. */
@Injectable({ providedIn: 'root' })
export class TicketAIService {
  private readonly api = inject(ApiClientService);
  analyze(ticketId: number) {
    return this.api.post<TicketAIAnalysisResult>(`api/ai/tickets/${ticketId}/analysis`, null);
  }
  replyDraft(ticketId: number, articleId: number | null) {
    return this.api.post<TicketAIDraftResult>(`api/ai/tickets/${ticketId}/reply-draft`, { knowledgeBaseArticleId: articleId });
  }
  internalNoteDraft(ticketId: number, articleId: number | null) {
    return this.api.post<TicketAIDraftResult>(`api/ai/tickets/${ticketId}/internal-note-draft`, { knowledgeBaseArticleId: articleId });
  }
  saveReviewedDraft(ticketId: number, approvedText: string, isInternal: boolean) {
    return this.api.post<TicketComment>(`api/ai/tickets/${ticketId}/save-drafts`, { approvedText, isInternal });
  }
}
