namespace RentalManager.Api.Services;

/// <summary>檔案儲存抽象；目前為本機磁碟，未來可替換為物件儲存。</summary>
public interface IFileStorage
{
    Task SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default);
    Stream OpenRead(string relativePath);
    void Delete(string relativePath);
    bool Exists(string relativePath);
}

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configured = configuration["Storage:Root"] ?? "App_Data/uploads";
        _root = Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var target = File.Create(fullPath);
        await content.CopyToAsync(target, cancellationToken);
    }

    public Stream OpenRead(string relativePath)
        => File.OpenRead(ResolveFullPath(relativePath));

    public void Delete(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    public bool Exists(string relativePath)
        => File.Exists(ResolveFullPath(relativePath));

    private string ResolveFullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        // 防止路徑跳脫儲存根目錄
        if (!fullPath.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }
        return fullPath;
    }
}
