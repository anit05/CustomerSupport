/** Common envelope returned by JSON API endpoints. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  traceId: string | null;
}
