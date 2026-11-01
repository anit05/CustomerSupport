export interface TicketSummary {
  id: number;
  ticketNumber: string;
  subject: string;
  customerName: string;
  productName: string;
  categoryName?: string | null;
  priorityId: number;
  priorityName: string;
  statusId: number;
  statusName: string;
  assignedToUserName?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}
export interface TicketComment {
  id: number;
  userId: number;
  userName: string;
  content: string;
  isInternal: boolean;
  createdAt: string;
}
export interface TicketHistory {
  id: number;
  action: string;
  fieldName?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  remarks?: string | null;
  changedByUserName: string;
  createdAt: string;
}
export interface TicketAttachment {
  id: number;
  originalFileName: string;
  contentType: string;
  fileSizeInBytes: number;
  uploadedByUserId: number;
  uploadedByUserName: string;
  createdAt: string;
}
export interface TicketAssignment {
  id: number;
  assignedToUserId: number;
  assignedToUserName: string;
  assignedByUserId: number;
  assignedByUserName: string;
  assignedAt: string;
  unassignedAt?: string | null;
}
export interface TicketDetails extends TicketSummary {
  description: string;
  customerId: number;
  productId: number;
  categoryId?: number | null;
  assignedToUserId?: number | null;
  comments: TicketComment[];
  history: TicketHistory[];
  attachments: TicketAttachment[];
  resolvedAt?: string | null;
  closedAt?: string | null;
}
export interface TicketFilters {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  productId?: number | null;
  categoryId?: number | null;
  priorityId?: number | null;
  statusId?: number | null;
  customerId?: number | null;
  assignedToUserId?: number | null;
  sortBy?: string;
  sortDirection?: string;
}
export interface CreateTicketRequest {
  productId: number;
  categoryId: number | null;
  priorityId: number;
  subject: string;
  description: string;
}
export interface AssignTicketRequest {
  supportExecutiveId: number;
}
export interface ChangeTicketStatusRequest {
  statusId: number;
  remarks: string;
}
export interface AddTicketCommentRequest {
  content: string;
  isInternal: boolean;
}
