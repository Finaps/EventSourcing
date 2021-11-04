namespace EventSourcing.Example.Domain.BankAccountHolder.Interfaces
{
    public interface IBankAccountHolderCreate
    {
        public string Name { get; }
        public string EmailAddress { get; }
    }
}