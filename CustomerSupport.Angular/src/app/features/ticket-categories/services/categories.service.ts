import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../models/category.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly api = inject(ApiClientService);
  getAll(all = false) { return this.api.get<Category[]>(`api/ticket-categories${all ? '/all' : ''}`); }
  getById(id: number) { return this.api.get<Category>(`api/ticket-categories/${id}`); }
  create(body: CreateCategoryRequest) { return this.api.post<Category>('api/ticket-categories', body); }
  update(id: number, body: UpdateCategoryRequest) { return this.api.put<Category>(`api/ticket-categories/${id}`, body); }
}
