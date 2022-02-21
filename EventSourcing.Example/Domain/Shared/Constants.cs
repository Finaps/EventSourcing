namespace EventSourcing.Example.Domain.Shared;

public static class Constants
{
    // Will be used to set the value for the ExpirationTime property in the BasketCreatedEvent
    public static TimeSpan BasketExpires = TimeSpan.FromHours(1);
    // Will be used to set the value for the HeldFor property in the ProductReservedEvent
    public static TimeSpan ProductReservationExpires = TimeSpan.FromHours(1);
    
    // These two values should probably be the same value since in general we need to reserve a product for as long as
    // the time it takes for a basket to expire
}