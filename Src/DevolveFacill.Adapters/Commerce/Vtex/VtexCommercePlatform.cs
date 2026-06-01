using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using DevolveFacill.Core.Ports;
using System.Text.Json;
using System.Linq;

namespace DevolveFacill.Adapters.Commerce.Vtex;

// Assinamos o contrato oficial que o seu Use Case exige
public class VtexCommercePlatform : ICommercePlatform
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    // O .NET injeta automaticamente o HttpClient e o leitor do .env aqui
    public VtexCommercePlatform(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    // ESTA É A FUNÇÃO QUE ACABAMOS DE DESCOBRIR NO SEU USE CASE!
    public async Task<IReadOnlyList<OrderSummary>> GetOrdersForCustomerAsync(string customerExternalId, CancellationToken ct)
    {
        // 1. Buscamos os dados de acesso da La Moda que você vai salvar no seu .env
        var appKey = _configuration["VTEX_APP_KEY"];
        var appToken = _configuration["VTEX_APP_TOKEN"];
        var baseUrl = _configuration["VTEX_API_URL"] ?? "https://lamoda.vtexcommercestable.com.br";

        // 2. Montamos a URL do OMS da VTEX usando o CPF do cliente (customerExternalId)
        var listUrl = $"{baseUrl}/api/oms/pvt/orders?q={customerExternalId}"; // q= costuma funcionar melhor para buscas genéricas como CPF

        // 3. Preparamos a requisição HTTP do tipo GET
        var listRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);

        // 4. Injetamos as chaves de segurança nos cabeçalhos (Headers) da requisição
        listRequest.Headers.Add("X-VTEX-API-AppKey", appKey);
        listRequest.Headers.Add("X-VTEX-API-AppToken", appToken);

        // 5. O HttpClient faz o disparo real através da internet para a VTEX
        var listResponse = await _httpClient.SendAsync(listRequest, ct);

        // 6. Se a VTEX der qualquer erro de comunicação (ex: 401 não autorizado), o sistema para aqui e avisa
        listResponse.EnsureSuccessStatusCode();

        // 7. Lemos o JSON da lista (que vem com items = null, como você percebeu)
        var vtexData = await listResponse.Content.ReadFromJsonAsync<VtexSearchOrdersResponse>(cancellationToken: ct);

        var list = new List<OrderSummary>();

        if (vtexData?.List != null && vtexData.List.Any())
        {
            // 8. Para cada pedido na lista, buscamos os detalhes completos em paralelo (para ser rápido)
            var detailTasks = vtexData.List.Select(async summaryOrder =>
            {
                var detailUrl = $"{baseUrl}/api/oms/pvt/orders/{summaryOrder.OrderId}";
                var detailRequest = new HttpRequestMessage(HttpMethod.Get, detailUrl);
                detailRequest.Headers.Add("X-VTEX-API-AppKey", appKey);
                detailRequest.Headers.Add("X-VTEX-API-AppToken", appToken);
                
                var detailResponse = await _httpClient.SendAsync(detailRequest, ct);
                if (!detailResponse.IsSuccessStatusCode) return null; // Pula se der erro em um pedido específico
                
                var detailedOrder = await detailResponse.Content.ReadFromJsonAsync<VtexOrder>(cancellationToken: ct);
                if (detailedOrder == null) return null;

                // Retornamos os dados preenchendo todos os parâmetros do Record do Core
                return new OrderSummary(
                    ExternalOrderId: detailedOrder.OrderId,
                    Platform: "VTEX",
                    CustomerExternalId: customerExternalId,
                    Items: detailedOrder.Items?.Select(i => new OrderItemSummary(
                        Sku: i.Id,
                        Name: i.Name,
                        Quantity: i.Quantity,
                        UnitPrice: i.Price / 100m, // Ajusta centavos para decimal real
                        ImageUrl: i.ImageUrl
                    )).ToList() ?? new List<OrderItemSummary>(),
                    Total: detailedOrder.Value / 100m, // ATENÇÃO: Na API de detalhes da VTEX, o total chama-se "value"
                    Currency: "BRL",
                    OrderedAt: detailedOrder.CreationDate,
                    EligibleForReturn: true, // Campo exigido pela modelagem do Core
                    ReturnDeadlineDays: 30, // Campo exigido pela modelagem do Core
                    AwaitingDelivery: detailedOrder.Status == "ready-for-handling" || detailedOrder.Status == "handling",
                    RawPayload: JsonSerializer.SerializeToDocument(detailedOrder)
                );
            });
            
            // Aguarda o processamento concorrente de todos os pedidos terminarem
            var detailedSummaries = await Task.WhenAll(detailTasks);
            list.AddRange(detailedSummaries.OfType<OrderSummary>());
        }

        return list;
    }

    // --- MÉTODOS FALTANTES DA INTERFACE ICommercePlatform ---

    public Task<OrderSummary> GetOrderAsync(string orderId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<VoucherResult> CreateStoreCreditAsync(StoreCreditRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<CustomerInfo?> FindCustomerByDocumentAsync(string cpf, CancellationToken ct)
    {
        // Retorna null em vez de lançar exceção. Assim, se a senha for digitada errada,
        // o sistema apenas dirá "CPF não encontrado" sem quebrar a API com Erro 500.
        return Task.FromResult<CustomerInfo?>(null);
    }
}

// Modelos de DTO internos para mapear o JSON de resposta da VTEX
public class VtexSearchOrdersResponse
{
    public List<VtexOrder>? List { get; set; }
}

public class VtexOrder
{
    public string OrderId { get; set; } = string.Empty;
    public decimal TotalValue { get; set; } // Propriedade que vem no Endpoint de Lista
    public int Value { get; set; } // Propriedade (em centavos) que vem no Endpoint de Detalhe
    public DateTimeOffset CreationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<VtexOrderItem>? Items { get; set; }
}

public class VtexOrderItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}