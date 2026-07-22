using LykyConnector.Core.Client;

namespace LykyConnector.Core.Sync;

public interface IProductSyncService
{
    Task<LykyResponse<List<SyncFailItem>>> UpdateStockAsync(
        string sku, int stock, CancellationToken ct = default);

    Task<LykyResponse<object>> UpdateCargoAsync(
        string sku, string cargoName, string? cargoShelves = null, CancellationToken ct = default);

    Task<LykyResponse<object>> UpdateCostPriceAsync(
        string sku, decimal costPrice, CancellationToken ct = default);
}

public class ProductSyncService : IProductSyncService
{
    private readonly ILykyApiClient _client;

    public ProductSyncService(ILykyApiClient client)
    {
        _client = client;
    }

    public async Task<LykyResponse<List<SyncFailItem>>> UpdateStockAsync(
        string sku, int stock, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "sku", sku },
            { "stock", stock.ToString() }
        };

        return await _client.PostAsync<List<SyncFailItem>>(
            "v1/product-sync/update-stock-one", bizParams, ct);
    }

    public async Task<LykyResponse<object>> UpdateCargoAsync(
        string sku, string cargoName, string? cargoShelves = null, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "sku", sku },
            { "cargo_name", cargoName }
        };
        if (!string.IsNullOrEmpty(cargoShelves))
            bizParams["cargo_shelves"] = cargoShelves;

        return await _client.PostAsync<object>(
            "v1/product-sync/update-cargo-one", bizParams, ct);
    }

    public async Task<LykyResponse<object>> UpdateCostPriceAsync(
        string sku, decimal costPrice, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "sku", sku },
            { "cost_price", costPrice.ToString("F2") }
        };

        return await _client.PostAsync<object>(
            "v1/product-sync/update-cost-price-one", bizParams, ct);
    }
}
