export interface Product {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}
export interface CreateProductRequest {
  name: string;
  description?: string | null;
}
export interface UpdateProductRequest extends CreateProductRequest {
  isActive: boolean;
}
