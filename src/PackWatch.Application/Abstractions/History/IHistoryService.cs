using PackWatch.Domain.Entities;

namespace PackWatch.Application.Abstractions.History;

public interface IHistoryService
{
    Task<IReadOnlyList<OrderRecord>> SearchAsync(
        OrderHistoryQuery query,
        CancellationToken cancellationToken);
}
