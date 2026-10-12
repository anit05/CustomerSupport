/** Bootstrap 5 utility classes used for visual status labels. */
export function ticketBadge(status: string): string {
  switch (status.toLowerCase()) {
    case 'open': return 'text-bg-info';
    case 'assigned': return 'text-bg-primary';
    case 'inprogress': return 'text-bg-warning';
    case 'resolved': return 'text-bg-success';
    case 'closed': return 'text-bg-secondary';
    default: return 'text-bg-light';
  }
}
export function priorityBadge(value: string): string {
  const priority = value.toLowerCase();
  if (priority === 'critical' || priority === 'high')
    return 'text-bg-danger';
  return priority === 'medium' ? 'text-bg-warning' : 'text-bg-light';
}
