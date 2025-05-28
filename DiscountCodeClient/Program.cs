using DiscountCodeSystem.Protos; 
using Grpc.Net.Client;

var channel = GrpcChannel.ForAddress("http://localhost:5164"); 

var client = new DiscountService.DiscountServiceClient(channel);

while (true)
{
    Console.WriteLine("\nChoose option:\n1. Generate Codes\n2. Use Code\n3. Count unused codes \n4. Exit");
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            Console.Write("How many codes? Max 2000");
            var count = ushort.Parse(Console.ReadLine()!);

            Console.Write("Code length (7 or 8): ");
            var length = byte.Parse(Console.ReadLine()!);

            var generateResponse = await client.GenerateCodesAsync(new GenerateRequest
            {
                Count = count,
                Length = length
            });

            if (!generateResponse.Success)
            {
                Console.WriteLine("Invalid request.");
            }
            else
            {
                Console.WriteLine("Generated codes:");
                foreach (var code in generateResponse.Codes)
                    Console.WriteLine($" - {code}");
            }
            break;

        case "2":
            Console.Write("Enter code to use: ");
            var codeToUse = Console.ReadLine();

            var useResponse = await client.UseCodeAsync(new UseCodeRequest
            {
                Code = codeToUse
            });

            switch (useResponse.Result)
            {
                case UseCodeResponse.Types.Status.Success:
                    Console.WriteLine("Code used successfully!");
                    break;
                case UseCodeResponse.Types.Status.AlreadyUsed:
                    Console.WriteLine("Code was already used.");
                    break;
                case UseCodeResponse.Types.Status.NotFound:
                    Console.WriteLine("Code not found.");
                    break;
            }
            break;

        case "3":
            var unusedResponse = await client.CountCodesUnusedAsync(new CountUnusedRequest());
            Console.WriteLine($"There are {unusedResponse.UnusedCount} unused codes.");
            break;
        case "4":
            return;

        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}
