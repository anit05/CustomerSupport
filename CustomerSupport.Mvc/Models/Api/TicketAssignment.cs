namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketAssignment
    {
        public int Id { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; } = string.Empty;
        public int AssignedByUserId { get; set; }
        public string AssignedByUserName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public DateTime? UnassignedAt { get; set; }
    }
}
