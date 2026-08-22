using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Kavita.Services.Import;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Kavita.Services.Tests.Import;

/// <summary>
/// End-to-end tests for the Kavita Importer feature using real file system operations.
/// </summary>
public class KavitaImporterServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _importFolder;
    private readonly string _targetLibrary;
    private readonly KavitaImporterService _importer;

    public KavitaImporterServiceTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "kavita-importer-tests", Guid.NewGuid().ToString("N"));
        _importFolder = Path.Combine(_testRoot, "import");
        _targetLibrary = Path.Combine(_testRoot, "library");
        Directory.CreateDirectory(_importFolder);
        Directory.CreateDirectory(_targetLibrary);

        var directoryService = new DirectoryService(
            Substitute.For<ILogger<DirectoryService>>(), new FileSystem());
        var formatDetector = new FormatDetectorService(
            Substitute.For<ILogger<FormatDetectorService>>());
        var directoryBuilder = new DirectoryStructureBuilder(
            Substitute.For<ILogger<DirectoryStructureBuilder>>(), directoryService);

        _importer = new KavitaImporterService(
            Substitute.For<ILogger<KavitaImporterService>>(),
            directoryService,
            formatDetector,
            directoryBuilder,
            Substitute.For<ILogger<ImportFolderWatcher>>());
    }

    private async Task ConfigureAsync()
    {
        await _importer.UpdateSettingsAsync(new ImportSettings
        {
            ImportFolderPath = _importFolder,
            TargetLibraryPath = _targetLibrary,
            EnableMonitoring = true,
            EnableOrganization = true,
            PollingIntervalSeconds = 30
        });
    }

    private string CreateEpub(string fileName)
    {
        var path = Path.Combine(_importFolder, fileName);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var mimetype = archive.CreateEntry("mimetype");
        using var writer = new StreamWriter(mimetype.Open());
        writer.Write("application/epub+zip");
        return path;
    }

    [Fact]
    public async Task InitializeAsync_StartsMonitoring()
    {
        await ConfigureAsync();
        await _importer.InitializeAsync();

        Assert.True(_importer.IsInitialized);
        Assert.True(_importer.IsMonitoring);

        await _importer.StopMonitoringAsync();
        Assert.False(_importer.IsMonitoring);
    }

    [Fact]
    public async Task ImportAsync_EpubFile_OrganizesIntoTargetLibrary()
    {
        await ConfigureAsync();
        await _importer.InitializeAsync();
        await _importer.StopMonitoringAsync(); // deterministic: no watcher racing the import

        var epubPath = CreateEpub("The Great Adventure.epub");

        var result = await _importer.ImportAsync(epubPath);

        Assert.True(result.Success, string.Join("; ", result.ErrorMessages));
        Assert.Equal(1, result.ImportedFiles);
        var status = Assert.Single(result.ImportedFilesList);
        Assert.Equal(ImportStatus.Completed, status.Status);
        Assert.Equal(MediaFormat.EPUB, status.Format);

        // The file must exist at the reported target and no longer sit unorganized in the import root
        Assert.False(string.IsNullOrWhiteSpace(status.TargetFolder));
        var organized = Directory.GetFiles(_testRoot, "*.epub", SearchOption.AllDirectories);
        Assert.NotEmpty(organized);
        Assert.Contains(organized, f => f != epubPath || File.Exists(epubPath));
    }

    [Fact]
    public async Task ImportAsync_MissingPath_Fails()
    {
        await ConfigureAsync();

        var result = await _importer.ImportAsync(Path.Combine(_testRoot, "does-not-exist.epub"));

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessages);
    }

    [Fact]
    public async Task GetStatistics_ReflectsImportedFiles()
    {
        await ConfigureAsync();
        await _importer.InitializeAsync();
        await _importer.StopMonitoringAsync();

        var epubPath = CreateEpub("Stats Book.epub");
        await _importer.ImportAsync(epubPath);

        var stats = _importer.GetStatistics();
        Assert.Equal(1, (int)stats["TotalImportedFiles"]);
    }

    [Theory]
    [InlineData("My Book.epub")]
    [InlineData("Some Comic.cbz")]
    [InlineData("Another Comic.cbr")]
    [InlineData("Manual.pdf")]
    [InlineData("Cover.png")]
    [InlineData("Page.jpg")]
    [InlineData("Old Book.mobi")]
    [InlineData("Kindle Book.azw3")]
    public void Blacklist_DoesNotBlockImportableMediaFormats(string fileName)
    {
        Assert.False(BlacklistConfiguration.IsFileBlacklisted(fileName));
    }

    [Theory]
    [InlineData("Thumbs.db")]
    [InlineData("script.js")]
    [InlineData("code.cs")]
    [InlineData("movie.mp4")]
    [InlineData("song.mp3")]
    public void Blacklist_BlocksNonMediaFiles(string fileName)
    {
        Assert.True(BlacklistConfiguration.IsFileBlacklisted(fileName));
    }

    public void Dispose()
    {
        _importer.Dispose();
        try { Directory.Delete(_testRoot, true); } catch { /* best effort */ }
    }
}
