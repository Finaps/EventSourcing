using System.Collections.Generic;
using EventSourcing.Core;
using EventSourcing.Core.Records;

namespace EventSourcing.Example.Domain.Products;

public record ProductSnapshot(string Name, int Quantity, List<Reservation> Reservations) : Snapshot;