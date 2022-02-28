namespace EventSourcing.Core;

public interface IProjectionUpdateService
{
  Task UpdateProjectionsAsync(Type aggregateType, Type projectionType, CancellationToken cancellationToken = default);
  Task UpdateProjectionsAsync(Type aggregateType, CancellationToken cancellationToken = default);
  Task UpdateProjectionsAsync(CancellationToken cancellationToken = default);

  async Task UpdateProjectionsAsync<TAggregate>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate =>
    await UpdateProjectionsAsync(typeof(TAggregate), cancellationToken);
  
  async Task UpdateProjectionsAsync<TAggregate, TProjection>(CancellationToken cancellationToken = default)
    where TAggregate : Aggregate where TProjection : Projection => 
    await UpdateProjectionsAsync(typeof(TAggregate), cancellationToken);
}