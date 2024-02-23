using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Bot;
using Newtonsoft.Json.Linq;

var mailToken = await File.ReadAllTextAsync("mail_token.txt");
var names = await File.ReadAllLinesAsync("first-names.txt");

//await RegisterAccounts(1);
await LikePost("2506059");
Console.Read();

return;

string TakeRandomName() => names?[new Random().Next(0, names.Length)]!;

string GenerateRandomString(int length)
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var stringBuilder = new StringBuilder();
    var random = new Random();

    for (var i = 0; i < length; i++)
        stringBuilder.Append(chars[random.Next(chars.Length)]);

    return stringBuilder.ToString();
}

#region Примеры использования

async Task RegisterAccounts(int countIterations)
{
    for (var i = 0; i < countIterations; i++)
    {
        var name = TakeRandomName();
        var email = $"{name}{new Random().Next(1_000, 999_999)}@maxric.com".ToLower();
        var pass = GenerateRandomString(new Random().Next(10, 20));

        var client = new Client(mailToken!);

        if (!await client.Register(name, email, pass)) 
            continue;
        
        await File.AppendAllTextAsync("accounts.txt", $"\n{email};{pass}");
        Console.WriteLine($"{email}: Registered");
    }
}

async Task LikePost(string postId)
{
    var accounts = await File.ReadAllLinesAsync("accounts.txt");
    
    foreach (var getAccount in accounts.Take(5))
    {
        _ = Task.Run(async () =>
        {
            var account = getAccount;
            var data = account.Split(";");
            
            try
            {
                var client = new Client(mailToken!);

                if (!await client.Login(data[0], data[1]))
                {
                    Console.WriteLine($"{data[0]}: Cant login account");
                    return;
                }

                var count = await client.Like(postId);

                Console.WriteLine($"{data[0]}: Count ({count})");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{data[0]}: {ex.Message}");
            }
        });
    }
}

#endregion