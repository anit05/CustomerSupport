import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CategoriesService } from '../../../ticket-categories/services/categories.service';
import { ProductsService } from '../../../products/services/products.service';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
@Component({ selector: 'app-catalog-form', standalone: true, imports: [FormsModule, RouterLink, NoticeComponent, PageHeadingComponent], templateUrl: './catalog-form.component.html' })
export class CatalogFormComponent implements OnInit {
  private readonly categoriesApi = inject(CategoriesService);
  private readonly productsApi = inject(ProductsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  type = this.route.snapshot.data['type'] as 'products' | 'categories';
  id = Number(this.route.snapshot.paramMap.get('id'));
  editing = Number.isInteger(this.id) && this.id > 0;
  singular = this.type === 'products' ? 'Product' : 'Category';
  base = this.type === 'products' ? '/admin/products' : '/admin/ticket-categories';
  heading = (this.editing ? 'Edit ' : 'Add ') + this.singular;
  model = { name: '', description: '', isActive: true };
  error = signal('');
  loading = signal(false);
  saving = signal(false);
  ngOnInit() {
    if (!this.editing)
      return;
    this.loading.set(true);
    const request = this.type === 'products' ? this.productsApi.getById(this.id) : this.categoriesApi.getById(this.id);
    request.subscribe({ next: x => { this.model = { name: x.name, description: x.description || '', isActive: x.isActive }; this.loading.set(false); }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); } });
  }
  save() {
    this.saving.set(true);
    this.error.set('');
    const body = { name: this.model.name.trim(), description: this.model.description.trim() || null };
    const request = this.type === 'products' ? (this.editing ? this.productsApi.update(this.id, { ...body, isActive: this.model.isActive }) : this.productsApi.create(body)) : (this.editing ? this.categoriesApi.update(this.id, { ...body, isActive: this.model.isActive }) : this.categoriesApi.create(body));
    request.subscribe({ next: () => void this.router.navigateByUrl(this.base), error: e => { this.error.set(messageFromError(e)); this.saving.set(false); } });
  }
}
