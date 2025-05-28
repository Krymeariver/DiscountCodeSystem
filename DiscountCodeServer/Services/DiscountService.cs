using DiscountCodeSystem.Protos;
using Grpc.Core;
using System.Collections.Concurrent;

public class DiscountServiceImpl : DiscountService.DiscountServiceBase
{
    private readonly ConcurrentDictionary<string, bool> _codes = new();
    private readonly object _lock = new();
    private readonly string _persistencePath = "codes.json";

    public DiscountServiceImpl()
    {
        LoadCodes();
    }

    public override Task<GenerateResponse> GenerateCodes(GenerateRequest request, ServerCallContext context)
    {
        var response = new GenerateResponse { Success = true };

        if (request.Count > 2000 || request.Length < 7 || request.Length > 8)
        {
            response.Success = false;
            return Task.FromResult(response);
        }

        lock (_lock)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            for (int i = 0; i < request.Count; i++)
            {
                string code;
                do
                {
                    code = new string(Enumerable.Repeat(chars, (int)request.Length)
                        .Select(s => s[random.Next(s.Length)]).ToArray());
                }
                while (!_codes.TryAdd(code, false)); 

                response.Codes.Add(code);
            }

            SaveCodes();
        }

        return Task.FromResult(response);
    }

    public override Task<UseCodeResponse> UseCode(UseCodeRequest request, ServerCallContext context)
    {
        var response = new UseCodeResponse();

        if (!_codes.TryGetValue(request.Code, out var used))
        {
            response.Result = UseCodeResponse.Types.Status.NotFound;
        }
        else if (used)
        {
            response.Result = UseCodeResponse.Types.Status.AlreadyUsed;
        }
        else
        {
            _codes[request.Code] = true;
            response.Result = UseCodeResponse.Types.Status.Success;
            SaveCodes();
        }

        return Task.FromResult(response);
    }

    public override Task<CountUnusedResponse> CountCodesUnused(CountUnusedRequest request, ServerCallContext context)
    {
        var count = _codes.Count(pair => !pair.Value); 
        return Task.FromResult(new CountUnusedResponse { UnusedCount = (uint)count });
    }


    private void LoadCodes()
    {
        lock (_lock)
        {
            if (!File.Exists(_persistencePath)) return;

            var json = File.ReadAllText(_persistencePath);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            if (loaded != null)
            {
                foreach (var pair in loaded)
                    _codes.TryAdd(pair.Key, pair.Value);
            }
        }
    }

    private void SaveCodes()
    {
        lock (_lock)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_codes);
            File.WriteAllText(_persistencePath, json);
        }
    }

}
