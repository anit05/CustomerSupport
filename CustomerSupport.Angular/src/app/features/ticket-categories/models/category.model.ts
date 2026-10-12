export interface Category {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
}
export interface CreateCategoryRequest {
  name: string;
  description?: string | null;
}
export interface UpdateCategoryRequest extends CreateCategoryRequest {
  isActive: boolean;
}
