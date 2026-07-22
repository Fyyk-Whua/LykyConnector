using LykyConnector.Core.Client;
using LykyConnector.Core.Sync;

namespace LykyConnector.Core.Tests.Sync;

public class SyncServicesTests
{
    [Fact]
    public async Task UpdateStock_SendsCorrectParams()
    {
        var mockClient = new MockLykyClient();
        var service = new ProductSyncService(mockClient);

        var result = await service.UpdateStockAsync("SKU001", 100);

        Assert.NotNull(mockClient.LastPath);
        Assert.Equal("v1/product-sync/update-stock-one", mockClient.LastPath);
        Assert.Equal("SKU001", mockClient.LastBizParams!["sku"]);
        Assert.Equal("100", mockClient.LastBizParams!["stock"]);
    }

    [Fact]
    public async Task UpdateCargo_SendsCorrectParams()
    {
        var mockClient = new MockLykyClient();
        var service = new ProductSyncService(mockClient);

        await service.UpdateCargoAsync("SKU001", "A-1", "货架B");

        Assert.Equal("v1/product-sync/update-cargo-one", mockClient.LastPath);
        Assert.Equal("A-1", mockClient.LastBizParams!["cargo_name"]);
        Assert.Equal("货架B", mockClient.LastBizParams!["cargo_shelves"]);
    }

    [Fact]
    public async Task UpdateCostPrice_SendsCorrectParams()
    {
        var mockClient = new MockLykyClient();
        var service = new ProductSyncService(mockClient);

        await service.UpdateCostPriceAsync("SKU001", 99.50m);

        Assert.Equal("v1/product-sync/update-cost-price-one", mockClient.LastPath);
        Assert.Equal("99.50", mockClient.LastBizParams!["cost_price"]);
    }

    [Fact]
    public async Task GetWaybill_ThrowsWhenOverLimit()
    {
        var mockClient = new MockLykyClient();
        var service = new OrderSyncService(mockClient);

        var manyOrders = Enumerable.Range(1, 51).Select(i => $"OD{i}").ToList();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetWaybillAsync(manyOrders));
    }

    [Fact]
    public async Task GetWaybill_Allows50()
    {
        var mockClient = new MockLykyClient();
        var service = new OrderSyncService(mockClient);

        var fiftyOrders = Enumerable.Range(1, 50).Select(i => $"OD{i}").ToList();

        await service.GetWaybillAsync(fiftyOrders);

        Assert.Equal("v1/order-sync/get-way-bill", mockClient.LastPath);
        Assert.Equal(50, mockClient.LastBizParams!["out_trade_no"]!.Split(',').Length);
    }

    [Fact]
    public async Task Deliver_SendsCorrectParams()
    {
        var mockClient = new MockLykyClient();
        var service = new OrderSyncService(mockClient);

        await service.DeliverAsync("OD001", expressId: ExpressCompany.ZTO, expressNo: "ZT123456789");

        Assert.Equal("v1/order-sync/delivery", mockClient.LastPath);
        Assert.Equal("OD001", mockClient.LastBizParams!["out_trade_no"]);
        Assert.Equal("19", mockClient.LastBizParams!["express_id"]);
        Assert.Equal("ZT123456789", mockClient.LastBizParams!["express_no"]);
    }
}

public class MockLykyClient : ILykyApiClient
{
    public string? LastPath { get; private set; }
    public Dictionary<string, string?>? LastBizParams { get; private set; }

    public Task<LykyResponse<T>> PostAsync<T>(
        string path,
        Dictionary<string, string?>? bizParams = null,
        CancellationToken ct = default)
    {
        LastPath = path;
        LastBizParams = bizParams;
        return Task.FromResult(new LykyResponse<T> { Code = 0, Name = "success" });
    }
}
