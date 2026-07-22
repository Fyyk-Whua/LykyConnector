using LykyConnector.Core.Client;

namespace LykyConnector.Core.Sync;

public interface IOrderSyncService
{
    Task<LykyResponse<object>> SyncBatchAsync(
        string outTradeNo, List<BatchItem> batches, CancellationToken ct = default);

    Task<LykyResponse<WaybillResult>> GetWaybillAsync(
        List<string> outTradeNos, CancellationToken ct = default);

    Task<LykyResponse<object>> CancelWaybillAsync(
        string outTradeNo, CancellationToken ct = default);

    Task<LykyResponse<object>> DeliverAsync(
        string outTradeNo, int type = 1, int? expressId = null,
        string? expressNo = null, CancellationToken ct = default);
}

public class OrderSyncService : IOrderSyncService
{
    private readonly ILykyApiClient _client;

    public OrderSyncService(ILykyApiClient client)
    {
        _client = client;
    }

    public async Task<LykyResponse<object>> SyncBatchAsync(
        string outTradeNo, List<BatchItem> batches, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "out_trade_no", outTradeNo }
        };
        for (int i = 0; i < batches.Count; i++)
        {
            bizParams[$"batch_no[{i}]"] = batches[i].BatchNo;
            bizParams[$"sku[{i}]"] = batches[i].Sku;
            bizParams[$"num[{i}]"] = batches[i].Num.ToString();
        }

        return await _client.PostAsync<object>(
            "v1/order-sync/batch", bizParams, ct);
    }

    public async Task<LykyResponse<WaybillResult>> GetWaybillAsync(
        List<string> outTradeNos, CancellationToken ct = default)
    {
        if (outTradeNos.Count > 50)
            throw new ArgumentException("最多获取50个订单的电子面单", nameof(outTradeNos));

        var bizParams = new Dictionary<string, string?>
        {
            { "out_trade_no", string.Join(",", outTradeNos) }
        };

        return await _client.PostAsync<WaybillResult>(
            "v1/order-sync/get-way-bill", bizParams, ct);
    }

    public async Task<LykyResponse<object>> CancelWaybillAsync(
        string outTradeNo, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "out_trade_no", outTradeNo }
        };

        return await _client.PostAsync<object>(
            "v1/order-sync/cancel-way-bill", bizParams, ct);
    }

    public async Task<LykyResponse<object>> DeliverAsync(
        string outTradeNo, int type = 1, int? expressId = null,
        string? expressNo = null, CancellationToken ct = default)
    {
        var bizParams = new Dictionary<string, string?>
        {
            { "out_trade_no", outTradeNo },
            { "type", type.ToString() }
        };
        if (expressId.HasValue)
            bizParams["express_id"] = expressId.Value.ToString();
        if (!string.IsNullOrEmpty(expressNo))
            bizParams["express_no"] = expressNo;

        return await _client.PostAsync<object>(
            "v1/order-sync/delivery", bizParams, ct);
    }
}

public class BatchItem
{
    public string Sku { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public int Num { get; set; }
}

public class WaybillResult
{
    public List<WaybillSuccess>? Success { get; set; }
    public List<WaybillFail>? Fail { get; set; }
}

public class WaybillSuccess
{
    public string? OutTradeNo { get; set; }
    public int Platform { get; set; }
    public string? ExpressNo { get; set; }
    public string? ExpressName { get; set; }
    public string? Content { get; set; }
}

public class WaybillFail
{
    public string? OutTradeNo { get; set; }
    public string? Msg { get; set; }
}

public static class ExpressCompany
{
    public const int SF = 1;
    public const int EMS = 3;
    public const int ZJS = 4;
    public const int YZKD = 5;
    public const int DBL = 6;
    public const int KYE = 12;
    public const int YTO = 18;
    public const int ZTO = 19;
    public const int STO = 24;
    public const int YD = 25;
    public const int ANE = 26;
    public const int JD = 96;
    public const int JT = 97;
    public const int YZBJ = 99;
    public const int OTHER = 999;

    public static readonly Dictionary<int, string> Names = new()
    {
        { 1, "顺丰速运" }, { 3, "EMS" }, { 4, "宅急送" },
        { 5, "中国邮政快递包裹" }, { 6, "德邦快递" }, { 12, "跨越速运" },
        { 18, "圆通速递" }, { 19, "中通快递" }, { 24, "申通快递" },
        { 25, "韵达速递" }, { 26, "安能快递" }, { 96, "京东快递" },
        { 97, "极兔速递" }, { 99, "中国邮政电商标快" }, { 999, "其他" }
    };
}
