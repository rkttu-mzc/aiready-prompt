using MzcAirPrompt.Models;
using Markdig;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MzcAirPrompt.Services;

public class PromptService
{
    private readonly HttpClient _httpClient;
    private readonly MarkdownPipeline _markdownPipeline;
    private List<PromptItem> _prompts = new();
    private bool _isLoaded = false;

    public PromptService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public async Task<List<PromptItem>> GetAllPromptsAsync()
    {
        if (!_isLoaded)
        {
            await LoadPromptsAsync();
        }
        return _prompts;
    }

    public async Task<List<PromptItem>> GetPromptsByCategoryAsync(string category)
    {
        var allPrompts = await GetAllPromptsAsync();
        return allPrompts.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<PromptItem>> SearchPromptsAsync(string searchTerm)
    {
        var allPrompts = await GetAllPromptsAsync();
        return allPrompts.Where(p => 
            p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            p.Tags.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        ).ToList();
    }

    public async Task<PromptItem?> GetPromptByIdAsync(string id)
    {
        var allPrompts = await GetAllPromptsAsync();
        return allPrompts.FirstOrDefault(p => p.Id == id);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var allPrompts = await GetAllPromptsAsync();
        return allPrompts.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
    }

    public async Task<List<string>> GetTagsAsync()
    {
        var allPrompts = await GetAllPromptsAsync();
        return allPrompts.SelectMany(p => p.Tags).Distinct().OrderBy(t => t).ToList();
    }    private async Task LoadPromptsAsync()
    {
        try
        {
            // 실제 마크다운 파일들을 로드
            await LoadMarkdownFilesAsync();
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading prompts: {ex.Message}");
            _prompts = new List<PromptItem>();
            _isLoaded = true;
        }
    }
    
    private Task InitializeSampleListAsync()
    {
        _prompts = new List<PromptItem>();
        return Task.CompletedTask;
    }

    private PromptItem ParseMarkdownFile(string content, string fileName)
    {
        var prompt = new PromptItem
        {
            Id = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName
        };

        // Front matter 파싱 (--- 로 구분된 YAML 헤더)
        var frontMatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);
        
        if (frontMatterMatch.Success)
        {
            var frontMatter = frontMatterMatch.Groups[1].Value;
            var markdownContent = frontMatterMatch.Groups[2].Value;

            // 간단한 YAML 파싱 (실제로는 YamlDotNet 같은 라이브러리 사용 권장)
            ParseFrontMatter(frontMatter, prompt);
            prompt.Content = markdownContent.Trim();
        }
        else
        {
            prompt.Content = content.Trim();
            // 제목이 없으면 파일명을 사용
            if (string.IsNullOrEmpty(prompt.Title))
            {
                prompt.Title = Path.GetFileNameWithoutExtension(fileName);
            }
        }

        prompt.HtmlContent = Markdown.ToHtml(prompt.Content, _markdownPipeline);
        return prompt;
    }

    private void ParseFrontMatter(string frontMatter, PromptItem prompt)
    {
        var lines = frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = line.Substring(0, colonIndex).Trim();
                var value = line.Substring(colonIndex + 1).Trim();

                switch (key.ToLower())
                {
                    case "title":
                        prompt.Title = value.Trim('"', '\'');
                        break;
                    case "description":
                        prompt.Description = value.Trim('"', '\'');
                        break;
                    case "category":
                        prompt.Category = value.Trim('"', '\'');
                        break;
                    case "author":
                        prompt.Author = value.Trim('"', '\'');
                        break;
                    case "tags":
                        // 간단한 배열 파싱 [tag1, tag2, tag3]
                        var tagsValue = value.Trim('[', ']');
                        prompt.Tags = tagsValue.Split(',')
                            .Select(t => t.Trim().Trim('"', '\''))
                            .Where(t => !string.IsNullOrEmpty(t))
                            .ToList();
                        break;
                    case "created":
                    case "date":
                        if (DateTime.TryParse(value.Trim('"', '\''), out var createdDate))
                        {
                            prompt.CreatedDate = createdDate;
                        }
                        break;
                    case "updated":
                        if (DateTime.TryParse(value.Trim('"', '\''), out var updatedDate))
                        {
                            prompt.UpdatedDate = updatedDate;
                        }
                        break;
                }
            }
        }
    }
    
    // 실제 마크다운 파일들을 로드하는 메서드
    private async Task LoadMarkdownFilesAsync()
    {
        try
        {
            // 매니페스트 파일에서 파일 목록 가져오기
            var manifestResponse = await _httpClient.GetAsync("prompts/manifest.json");
            if (!manifestResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Manifest file not found, falling back to sample data");
                await InitializeSampleListAsync();
                return;
            }

            var manifestJson = await manifestResponse.Content.ReadAsStringAsync();
            var fileInfos = JsonSerializer.Deserialize<List<PromptFileInfo>>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (fileInfos == null)
            {
                Console.WriteLine("Failed to deserialize manifest file");
                await InitializeSampleListAsync();
                return;
            }

            var prompts = new List<PromptItem>();

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    var fragments = new List<string>(["prompts"]);
                    if (!string.IsNullOrEmpty(fileInfo.RelPath))
                        fragments.Add(fileInfo.RelPath.Trim('/'));
                    fragments.Add(fileInfo.FileName);
                    var response = await _httpClient.GetAsync(string.Join('/', fragments));

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var prompt = ParseMarkdownFile(content, fileInfo.FileName);
                        prompts.Add(prompt);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load {fileInfo.RelPath}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading {fileInfo.FileName}: {ex.Message}");
                }
            }

            _prompts = prompts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading markdown files: {ex.Message}");
            await InitializeSampleListAsync();
        }
    }

    // 매니페스트 파일의 파일 정보를 나타내는 클래스
    private class PromptFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string RelPath { get; set; } = string.Empty;
    }
}
