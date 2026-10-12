import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { map } from 'rxjs';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { KnowledgeBaseService } from '../../../knowledge-base/services/knowledge-base.service';
import { FaqsService } from '../../../faqs/services/faqs.service';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { Article } from '../../../knowledge-base/models/article.model';
import { Faq } from '../../../faqs/models/faq.model';
import { messageFromError } from '../../../../shared/utils/http-error-message';
import { PageHeadingComponent } from '../../../../shared/components/page-heading/page-heading.component';
import { NoticeComponent } from '../../../../shared/components/notice/notice.component';
@Component({ selector: 'app-content-detail', standalone: true, imports: [DatePipe, RouterLink, PageHeadingComponent, NoticeComponent], templateUrl: './content-detail.component.html' })
export class ContentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private readonly knowledgeBaseApi = inject(KnowledgeBaseService);
  private readonly faqsApi = inject(FaqsService);
  auth = inject(AuthService);
  type = this.route.snapshot.data['type'] as 'faqs' | 'knowledge-base';
  id = Number(this.route.snapshot.paramMap.get('id'));
  heading = this.type === 'faqs' ? 'FAQ details' : 'Knowledge base article';
  backUrl = this.type === 'faqs' ? '/faqs' : '/knowledge-base';
  adminBase = this.type === 'faqs' ? '/admin/faqs' : '/admin/knowledge-base';
  item = signal<Article | Faq | null>(null);
  loading = signal(true);
  error = signal('');
  label(x: Article | Faq) { return this.type === 'faqs' ? (x as Faq).question : (x as Article).title; }
  body(x: Article | Faq) { return this.type === 'faqs' ? (x as Faq).answer : (x as Article).content; }
  ngOnInit() { const req = this.type === 'faqs' ? this.faqsApi.getById(this.id).pipe(map(x => x as Article | Faq)) : this.knowledgeBaseApi.getById(this.id).pipe(map(x => x as Article | Faq)); req.subscribe({ next: x => { this.item.set(x); this.loading.set(false); }, error: e => { this.error.set(messageFromError(e)); this.loading.set(false); } }); }
}
