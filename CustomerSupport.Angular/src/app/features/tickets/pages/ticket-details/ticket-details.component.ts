
import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';

import { AuthService } from '../../../../core/auth/services/auth.service';
import { TicketsService } from '../../services/tickets.service';
import { CategoriesService } from '../../../ticket-categories/services/categories.service';
import { MasterDataService } from '../../../master-data/services/master-data.service';

import { Category } from '../../../ticket-categories/models/category.model';
import { MasterValue } from '../../../master-data/models/master-value.model';

import {
  TicketDetails,
  TicketAssignment,
  TicketAttachment,
  TicketComment,
  TicketHistory
} from '../../models/ticket.model';

import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';

@Component({
  selector: 'app-ticket-details',
  standalone: true,
  imports: [
    FormsModule,
    DatePipe,
    DecimalPipe,
    RouterLink,
    NoticeComponent
  ],
  templateUrl: './ticket-details.component.html'
})
export class TicketDetailsComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly ticketsApi = inject(TicketsService);
  private readonly categoriesApi = inject(CategoriesService);
  private readonly mastersApi = inject(MasterDataService);

  readonly auth = inject(AuthService);

  readonly id = Number(this.route.snapshot.paramMap.get('id'));

  readonly ticket = signal<TicketDetails | null>(null);

  readonly loading = signal(true);
  readonly busy = signal(false);

  readonly error = signal('');
  readonly success = signal('');

  readonly attachments = signal<TicketAttachment[]>([]);
  readonly assignments = signal<TicketAssignment[]>([]);

  readonly categories = signal<Category[]>([]);
  readonly priorities = signal<MasterValue[]>([]);
  readonly statuses = signal<MasterValue[]>([]);

  // Display comments from oldest to newest.
  // Internal comments are hidden from Customers.
  readonly visibleComments = computed<TicketComment[]>(() =>
    (this.ticket()?.comments ?? [])
      .filter(x =>
        this.auth.role() !== 'Customer' || !x.isInternal)
      .slice()
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt))
  );

  // Display the latest ticket history first.
  readonly latestHistory = computed<TicketHistory[]>(() =>
    (this.ticket()?.history ?? [])
      .slice()
      .sort((a, b) => b.createdAt.localeCompare(a.createdAt))
  );

  // Display the latest assignment first.
  readonly latestAssignments = computed(() =>
    this.assignments()
      .slice()
      .sort((a, b) => b.assignedAt.localeCompare(a.assignedAt))
  );

  commentText = '';
  isInternal = false;

  selectedFile: File | null = null;

  supportExecutiveId: number | null = null;

  nextStatusId = 0;
  remarks = '';

  selectedCategoryId: number | null = null;
  selectedPriorityId = 0;

  hasPendingSuggestions = false;

  private ignoreQuerySuggestions = false;

  // Only support staff can update a non-closed ticket.
  get canManageTicket(): boolean {
    return this.auth.role() !== 'Customer' &&
      this.ticket()?.statusId !== 5;
  }

  // Navigate back to the appropriate ticket listing.
  get backUrl(): string {
    return this.auth.role() === 'Administrator'
      ? '/admin/tickets'
      : this.auth.role() === 'SupportExecutive'
        ? '/support/tickets'
        : '/tickets/my';
  }

  // Return the allowed next statuses.
  availableTransitions() {
    const s = this.ticket()?.statusId;

    return this.statuses().filter(x =>
      (s === 2 && x.id === 3) ||
      (s === 3 && x.id === 4) ||
      (s === 4 && [3, 5].includes(x.id))
    );
  }

  ngOnInit(): void {

    // Validate the Ticket ID.
    if (!Number.isInteger(this.id) || this.id <= 0) {
      this.error.set('Invalid ticket ID.');
      this.loading.set(false);
      return;
    }

    this.success.set(history.state?.success ?? '');

    // Load ticket details.
    this.reload();

    // Load master data only for support staff.
    if (this.auth.role() !== 'Customer') {

      forkJoin({

        // UPDATED:
        // Get only active Categories.
        // GET /api/ticket-categories
        //
        // This endpoint allows any authenticated user.
        // Do not use getAll(true), because that calls the
        // Administrator-only /api/ticket-categories/all endpoint.
        categories: this.categoriesApi.getAll(),

        priorities: this.mastersApi.getPriorities(),

        statuses: this.mastersApi.getStatuses()

      }).subscribe({

        next: data => {

          this.categories.set(
            data.categories.filter(x => x.isActive)
          );

          this.priorities.set(
            data.priorities.filter(x => x.isActive)
          );

          this.statuses.set(data.statuses);

          // Preselect valid AI suggestions, if supplied.
          this.applyQuerySuggestions();
        },

        error: e => {
          this.error.set(messageFromError(e));
        }

      });
    }
  }

  // Load the ticket and its related information.
  reload(): void {

    this.ticketsApi.getById(this.id).subscribe({

      next: x => {

        this.ticket.set(x);

        // Select current Category and Priority.
        this.selectedCategoryId = x.categoryId ?? null;
        this.selectedPriorityId = x.priorityId;

        // Select the assigned Support Executive.
        this.supportExecutiveId = x.assignedToUserId ?? null;

        this.attachments.set(x.attachments ?? []);

        this.loading.set(false);

        // Apply AI suggestions when requested.
        this.applyQuerySuggestions();

        // Load assignment history for support staff.
        if (this.auth.role() !== 'Customer') {

          this.ticketsApi.getAssignments(this.id).subscribe({

            next: assignments => {
              this.assignments.set(assignments);
            },

            error: e => {
              this.error.set(messageFromError(e));
            }

          });
        }

        // Load ticket attachments.
        this.ticketsApi.getAttachments(this.id).subscribe({

          next: attachments => {
            this.attachments.set(attachments);
          },

          error: e => {
            this.error.set(messageFromError(e));
          }

        });
      },

      error: e => {
        this.error.set(messageFromError(e));
        this.loading.set(false);
      }

    });
  }

  // Preselect AI-suggested Category and Priority.
  // No database update happens here.
  private applyQuerySuggestions(): void {

    const t = this.ticket();

    if (
      this.ignoreQuerySuggestions ||
      !t ||
      !this.priorities().length ||
      this.route.snapshot.queryParamMap.get('applyAi') !== 'true' ||
      t.statusId === 5
    ) {
      return;
    }

    const categoryParam =
      this.route.snapshot.queryParamMap.get('suggestedCategoryId');

    const priorityParam =
      this.route.snapshot.queryParamMap.get('suggestedPriorityId');

    const category = Number(categoryParam);
    const priority = Number(priorityParam);

    // Accept only active Categories.
    if (
      categoryParam &&
      this.categories().some(x => x.id === category)
    ) {
      this.selectedCategoryId = category;
    }

    // Accept only active Priorities.
    if (
      priorityParam &&
      this.priorities().some(x => x.id === priority)
    ) {
      this.selectedPriorityId = priority;
    }

    // Detect whether suggested values differ from saved values.
    this.hasPendingSuggestions =
      this.selectedCategoryId !== (t.categoryId ?? null) ||
      this.selectedPriorityId !== t.priorityId;
  }

  // Common method for saving ticket-related changes.
  private run<T>(
    request: Observable<T>,
    success: string,
    reset?: () => void
  ): void {

    this.busy.set(true);
    this.error.set('');
    this.success.set('');

    request.subscribe({

      next: () => {

        this.busy.set(false);
        this.success.set(success);

        reset?.();

        // Reload the latest ticket information.
        this.reload();
      },

      error: e => {
        this.busy.set(false);
        this.error.set(messageFromError(e));
      }

    });
  }

  // Save Category and Priority approved by support staff.
  updateClassification(): void {

    if (
      !this.canManageTicket ||
      !this.priorities().some(
        x => x.id === this.selectedPriorityId
      ) ||
      (
        this.selectedCategoryId !== null &&
        !this.categories().some(
          x => x.id === this.selectedCategoryId
        )
      )
    ) {
      this.error.set(
        'Choose a valid active category and priority.'
      );
      return;
    }

    this.run(
      this.ticketsApi.updateClassification(
        this.id,
        this.selectedCategoryId,
        this.selectedPriorityId
      ),
      'Ticket Category and Priority updated successfully.',
      () => {

        this.hasPendingSuggestions = false;

        this.ignoreQuerySuggestions = true;

        void this.router.navigate(
          ['/tickets', this.id],
          { replaceUrl: true }
        );
      }
    );
  }

  // Add a customer-visible or internal comment.
  addComment(): void {

    if (!this.commentText.trim()) {
      return;
    }

    this.run(
      this.ticketsApi.addComment(
        this.id,
        this.commentText.trim(),
        this.auth.role() !== 'Customer' && this.isInternal
      ),
      'Comment added.',
      () => {
        this.commentText = '';
        this.isInternal = false;
      }
    );
  }

  // Assign or reassign the ticket.
  assign(): void {

    if (
      this.supportExecutiveId &&
      this.supportExecutiveId > 0
    ) {
      this.run(
        this.ticketsApi.assign(
          this.id,
          this.supportExecutiveId
        ),
        'Ticket assignment updated.'
      );
    }
  }

  // Update ticket status.
  changeStatus(): void {

    if (
      !this.availableTransitions().some(
        x => x.id === this.nextStatusId
      )
    ) {
      return;
    }

    this.run(
      this.ticketsApi.changeStatus(
        this.id,
        this.nextStatusId,
        this.remarks
      ),
      'Ticket status updated.',
      () => {
        this.nextStatusId = 0;
        this.remarks = '';
      }
    );
  }

  // Select a file for attachment upload.
  selectFile(event: Event): void {

    this.selectedFile =
      (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  // Upload the selected attachment.
  upload(): void {

    if (this.selectedFile) {

      this.run(
        this.ticketsApi.uploadAttachment(
          this.id,
          this.selectedFile
        ),
        'File uploaded.',
        () => {
          this.selectedFile = null;
        }
      );
    }
  }

  // Download a ticket attachment.
  download(attachment: TicketAttachment): void {

    this.error.set('');

    this.ticketsApi
      .downloadAttachment(this.id, attachment.id)
      .subscribe({

        next: response => {

          if (!response.body) {
            return;
          }

          const url = URL.createObjectURL(response.body);

          const link = document.createElement('a');

          link.href = url;
          link.download = attachment.originalFileName;

          document.body.appendChild(link);

          link.click();
          link.remove();

          setTimeout(
            () => URL.revokeObjectURL(url),
            5000
          );
        },

        error: e => {
          this.error.set(messageFromError(e));
        }

      });
  }
}
