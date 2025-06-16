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
    private List<PromptFileInfo> _manifestFileInfos = new();
    private Dictionary<string, PromptItem> _loadedPrompts = new();
    private bool _manifestLoaded = false;
    private bool _allPromptsLoaded = false;

    public PromptService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }    public async Task<List<PromptItem>> GetAllPromptsAsync()
    {
        if (!_allPromptsLoaded)
        {
            await LoadAllPromptsAsync();
        }
        return _prompts;
    }

    public async Task<int> GetPromptCountAsync()
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }
        return _manifestFileInfos.Count;
    }

    public async Task<int> GetCategoryCountAsync()
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }
        return _manifestFileInfos.Select(f => f.Category).Distinct().Count();
    }

    public async Task<List<string>> GetCategoriesFromManifestAsync()
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }
        return _manifestFileInfos.Select(f => f.Category).Distinct().OrderBy(c => c).ToList();
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
    }    public async Task<PromptItem?> GetPromptByIdAsync(string id)
    {
        // 이미 로드된 프롬프트가 있는지 확인
        if (_loadedPrompts.ContainsKey(id))
        {
            return _loadedPrompts[id];
        }

        // 매니페스트에서 해당 파일 정보 찾기
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }

        var fileInfo = _manifestFileInfos.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f.FileName) == id);
        if (fileInfo != null)
        {
            var prompt = await LoadSinglePromptAsync(fileInfo);
            if (prompt != null)
            {
                _loadedPrompts[id] = prompt;
                return prompt;
            }
        }

        // 모든 프롬프트가 로드되지 않았다면 전체 로드 후 검색
        if (!_allPromptsLoaded)
        {
            var allPrompts = await GetAllPromptsAsync();
            return allPrompts.FirstOrDefault(p => p.Id == id);
        }

        return null;
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
    }
    
    private async Task LoadAllPromptsAsync()
    {
        try
        {
            if (!_manifestLoaded)
            {
                await LoadManifestAsync();
            }

            var prompts = new List<PromptItem>();

            foreach (var fileInfo in _manifestFileInfos)
            {
                // 이미 로드된 프롬프트가 있다면 재사용
                var promptId = Path.GetFileNameWithoutExtension(fileInfo.FileName);
                if (_loadedPrompts.ContainsKey(promptId))
                {
                    prompts.Add(_loadedPrompts[promptId]);
                    continue;
                }

                var prompt = await LoadSinglePromptAsync(fileInfo);
                if (prompt != null)
                {
                    prompts.Add(prompt);
                    _loadedPrompts[promptId] = prompt;
                }
            }

            _prompts = prompts;
            _allPromptsLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading all prompts: {ex.Message}");
            _prompts = new List<PromptItem>();
            _allPromptsLoaded = true;
        }
    }

    private async Task LoadManifestAsync()
    {
        try
        {
            var manifestResponse = await _httpClient.GetAsync("prompts/manifest.json");
            if (!manifestResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Manifest file not found");
                _manifestFileInfos = new List<PromptFileInfo>();
                _manifestLoaded = true;
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
                _manifestFileInfos = new List<PromptFileInfo>();
            }
            else
            {
                // 매니페스트에서 카테고리 정보 추출
                foreach (var fileInfo in fileInfos)
                {
                    if (string.IsNullOrEmpty(fileInfo.Category) && !string.IsNullOrEmpty(fileInfo.RelPath))
                    {
                        fileInfo.Category = fileInfo.RelPath.Trim('/');
                    }
                }
                _manifestFileInfos = fileInfos;
            }

            _manifestLoaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading manifest: {ex.Message}");
            _manifestFileInfos = new List<PromptFileInfo>();
            _manifestLoaded = true;
        }
    }

    private async Task<PromptItem?> LoadSinglePromptAsync(PromptFileInfo fileInfo)
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
                
                // 카테고리 정보가 없다면 매니페스트에서 가져오기
                if (string.IsNullOrEmpty(prompt.Category) && !string.IsNullOrEmpty(fileInfo.Category))
                {
                    prompt.Category = fileInfo.Category;
                }
                
                return prompt;
            }
            else
            {
                Console.WriteLine($"Failed to load {fileInfo.RelPath}/{fileInfo.FileName}: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {fileInfo.FileName}: {ex.Message}");
            return null;
        }
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
    // 매니페스트 파일의 파일 정보를 나타내는 클래스
    private class PromptFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string RelPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }    public async Task<List<PromptItem>> GetAllPromptMetadataAsync()
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }

        var prompts = new List<PromptItem>();
        foreach (var fileInfo in _manifestFileInfos)
        {
            var promptId = Path.GetFileNameWithoutExtension(fileInfo.FileName);
            
            // 이미 로드된 프롬프트가 있다면 재사용
            if (_loadedPrompts.ContainsKey(promptId))
            {
                var existingPrompt = _loadedPrompts[promptId];
                prompts.Add(new PromptItem
                {
                    Id = existingPrompt.Id,
                    Title = existingPrompt.Title,
                    Description = existingPrompt.Description,
                    Category = existingPrompt.Category,
                    Tags = existingPrompt.Tags,
                    Author = existingPrompt.Author,
                    CreatedDate = existingPrompt.CreatedDate,
                    UpdatedDate = existingPrompt.UpdatedDate,
                    Content = "" // Content는 포함하지 않음
                });
            }
            else
            {
                // 메타데이터만 추출하기 위해 실제 파일을 로드
                var prompt = await LoadSinglePromptAsync(fileInfo);
                if (prompt != null)
                {
                    // Content를 제거한 버전 생성
                    var metadataPrompt = new PromptItem
                    {
                        Id = prompt.Id,
                        Title = prompt.Title,
                        Description = prompt.Description,
                        Category = prompt.Category,
                        Tags = prompt.Tags,
                        Author = prompt.Author,
                        CreatedDate = prompt.CreatedDate,
                        UpdatedDate = prompt.UpdatedDate,
                        Content = "" // Content는 포함하지 않음
                    };
                    prompts.Add(metadataPrompt);
                    
                    // 캐시에는 전체 내용을 저장
                    _loadedPrompts[promptId] = prompt;
                }
            }
        }

        return prompts.OrderByDescending(p => p.UpdatedDate).ToList();
    }

    public async Task<int> GetCategoryCountAsync(string category)
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }
        return _manifestFileInfos.Count(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    public async IAsyncEnumerable<PromptItem> GetPromptsAsyncEnumerable()
    {
        if (!_manifestLoaded)
        {
            await LoadManifestAsync();
        }

        foreach (var fileInfo in _manifestFileInfos)
        {
            // 이미 로드된 프롬프트가 있다면 재사용
            var promptId = Path.GetFileNameWithoutExtension(fileInfo.FileName);
            if (_loadedPrompts.ContainsKey(promptId))
            {
                yield return _loadedPrompts[promptId];
                continue;
            }

            var prompt = await LoadSinglePromptAsync(fileInfo);
            if (prompt != null)
            {
                _loadedPrompts[promptId] = prompt;
                yield return prompt;
            }
        }
    }

    public async IAsyncEnumerable<PromptItem> GetPromptsByCategoryAsyncEnumerable(string category)
    {
        await foreach (var prompt in GetPromptsAsyncEnumerable())
        {
            if (prompt.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            {
                yield return prompt;
            }
        }
    }

    public async IAsyncEnumerable<PromptItem> SearchPromptsAsyncEnumerable(string searchTerm)
    {
        await foreach (var prompt in GetPromptsAsyncEnumerable())
        {
            if (prompt.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                prompt.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                prompt.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                prompt.Tags.Any(t => t.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
            {
                yield return prompt;
            }
        }
    }
}
