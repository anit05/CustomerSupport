import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { CategoriesService } from '../../../ticket-categories/services/categories.service';
import { ProductsService } from '../../../products/services/products.service';
import { Category } from '../../../ticket-categories/models/category.model';
import { Product } from '../../../products/models/product.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
@Component({ selector: 'app-catalog-list', standalone: true, imports: [RouterLink, NoticeComponent], templateUrl: './catalog-list.component.html' })
export class CatalogListComponent implements OnInit {
  private readonly categoriesApi = inject(CategoriesService);
  private readonly productsApi = inject(ProductsService);
  private route = inject(ActivatedRoute);
  type = this.route.snapshot.data['type'] as 'products' | 'categories';
  heading = this.type === 'products' ? 'Products' : 'Ticket categories';
  description = this.type === 'products' ? 'Maintain your product catalog and active states.' : 'Organize tickets using relevant service categories.';
  singular = this.type === 'products' ? 'Product' : 'Category';
  base = this.type === 'products' ? '/admin/products' : '/admin/ticket-categories';
  items = signal<(Product | Category)[]>([]);
  loading = signal(true);
  error = signal('');
  activeCount(): number { return this.items().filter(item => item.isActive).length; }
  ngOnInit() {
    const request = this.type === 'products' ? this.productsApi.getAll(true).pipe(map(items => items as (Product | Category)[])) : this.categoriesApi.getAll(true).pipe(map(items => items as (Product | Category)[]));
    request.subscribe({ next: result => { this.items.set(result); this.loading.set(false); }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); } });
  }
}
