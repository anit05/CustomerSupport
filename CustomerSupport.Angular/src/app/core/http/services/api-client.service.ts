import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { API_BASE_URL } from '../../config/api.config';
import { ApiResponse } from '../models/api-response.model';
@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly http = inject(HttpClient);
  private url(path: string): string { return `${API_BASE_URL}/${path.replace(/^\//, '')}`; }
  private params(query?: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== null && value !== undefined && value !== '')
        params = params.set(key, String(value));
    }
    return params;
  }
  get<T>(path: string, query?: object): Observable<T> {
    return this.http.get<ApiResponse<T>>(this.url(path), { params: this.params(query) }).pipe(map(result => this.unwrap(result)));
  }
  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<ApiResponse<T>>(this.url(path), body).pipe(map(result => this.unwrap(result)));
  }
  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<ApiResponse<T>>(this.url(path), body).pipe(map(result => this.unwrap(result)));
  }
  delete<T>(path: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(this.url(path)).pipe(map(result => this.unwrap(result)));
  }
  download(path: string): Observable<HttpResponse<Blob>> {
    return this.http.get(this.url(path), { responseType: 'blob', observe: 'response' });
  }
  private unwrap<T>(result: ApiResponse<T>): T {
    if (!result.success)
      throw new Error(result.message || result.errors?.join(', ') || 'API operation failed.');
    return result.data;
  }
}
