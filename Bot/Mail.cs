using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bot;

public class Mail
{
    private const string Host = "https://privatix-temp-mail-v1.p.rapidapi.com";
    
    public static async Task<string> GetTokenConfirm(string apiKey, string email)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Host}/request/mail/id/{GetMd5Hash(email)}/");
        request.Headers.Add("X-RapidAPI-Host", "privatix-temp-mail-v1.p.rapidapi.com");
        request.Headers.Add("X-RapidAPI-Key", apiKey);
        var response = await client.SendAsync(request);
        return new Regex(@"token=([A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_=]+)").Match(await response.Content.ReadAsStringAsync()).Value;
    }
    
    private static string GetMd5Hash(string input)
    {
        var data = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var sBuilder = new StringBuilder();

        foreach (var t in data)
            sBuilder.Append(t.ToString("x2"));

        return sBuilder.ToString();
    }
}