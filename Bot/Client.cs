using Newtonsoft.Json.Linq;

namespace Bot;

public class Client(string apiTokenMail)
{
    private const string Host = "https://api.dtf.ru";
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";
    private string? _token;
    
    public bool Initialized { private set; get; }

    #region Функции либы

    public async Task<bool> Login(string email, string password)
    {
        var response = await ApiLogin(email, password);
        if (!CheckSuccessfulLogin(response))
            return false;
        
        _token = JObject.Parse(response)["data"]!["accessToken"]!.ToString();
        return Initialized = true;
    }

    public async Task<int> Like(string postId)
    {
        if (!Initialized)
            throw new Exception("client not initialized");
        
        var likeResponse = await ApiLike(postId, false);

        if (CheckSuccessfulLike(likeResponse))
            return int.Parse(JObject.Parse(likeResponse)["result"]!["counterLikes"]!.ToString());
        else
            throw new Exception(JObject.Parse(likeResponse)["message"]!.ToString());
    }

    public async Task<int> Dislike(string postId)
    {
        if (!Initialized)
            throw new Exception("client not initialized");
        
        var likeResponse = await ApiLike(postId, true);

        if (CheckSuccessfulLike(likeResponse))
            return int.Parse(JObject.Parse(likeResponse)["result"]!["counterLikes"]!.ToString());
        else
            throw new Exception(JObject.Parse(likeResponse)["message"]!.ToString());
    }

    public async Task<bool> Register(string name, string email, string password)
    {
        var regResponse = await ApiRegister(name, email, password);

        if (!CheckSuccessfulReg(regResponse))
            throw new Exception(JObject.Parse(regResponse)["message"]!.ToString());

        await Task.Delay(5 * 1_000);
        var countTryGetToken = 0;
    
        while (true)
        {
            _token = await Mail.GetTokenConfirm(apiTokenMail, email);
            if (string.IsNullOrEmpty(_token))
                await Task.Delay(1_000);
            else
                break;

            if (countTryGetToken++ > 30)
                throw new Exception("cant wait message");
        }

        var confResponse = await ApiConfirmEmail(_token.Remove(0, "token=".Length));

        if (!CheckSuccessfulConfirmed(confResponse))
            throw new Exception(JObject.Parse(confResponse)["message"]!.ToString());
            
        return true;
    }
    
    private static bool CheckSuccessfulLogin(string response)
    {
        try
        {
            return JObject.Parse(response)["message"]!.ToString().Equals("logined");
        }
        catch
        {
            return false;
        }
    }
    
    private static bool CheckSuccessfulLike(string response)
    {
        try
        {
            return JObject.Parse(response)["message"]!.ToString().Equals(string.Empty);
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSuccessfulReg(string response)
    {
        try
        {
            return JObject.Parse(response)["message"]!.ToString().Equals("Email was sent");
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSuccessfulConfirmed(string response)
    {
        try
        {
            return JObject.Parse(response)["message"]!.ToString().Equals("Confirmed");
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Функции API (Серые)
    
    private static async Task<string> ApiRegister(string name, string email, string pass)
    {
        try
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, $"{Host}/v3.4/auth/email/register");
            request.Headers.Add("user-agent", UserAgent);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(name), "name");
            content.Add(new StringContent(email), "email");
            content.Add(new StringContent(pass), "password");
            request.Content = content;
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return string.Empty;
        }
    }
    
    private static async Task<string> ApiConfirmEmail(string token)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{Host}/v3.4/auth/email/confirm");
        request.Headers.Add("User-Agent", UserAgent);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(token), "token");
        request.Content = content;
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
    
    private static async Task<string> ApiLogin(string email, string password)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{Host}/v3.4/auth/email/login");
        request.Headers.Add("user-agent", UserAgent);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(email), "email");
        content.Add(new StringContent(password), "password");
        request.Content = content;
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
    
    private async Task<string> ApiLike(string postId, bool isDislike)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{Host}/v2.4/like");
        request.Headers.Add("jwtauthorization", $"Bearer {_token}");
        request.Headers.Add("user-agent", UserAgent);
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(postId), "id");
        content.Add(new StringContent("content"), "type");
        content.Add(new StringContent(isDislike ? "-1" : "1"), "sign");
        request.Content = content;
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> ApiOpenComments(string postId)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Host}/v2.4/comments?sorting=hotness&contentId={postId}");
        request.Headers.Add("jwtauthorization", $"Bearer {_token}");
        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
    
    #endregion
}