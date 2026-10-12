import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { Faq, CreateFaqRequest, UpdateFaqRequest } from '../models/faq.model';
import { PageResult } from '../../../shared/models/paged-result.model';
import { ContentSearch } from '../../../shared/models/content-search.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class FaqsService {
  private readonly api = inject(ApiClientService);
  search(query: ContentSearch = {}) { return this.api.get<PageResult<Faq>>('api/faqs', query); }
  getById(id: number) { return this.api.get<Faq>(`api/faqs/${id}`); }
  create(body: CreateFaqRequest) { return this.api.post<Faq>('api/faqs', body); }
  update(id: number, body: UpdateFaqRequest) { return this.api.put<Faq>(`api/faqs/${id}`, body); }
  deactivate(id: number) { return this.api.delete<null>(`api/faqs/${id}`); }
}
