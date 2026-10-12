import { HttpErrorResponse } from '@angular/common/http';
/** Maps API failures into safe, human-readable messages for the UI. */
export function messageFromError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as {
      errors?: string[];
      message?: string;
      title?: string;
    } | null;
    if (Array.isArray(body?.errors) && body.errors.length)
      return body.errors.join(' • ');
    if (error.status === 0)
      return 'Cannot connect to the Web API. Check the API URL, HTTPS certificate, and CORS.';
    return body?.message || body?.title || `Request failed (${error.status}).`;
  }
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
}
