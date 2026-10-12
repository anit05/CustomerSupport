import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CategoriesService } from '../../../ticket-categories/services/categories.service';
import { TicketsService } from '../../services/tickets.service';
import { MasterDataService } from '../../../master-data/services/master-data.service';
import { ProductsService } from '../../../products/services/products.service';
import { Category } from '../../../ticket-categories/models/category.model';
import { MasterValue } from '../../../master-data/models/master-value.model';
import { Product } from '../../../products/models/product.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
@Component({ selector: 'app-ticket-create', standalone: true, imports: [FormsModule, RouterLink, NoticeComponent, PageHeadingComponent], templateUrl: './ticket-create.component.html' })
export class TicketCreateComponent implements OnInit {
  private readonly categoriesApi = inject(CategoriesService);
  private readonly ticketsApi = inject(TicketsService);
  private readonly masterDataApi = inject(MasterDataService);
  private readonly productsApi = inject(ProductsService);
  private router = inject(Router);
  model = { productId: 0, categoryId: null as number | null, priorityId: 2, subject: '', description: '' };
  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  priorities = signal<MasterValue[]>([]);
  loading = signal(true);
  saving = signal(false);
  error = signal('');
  ngOnInit() {
    forkJoin({ products: this.productsApi.getAll(), categories: this.categoriesApi.getAll(), priorities: this.masterDataApi.getPriorities() }).subscribe({
      next: x => { this.products.set(x.products); this.categories.set(x.categories); this.priorities.set(x.priorities); this.loading.set(false); },
      error: e => { this.error.set(messageFromError(e)); this.loading.set(false); }
    });
  }
  submit() {
    this.saving.set(true);
    this.error.set('');
    this.ticketsApi.create(this.model).subscribe({
      next: ticket => void this.router.navigate(['/tickets', ticket.id]), error: e => { this.error.set(messageFromError(e)); this.saving.set(false); }
    });
  }
}
