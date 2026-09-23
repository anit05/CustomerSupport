namespace CustomerSupport.Application.DTOs.Tickets
{
  public sealed class TicketAssignmentResponseDTO
  {
    // Unique identifier of the TicketAssignment record.
    public int Id { get; set; }

    // User who received responsibility for the Ticket.
    public int AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;

    // Administrator who performed the assignment.
    public int AssignedByUserId { get; set; }
    public string AssignedByUserName { get; set; } = string.Empty;

    // Assignment lifecycle timestamps.
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
  }
}
