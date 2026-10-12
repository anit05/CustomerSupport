export interface MasterValue {
  id: number;
  name: string;
  description?: string | null;
  sortOrder: number;
  isActive: boolean;
}
