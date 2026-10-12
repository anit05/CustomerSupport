export interface Faq {
  id: number;
  question: string;
  answer: string;
  productId?: number | null;
  productName?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
}
export interface CreateFaqRequest {
  question: string;
  answer: string;
  productId: number | null;
}
export interface UpdateFaqRequest extends CreateFaqRequest {
  isActive: boolean;
}
