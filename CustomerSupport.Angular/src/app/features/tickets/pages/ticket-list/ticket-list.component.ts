import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CategoriesService } from '../../../ticket-categories/services/categories.service';
import { MasterDataService } from '../../../master-data/services/master-data.service';
import { ProductsService } from '../../../products/services/products.service';
import { TicketsService } from '../../services/tickets.service';
import { Category } from '../../../ticket-categories/models/category.model';
import { MasterValue } from '../../../master-data/models/master-value.model';
import { PageResult } from '../../../../shared/models/paged-result.model';
import { Product } from '../../../products/models/product.model';
import { TicketFilters, TicketSummary } from '../../models/ticket.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { priorityBadge, ticketBadge } from '../../../../shared/utils/status-badges';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
@Component({ selector: 'app-ticket-list', standalone: true, imports: [FormsModule, DatePipe, RouterLink, PageHeadingComponent, NoticeComponent, EmptyStateComponent], templateUrl: './ticket-list.component.html' })
export class TicketListComponent implements OnInit {
  private readonly categoriesApi = inject(CategoriesService);
  private readonly masterDataApi = inject(MasterDataService);
  private readonly productsApi = inject(ProductsService);
  private readonly ticketsApi = inject(TicketsService);
  private route = inject(ActivatedRoute);
  mode = this.route.snapshot.data['mode'] as 'customer' | 'support' | 'admin';
  title = this.mode === 'customer' ? 'My support tickets' : this.mode === 'support' ? 'Assigned tickets' : 'Ticket management';
  subtitle = this.mode === 'customer' ? 'Track, filter, and review all your support requests.' : this.mode === 'support' ? 'Review tickets assigned to you and continue resolution.' : 'Search and manage support tickets across the organization.';
  filters: TicketFilters = { pageNumber: 1, pageSize: 10, search: '', productId: null, categoryId: null, priorityId: null, statusId: null, customerId: null, assignedToUserId: null, sortBy: 'createdAt', sortDirection: 'desc' };
  result = signal<PageResult<TicketSummary> | null>(null);
  loading = signal(true);
  error = signal('');
  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  priorities = signal<MasterValue[]>([]);
  statuses = signal<MasterValue[]>([]);
  ticketBadge = ticketBadge;
  priorityBadge = priorityBadge;
  ngOnInit() {
    this.load();
    forkJoin({ products: this.productsApi.getAll(this.mode === 'admin'), categories: this.categoriesApi.getAll(this.mode === 'admin'), priorities: this.masterDataApi.getPriorities(), statuses: this.masterDataApi.getStatuses() }).subscribe({
      next: x => { this.products.set(x.products); this.categories.set(x.categories); this.priorities.set(x.priorities); this.statuses.set(x.statuses); }, error: e => this.error.set(messageFromError(e))
    });
  }
  load() { this.loading.set(true); this.error.set(''); this.ticketsApi.search(this.filters).subscribe({ next: x => { this.result.set(x); this.loading.set(false); }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); } }); }
  search() { this.filters.pageNumber = 1; this.load(); }
  changePage(diff: number) { this.filters.pageNumber = Math.max(1, (this.filters.pageNumber ?? 1) + diff); this.load(); }
  reset() { this.filters = { pageNumber: 1, pageSize: 10, search: '', productId: null, categoryId: null, priorityId: null, statusId: null, customerId: null, assignedToUserId: null, sortBy: 'createdAt', sortDirection: 'desc' }; this.load(); }
}
