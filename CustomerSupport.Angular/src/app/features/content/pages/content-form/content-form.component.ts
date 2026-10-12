import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { KnowledgeBaseService } from '../../../knowledge-base/services/knowledge-base.service';
import { FaqsService } from '../../../faqs/services/faqs.service';
import { ProductsService } from '../../../products/services/products.service';
import { Product } from '../../../products/models/product.model';
import { Article, CreateArticleRequest } from '../../../knowledge-base/models/article.model';
import { Faq, CreateFaqRequest } from '../../../faqs/models/faq.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
@Component({ selector: 'app-content-form', standalone: true, imports: [FormsModule, RouterLink, NoticeComponent, PageHeadingComponent], templateUrl: './content-form.component.html' })
export class ContentFormComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private readonly knowledgeBaseApi = inject(KnowledgeBaseService);
  private readonly faqsApi = inject(FaqsService);
  private readonly productsApi = inject(ProductsService);
  type = this.route.snapshot.data['type'] as 'faqs' | 'knowledge-base';
  id = Number(this.route.snapshot.paramMap.get('id'));
  editing = Number.isInteger(this.id) && this.id > 0;
  heading = (this.editing ? 'Edit ' : 'Create ') + (this.type === 'faqs' ? 'FAQ' : 'knowledge base article');
  backUrl = this.type === 'faqs' ? '/admin/faqs' : '/admin/knowledge-base';
  title = '';
  content = '';
  productId: number | null = null;
  isActive = true;
  isPublished = false;
  products = signal<Product[]>([]);
  loading = signal(false);
  saving = signal(false);
  error = signal('');
  ngOnInit() {
    this.productsApi.getAll(true).subscribe({ next: x => this.products.set(x), error: e => this.error.set(messageFromError(e)) });
    if (!this.editing)
      return;
    this.loading.set(true);
    const req: Observable<Faq | Article> = this.type === 'faqs' ? this.faqsApi.getById(this.id) : this.knowledgeBaseApi.getById(this.id);
    req.subscribe({
      next: (x: Faq | Article) => {
        this.title = this.type === 'faqs' ? (x as Faq).question : (x as Article).title;
        this.content = this.type === 'faqs' ? (x as Faq).answer : (x as Article).content;
        this.productId = x.productId ?? null;
        this.isActive = x.isActive;
        if (this.type === 'knowledge-base')
          this.isPublished = (x as Article).isPublished;
        this.loading.set(false);
      }, error: (e: unknown) => { this.error.set(messageFromError(e)); this.loading.set(false); }
    });
  }
  save(): void {
    this.saving.set(true);
    this.error.set('');
    // FAQs and knowledge-base articles have different API request DTOs.
    // Construct each one explicitly instead of sending an untyped union.
    let request: Observable<Article | Faq>;
    if (this.type === 'faqs') {
      const body: CreateFaqRequest = {
        question: this.title.trim(),
        answer: this.content.trim(),
        productId: this.productId
      };
      request = this.editing
        ? this.faqsApi.update(this.id, { ...body, isActive: this.isActive })
        : this.faqsApi.create(body);
    }
    else {
      const body: CreateArticleRequest = {
        title: this.title.trim(),
        content: this.content.trim(),
        productId: this.productId,
        isPublished: this.isPublished
      };
      request = this.editing
        ? this.knowledgeBaseApi.update(this.id, { ...body, isActive: this.isActive })
        : this.knowledgeBaseApi.create(body);
    }
    request.subscribe({
      next: () => void this.router.navigateByUrl(this.backUrl),
      error: e => {
        this.error.set(messageFromError(e));
        this.saving.set(false);
      }
    });
  }
}
