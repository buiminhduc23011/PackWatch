using PackWatch.Domain.Entities;

namespace PackWatch.Persistence.Services;

internal interface IOrderHistoryWriter
{
    Task AppendAsync(OrderRecord record, CancellationToken cancellationToken);
}
