using Finaps.EventSourcing.Core;

namespace Finaps.EventSourcing.Example.Domain.Products;

// It makes sense to snapshot the Product aggregate once in a while since it can contain many irrelevant events that
// do not influence the current state (e.g. ProductReservedEvent)
public record ProductSnapshot(string Name, int Quantity, List<Reservation> Reservations) : Snapshot<Product>;