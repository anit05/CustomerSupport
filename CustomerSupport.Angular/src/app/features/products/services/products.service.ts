import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { Product, CreateProductRequest, UpdateProductRequest } from '../models/product.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly api = inject(ApiClientService);
  getAll(all = false) { return this.api.get<Product[]>(`api/products${all ? '/all' : ''}`); }
  getById(id: number) { return this.api.get<Product>(`api/products/${id}`); }
  create(body: CreateProductRequest) { return this.api.post<Product>('api/products', body); }
  update(id: number, body: UpdateProductRequest) { return this.api.put<Product>(`api/products/${id}`, body); }
}
