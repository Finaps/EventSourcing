using System;
using System.Collections.Generic;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccountHolder.Commands;
using EventSourcing.Example.Domain.BankAccountHolder.Events;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder
{
    public class BankAccountHolder : Aggregate, IBankAccountHolderCreate
    {
        public string Name { get; }
        public string EmailAddress { get; }
        public List<Guid> BankAccountIds { get; } = new();

        public void Create(BankAccountHolderCreate request)
        {
            if (Version != 0) throw new InvalidOperationException("Bank account holder already exists");
            
            Add(Event.Create<BankAccountHolderCreatedEvent, IBankAccountHolderCreate>(this, request));
        }
        
        public void Update(BankAccountHolderUpdate request)
        {
            Add(Event.Create<BankAccountHolderUpdatedEvent, IBankAccountHolderUpdate>(this, request));
        }
        
        public void AddBankAccount(BankAccountHolderAddBankAccount request)
        {
            if (BankAccountIds.Contains(request.BankAccountId))
                throw new InvalidOperationException("Bank account is already added to this bank account holder");
            
            Add(Event.Create<BankAccountAddedEvent, IBankAccountHolderAddBankAccount>(this, request));
        }
        
        public void RemoveBankAccount(BankAccountHolderRemoveBankAccount request)
        {
            if (BankAccountIds.Contains(request.BankAccountId))
                throw new InvalidOperationException("Bank account is not linked to this bank account holder");
            
            Add(Event.Create<BankAccountRemovedEvent, IBankAccountHolderRemoveBankAccount>(this, request));
        }
        
        
        protected override void Apply<TEvent>(TEvent e)
        {
            Map(e);
            
            switch (e)
            {
                case BankAccountAddedEvent bankAccountAddedEvent:
                    BankAccountIds.Add(bankAccountAddedEvent.BankAccountId);
                    break;
                
                case BankAccountRemovedEvent bankAccountRemovedEvent:
                    BankAccountIds.Remove(bankAccountRemovedEvent.BankAccountId);
                    break;
                    
            }
        }
    }
}