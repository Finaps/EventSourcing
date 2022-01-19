using System.Collections.Generic;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Products;

public record ProductSnapshot(string Name, int Quantity, List<Reservation> Reservations) : Snapshot;