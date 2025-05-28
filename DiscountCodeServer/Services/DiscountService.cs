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
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, (int)request.Length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }
            while (_codeCollection.Exists(x => x.Code == code)); // avoid duplicates

            _codeCollection.Insert(new DiscountCode { Code = code, Used = false });
            response.Codes.Add(code);
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
