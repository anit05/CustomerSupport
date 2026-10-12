import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { Article, CreateArticleRequest, UpdateArticleRequest } from '../models/article.model';
import { PageResult } from '../../../shared/models/paged-result.model';
import { ContentSearch } from '../../../shared/models/content-search.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class KnowledgeBaseService {
  private readonly api = inject(ApiClientService);
  search(query: ContentSearch = {}) { return this.api.get<PageResult<Article>>('api/knowledge-base', query); }
  getById(id: number) { return this.api.get<Article>(`api/knowledge-base/${id}`); }
  create(body: CreateArticleRequest) { return this.api.post<Article>('api/knowledge-base', body); }
  update(id: number, body: UpdateArticleRequest) { return this.api.put<Article>(`api/knowledge-base/${id}`, body); }
  deactivate(id: number) { return this.api.delete<null>(`api/knowledge-base/${id}`); }
}
