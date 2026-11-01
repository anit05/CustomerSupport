export interface Article {
  id: number;
  title: string;
  content: string;
  productId?: number | null;
  productName?: string | null;
  isPublished: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}
export interface CreateArticleRequest {
  title: string;
  content: string;
  productId: number | null;
  isPublished: boolean;
}
export interface UpdateArticleRequest extends CreateArticleRequest {
  isActive: boolean;
}
