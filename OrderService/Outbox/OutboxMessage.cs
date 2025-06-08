namespace OrderService.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Payload { get; set; } = null!;
        public bool Published { get; set; }
        public DateTime OccurredOn { get; set; }
    }
}