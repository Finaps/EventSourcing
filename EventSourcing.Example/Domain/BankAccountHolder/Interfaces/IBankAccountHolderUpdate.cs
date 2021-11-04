namespace EventSourcing.Example.Domain.BankAccountHolder.Interfaces
{
    public interface IBankAccountHolderUpdate
    {
        public string Name { get; }
        public string EmailAddress { get; }
    }
}