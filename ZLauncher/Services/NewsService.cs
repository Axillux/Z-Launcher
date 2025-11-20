using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZLauncher.Models;

namespace ZLauncher.Services;

public class NewsService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/Axillux/Z-Launcher/releases";

    public async Task<List<NewsItem>> GetNewsAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "ZLauncher");

            var json = await client.GetStringAsync(GitHubApiUrl);
            var releases = JsonDocument.Parse(json).RootElement;
            
            var news = new List<NewsItem>();
            
            foreach (var release in releases.EnumerateArray())
            {
                if (news.Count >= 5) break;

                var title = release.GetProperty("name").GetString() ?? release.GetProperty("tag_name").GetString() ?? "Update";
                var body = release.GetProperty("body").GetString() ?? "";
                var date = release.GetProperty("published_at").GetDateTime().ToString("dd.MM.yyyy");
                var url = release.GetProperty("html_url").GetString() ?? "";

                var shortContent = body.Length > 100 ? body.Substring(0, 97) + "..." : body;

                news.Add(new NewsItem
                {
                    Title = title,
                    Content = shortContent.Replace("\r\n", " ").Replace("#", ""),
                    Date = date,
                    Url = url
                });
            }

            return news;
        }
        catch
        {
            return new List<NewsItem>
            {
                new NewsItem { Title = "Ошибка загрузки новостей", Content = "Не удалось получить данные с GitHub.", Date = DateTime.Now.ToString("dd.MM.yyyy") }
            };
        }
    }
}
