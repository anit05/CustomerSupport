
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { TicketsService } from '../../services/tickets.service';
import { TicketAIService } from '../../services/ticket-ai.service';
import { KnowledgeBaseService } from '../../../knowledge-base/services/knowledge-base.service';

import { TicketDetails } from '../../models/ticket.model';
import {
  TicketAIAnalysisResult,
  TicketAIDraftResult
} from '../../models/ticket-ai.model';

import { Article } from '../../../knowledge-base/models/article.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';

// Identifies the AI operation currently being executed.
type AIAction = 'analysis' | 'reply' | 'internal' | 'save';

@Component({
  selector: 'app-ticket-ai',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    NoticeComponent
  ],
  templateUrl: './ticket-ai.component.html'
})
export class TicketAIComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ticketsApi = inject(TicketsService);
  private readonly ai = inject(TicketAIService);
  private readonly kb = inject(KnowledgeBaseService);

  readonly id = Number(this.route.snapshot.paramMap.get('id'));

  // Ticket information.
  readonly ticket = signal<TicketDetails | null>(null);

  // AI-generated results.
  readonly analysis = signal<TicketAIAnalysisResult | null>(null);
  readonly draft = signal<TicketAIDraftResult | null>(null);

  // Available Knowledge Base articles.
  readonly articles = signal<Article[]>([]);

  // Page states.
  readonly loading = signal(true);
  readonly error = signal('');
  readonly success = signal('');

  // NEW: Identify which AI operation is running.
  readonly workingAction = signal<AIAction | null>(null);

  // NEW: All buttons remain disabled while any operation runs.
  readonly busy = computed(() => this.workingAction() !== null);

  // Draft information.
  draftTitle = '';
  articleId: number | null = null;
  draftText = '';
  isInternal = true;
  confirmCustomerVisible = false;

  ngOnInit(): void {

    // Validate the Ticket ID.
    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.error.set('Invalid ticket ID.');
      this.loading.set(false);
      return;
    }

    // Load ticket details and Knowledge Base articles.
    forkJoin({
      ticket: this.ticketsApi.getById(this.id),
      articles: this.kb.search({
        pageNumber: 1,
        pageSize: 100
      })
    }).subscribe({

      next: value => {

        this.ticket.set(value.ticket);

        // Show active, published and applicable articles.
        this.articles.set(
          value.articles.items.filter(a =>
            a.isActive &&
            a.isPublished &&
            (
              a.productId == null ||
              a.productId === value.ticket.productId
            )
          )
        );

        this.loading.set(false);
      },

      error: () => {

        // Knowledge Base errors must not prevent AI operations.
        this.ticketsApi.getById(this.id).subscribe({

          next: t => {
            this.ticket.set(t);
            this.loading.set(false);

            this.error.set(
              'Knowledge Base articles could not be loaded. ' +
              'AI generation without an article is still available.'
            );
          },

          error: e => {
            this.error.set(messageFromError(e));
            this.loading.set(false);
          }
        });
      }
    });
  }

  // Generate structured analysis for the selected ticket.
  analyze(): void {

    // Prevent multiple AI requests running simultaneously.
    if (this.busy()) {
      return;
    }

    // Clear old results and mark the Analyze button as working.
    this.beginGeneration('analysis');

    this.ai.analyze(this.id).subscribe({

      next: result => {

        // Show only the latest structured analysis.
        this.analysis.set(result);

        this.finish();
      },

      error: e => this.fail(e)
    });
  }

  // Generate a customer reply or an internal note.
  generateDraft(internal: boolean): void {

    if (this.busy()) {
      return;
    }

    // Identify the clicked button.
    const action: AIAction = internal ? 'internal' : 'reply';

    // Clear old results and mark that button as working.
    this.beginGeneration(action);

    // Select the appropriate AI endpoint.
    const operation = internal
      ? this.ai.internalNoteDraft(this.id, this.articleId)
      : this.ai.replyDraft(this.id, this.articleId);

    operation.subscribe({

      next: result => {

        // Display only the latest draft.
        this.draft.set(result);

        // Prepare editable draft content.
        this.draftText = result.draftText;

        this.draftTitle = internal
          ? 'Internal note draft'
          : 'Customer reply draft';

        this.isInternal = internal;
        this.confirmCustomerVisible = false;

        this.finish();
      },

      error: e => this.fail(e)
    });
  }

  // Preselect AI-suggested values on the Ticket Details page.
  // This operation does not save anything to SQL Server.
  applySuggestions(): void {

    if (this.busy()) {
      return;
    }

    const result = this.analysis();

    if (!result) {
      return;
    }

    void this.router.navigate(
      ['/tickets', this.id],
      {
        queryParams: {
          applyAi: true,
          suggestedCategoryId:
            result.suggestedCategoryId ?? undefined,
          suggestedPriorityId:
            result.suggestedPriorityId ?? undefined
        }
      }
    );
  }

  // Save the reviewed draft as a ticket comment.
  saveDraft(): void {

    if (this.busy()) {
      return;
    }

    const text = this.draftText.trim();

    // Validate the reviewed text.
    if (
      this.ticket()?.statusId === 5 ||
      !text ||
      text.length > 1900
    ) {
      this.error.set(
        'The reviewed comment must contain between 1 and ' +
        '1900 characters and the ticket must be open.'
      );
      return;
    }

    // Require confirmation for customer-visible comments.
    if (!this.isInternal && !this.confirmCustomerVisible) {
      this.error.set(
        'Confirm that the customer-visible comment is ' +
        'accurate and contains no confidential information.'
      );
      return;
    }

    // Mark the Save button as working.
    // Do not clear the edited draft while saving.
    this.begin('save');

    this.ai.saveReviewedDraft(
      this.id,
      text,
      this.isInternal
    ).subscribe({

      next: () => {

        this.finish();

        // Clear the saved draft.
        this.draft.set(null);
        this.draftText = '';

        // Return to Ticket Details.
        void this.router.navigate(
          ['/tickets', this.id],
          {
            state: {
              success:
                'Reviewed AI draft saved as a ticket comment.'
            }
          }
        );
      },

      error: e => this.fail(e)
    });
  }

  // Clear all previous AI results before generating new content.
  private clearResults(): void {

    this.analysis.set(null);
    this.draft.set(null);

    this.draftText = '';
    this.draftTitle = '';

    this.isInternal = true;
    this.confirmCustomerVisible = false;
  }

  // Start a new AI generation operation.
  private beginGeneration(action: AIAction): void {

    // Remove the previous AI results.
    this.clearResults();

    // Identify the button currently performing the request.
    this.begin(action);
  }

  // Start an API operation.
  private begin(action: AIAction): void {

    this.error.set('');
    this.success.set('');

    // This single state controls both loading labels
    // and disabling all the other buttons.
    this.workingAction.set(action);
  }

  // Complete an API operation.
  private finish(): void {

    // Clear the active action.
    // All buttons return to their normal state.
    this.workingAction.set(null);
  }

  // Handle errors and restore button states.
  private fail(e: unknown): void {

    this.error.set(messageFromError(e));

    // Re-enable all buttons after the failure.
    this.finish();
  }
}
