using DiscountCodeSystem.Protos;
using Grpc.Net.Client;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class DiscountServiceTests
{
    private readonly DiscountService.DiscountServiceClient _client;

    public DiscountServiceTests()
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5164"); // Match server port
        _client = new DiscountService.DiscountServiceClient(channel);
    }

    [Fact]
    public async Task SingleClient_CanGenerateAndUseCodes()
    {
        var generateResponse = await _client.GenerateCodesAsync(new GenerateRequest { Count = 5, Length = 7 });

        Assert.True(generateResponse.Success);
        Assert.Equal(5, generateResponse.Codes.Count);

        var firstCode = generateResponse.Codes.First();

        var useResponse = await _client.UseCodeAsync(new UseCodeRequest { Code = firstCode });

        Assert.Equal(UseCodeResponse.Types.Status.Success, useResponse.Result);

        // Try using it again
        var reuseResponse = await _client.UseCodeAsync(new UseCodeRequest { Code = firstCode });
        Assert.Equal(UseCodeResponse.Types.Status.AlreadyUsed, reuseResponse.Result);
    }

    [Fact]
    public async Task MultipleClients_CanGenerateCodesConcurrently()
    {
        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var response = await _client.GenerateCodesAsync(new GenerateRequest { Count = 100, Length = 8 });
            Assert.True(response.Success);
            Assert.Equal(100, response.Codes.Count);
        });

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task MultipleClients_CanUseCodesInParallel()
    {
        // Generate once
        var generate = await _client.GenerateCodesAsync(new GenerateRequest { Count = 100, Length = 7 });
        var codes = generate.Codes.ToList();

        var tasks = codes.Select(code => Task.Run(async () =>
        {
            var result = await _client.UseCodeAsync(new UseCodeRequest { Code = code });
            Assert.True(result.Result == UseCodeResponse.Types.Status.Success || result.Result == UseCodeResponse.Types.Status.AlreadyUsed);
        }));

        await Task.WhenAll(tasks);
    }
}
