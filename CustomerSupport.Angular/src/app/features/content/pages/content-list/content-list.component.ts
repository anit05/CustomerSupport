import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';
import { KnowledgeBaseService } from '../../../knowledge-base/services/knowledge-base.service';
import { FaqsService } from '../../../faqs/services/faqs.service';
import { ProductsService } from '../../../products/services/products.service';
import { Article } from '../../../knowledge-base/models/article.model';
import { Faq } from '../../../faqs/models/faq.model';
import { Product } from '../../../products/models/product.model';
import { PageResult } from '../../../../shared/models/paged-result.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
@Component({ selector: 'app-content-list', standalone: true, imports: [FormsModule, RouterLink, PageHeadingComponent, NoticeComponent, EmptyStateComponent], templateUrl: './content-list.component.html' })
export class ContentListComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readonly knowledgeBaseApi = inject(KnowledgeBaseService);
  private readonly faqsApi = inject(FaqsService);
  private readonly productsApi = inject(ProductsService);
  type = this.route.snapshot.data['type'] as 'faqs' | 'knowledge-base';
  management = !!this.route.snapshot.data['management'];
  heading = this.type === 'faqs' ? (this.management ? 'Manage FAQs' : 'Frequently asked questions') : (this.management ? 'Manage knowledge base' : 'Knowledge base');
  description = this.type === 'faqs' ? 'Clear answers to frequently asked questions.' : 'Helpful solutions and articles from the support team.';
  publicBase = this.type === 'faqs' ? '/faqs' : '/knowledge-base';
  adminBase = this.type === 'faqs' ? '/admin/faqs' : '/admin/knowledge-base';
  products = signal<Product[]>([]);
  result = signal<PageResult<Article | Faq> | null>(null);
  loading = signal(true);
  error = signal('');
  success = signal('');
  query = '';
  productId: number | null = null;
  pageNumber = 1;
  ngOnInit() { this.productsApi.getAll(this.management).subscribe({ next: x => this.products.set(x), error: e => this.error.set(messageFromError(e)) }); this.load(); }
  label(item: Article | Faq) { return this.type === 'faqs' ? (item as Faq).question : (item as Article).title; }
  excerpt(item: Article | Faq) { const raw = this.type === 'faqs' ? (item as Faq).answer : (item as Article).content; return raw.length > 200 ? raw.slice(0, 200) + '…' : raw; }
  isPublished(item: Article | Faq) { return (item as Article).isPublished; }
  load() {
    this.loading.set(true);
    this.error.set('');
    const query = { search: this.query, productId: this.productId ?? undefined, pageNumber: this.pageNumber, pageSize: 10 };
    const request = this.type === 'faqs' ? this.faqsApi.search(query).pipe(map(page => page as PageResult<Article | Faq>)) : this.knowledgeBaseApi.search(query).pipe(map(page => page as PageResult<Article | Faq>));
    request.subscribe({ next: x => { this.result.set(x); this.loading.set(false); }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); } });
  }
  search() { this.pageNumber = 1; this.load(); }
  page(delta: number) { this.pageNumber = Math.max(1, this.pageNumber + delta); this.load(); }
  deactivate(id: number) {
    if (!confirm('Deactivate this item? It will no longer be visible to regular users.'))
      return;
    const request = this.type === 'faqs' ? this.faqsApi.deactivate(id) : this.knowledgeBaseApi.deactivate(id);
    request.subscribe({ next: () => { this.success.set('Item deactivated successfully.'); this.load(); }, error: e => this.error.set(messageFromError(e)) });
  }
}
