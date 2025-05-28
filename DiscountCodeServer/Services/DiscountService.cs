using DiscountCodeServer;
using DiscountCodeSystem.Protos;
using Grpc.Core;
using LiteDB;

public class DiscountServiceImpl : DiscountService.DiscountServiceBase
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<DiscountCode> _codeCollection;


    public DiscountServiceImpl()
    {
        Console.WriteLine("Initializing DiscountServiceImpl...");
        _db = new LiteDatabase("codes.db");
        _codeCollection = _db.GetCollection<DiscountCode>("codes");
        _codeCollection.EnsureIndex(x => x.Code);

    }

    public override Task<GenerateResponse> GenerateCodes(GenerateRequest request, ServerCallContext context)
    {
        var response = new GenerateResponse { Success = true };

        if (request.Count > 2000 || request.Length < 7 || request.Length > 8)
        {
            response.Success = false;
            return Task.FromResult(response);
        }

        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        for (int i = 0; i < request.Count; i++)
        {
            bool added = false;
            int attempts = 0;
            const int maxAttempts = 1000;

            while (!added)
            {
                if (++attempts > maxAttempts)
                    throw new Exception("Failed to generate a unique code after many attempts. Code space may be saturated.");

                var code = new string(Enumerable.Repeat(chars, (int)request.Length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                try
                {
                    _codeCollection.Insert(new DiscountCode
                    {
                        Code = code,
                        Used = false,
                        CreatedAt = DateTime.UtcNow,
                        UsedAt = null
                    });

                    response.Codes.Add(code);
                    added = true;
                }
                catch (LiteDB.LiteException ex) when (ex.Message.Contains("duplicate key")) // Cannot insert duplicate key in unique index '_id'. The duplicate value is '"code"'.
                {
                    // Collision — try again
                }
            }
        }

        return Task.FromResult(response);
    }

    public override Task<UseCodeResponse> UseCode(UseCodeRequest request, ServerCallContext context)
    {
        var response = new UseCodeResponse();

        var entry = _codeCollection.FindById(request.Code);

        if (entry == null)
        {
            response.Result = UseCodeResponse.Types.Status.NotFound;
        }
        else if (entry.Used)
        {
            response.Result = UseCodeResponse.Types.Status.AlreadyUsed;
        }
        else
        {
            entry.Used = true;
            entry.UsedAt = DateTime.UtcNow;
            _codeCollection.Update(entry);
            response.Result = UseCodeResponse.Types.Status.Success;
        }

        return Task.FromResult(response);
    }


    public override Task<CountUnusedResponse> CountCodesUnused(CountUnusedRequest request, ServerCallContext context)
    {
        var count = _codeCollection.Count(x => x.Used == false);
        return Task.FromResult(new CountUnusedResponse { UnusedCount = (uint)count });
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
