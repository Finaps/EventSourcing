using System;

namespace EventSourcing.Example.Domain.Shared
{
    public static class Constants
    {
        public static TimeSpan BasketExpires = TimeSpan.FromHours(1);
        public static TimeSpan ProductReservationExpires = TimeSpan.FromHours(1);
    }
}