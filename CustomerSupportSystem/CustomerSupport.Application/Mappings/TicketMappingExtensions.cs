using CustomerSupport.Application.DTOs.Tickets;
using CustomerSupport.Domain.Entities;
using CustomerSupport.Domain.Enums;

namespace CustomerSupport.Application.Mappings
{
  // Centralizes conversion between Ticket DTOs and Domain Entities.
  public static class TicketMappingExtensions
  {
    public static SupportTicket ToEntity(this CreateTicketRequestDTO request, int customerId)
    {
      // CustomerId, StatusId, and CreatedBy are controlled by the server.
      // The client is not allowed to choose these values.
      return new SupportTicket
      {
        CustomerId = customerId,
        ProductId = request.ProductId,
        CategoryId = request.CategoryId,
        PriorityId = request.PriorityId,

        // Every newly created Ticket starts in the Open state.
        StatusId = TicketStatusType.Open,

        Subject = request.Subject.Trim(),
        Description = request.Description.Trim(),

        // Record who created the Ticket for auditing.
        CreatedBy = customerId
      };
    }

    public static void MapFrom(this SupportTicket ticket, UpdateTicketRequestDTO request, int updatedBy)
    {
      // Only properties allowed by the update DTO are modified.
      ticket.Subject = request.Subject.Trim();
      ticket.Description = request.Description.Trim();

      // Maintain audit information whenever the Ticket changes.
      ticket.UpdatedAt = DateTime.UtcNow;
      ticket.UpdatedBy = updatedBy;
    }

    public static TicketSummaryResponseDTO ToSummaryDTO(this SupportTicket ticket)
    {
      // Summary DTOs contain only the fields required for Ticket lists.
      return new TicketSummaryResponseDTO
      {
        Id = ticket.Id,
        TicketNumber = ticket.TicketNumber,
        Subject = ticket.Subject,
        CustomerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim(),
        ProductName = ticket.Product.Name,
        CategoryName = ticket.Category?.Name,
        PriorityId = ticket.PriorityId,
        PriorityName = ticket.Priority.Name,
        StatusId = ticket.StatusId,
        StatusName = ticket.Status.Name,
        AssignedToUserName =
              ticket.AssignedToUser == null
                  ? null
                  : $"{ticket.AssignedToUser.FirstName} {ticket.AssignedToUser.LastName}".Trim(),
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt
      };
    }

    public static TicketDetailsResponseDTO ToDetailsDTO(this SupportTicket ticket, bool includeInternalComments)
    {
      // Detailed responses include comments and history.
      // Internal comments are included only for Support Executives and Administrators.
      return new TicketDetailsResponseDTO
      {
        Id = ticket.Id,
        TicketNumber = ticket.TicketNumber,
        Subject = ticket.Subject,
        Description = ticket.Description,
        CustomerId = ticket.CustomerId,
        CustomerName = $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim(),
        ProductId = ticket.ProductId,
        ProductName = ticket.Product.Name,
        CategoryId = ticket.CategoryId,
        CategoryName = ticket.Category?.Name,
        PriorityId = ticket.PriorityId,
        PriorityName = ticket.Priority.Name,
        StatusId = ticket.StatusId,
        StatusName = ticket.Status.Name,
        AssignedToUserId = ticket.AssignedToUserId,
        AssignedToUserName =
              ticket.AssignedToUser == null
                  ? null
                  : $"{ticket.AssignedToUser.FirstName} {ticket.AssignedToUser.LastName}".Trim(),
        ResolvedAt = ticket.ResolvedAt,
        ClosedAt = ticket.ClosedAt,
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,

        // Customers must never receive internal staff comments.
        Comments =
              ticket.Comments
                  .Where(comment => includeInternalComments || !comment.IsInternal)
                  .OrderBy(comment => comment.CreatedAt)
                  .Select(comment =>
                      new TicketCommentResponseDTO
                      {
                        Id = comment.Id,
                        UserId = comment.UserId,
                        UserName = $"{comment.User.FirstName} {comment.User.LastName}".Trim(),
                        Content = comment.Content,
                        IsInternal = comment.IsInternal,
                        CreatedAt = comment.CreatedAt
                      })
                  .ToList(),

        // History is returned in chronological order.
        History =
              ticket.History
                  .OrderBy(history => history.CreatedAt)
                  .Select(history =>
                      new TicketHistoryResponseDTO
                      {
                        Id = history.Id,
                        Action = history.Action,
                        FieldName = history.FieldName,
                        OldValue = history.OldValue,
                        NewValue = history.NewValue,
                        Remarks = history.Remarks,
                        ChangedByUserName = $"{history.ChangedByUser.FirstName} {history.ChangedByUser.LastName}".Trim(),
                        CreatedAt = history.CreatedAt
                      })
                  .ToList()
      };
    }

    public static TicketAssignmentResponseDTO ToResponseDTO(this TicketAssignment assignment)
    {
      // Convert assignment history Entity data into an API-safe DTO.
      return new TicketAssignmentResponseDTO
      {
        Id = assignment.Id,
        AssignedToUserId = assignment.AssignedToUserId,
        AssignedToUserName = $"{assignment.AssignedToUser.FirstName} {assignment.AssignedToUser.LastName}".Trim(),
        AssignedByUserId = assignment.AssignedByUserId,
        AssignedByUserName = $"{assignment.AssignedByUser.FirstName} {assignment.AssignedByUser.LastName}".Trim(),
        AssignedAt = assignment.AssignedAt,
        UnassignedAt = assignment.UnassignedAt
      };
    }
  }
}
