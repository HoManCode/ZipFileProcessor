using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;
using ZipFileProcessor.Services.Notification;
using ZipFileProcessor.Services.Validator;

namespace ZipFileProcessor.Services.Processor
{
    public class ZipProcessor : IProcessor
    {
        private readonly IValidator _xmlValidator;
        private readonly INotification _emailNotification;
        private readonly IConfiguration _configuration;

        public ZipProcessor(
            IValidator xmlValidator,
            INotification emailNotification,
            IConfiguration configuration)
        {
            _xmlValidator = xmlValidator;
            _emailNotification = emailNotification;
            _configuration = configuration;
        }

        public async Task Process(string? zipFilePath)
        {
            Log.Information("********************Starting ZIP file processing for {ZipFilePath}********************", zipFilePath);

            var fileTypes = _configuration.GetSection("FileTypes").Get<List<string>>()
                ?? throw new InvalidOperationException("File types configuration is missing or empty");

            try
            {
                if (zipFilePath != null)
                {
                    ValidateZipFilePath(zipFilePath);

                    ValidateZipFile(zipFilePath, fileTypes);
                    
                    using var zipArchive = ZipFile.OpenRead(zipFilePath);

                    var xmlFile = zipArchive.GetEntry(GetConfigurationValue("PartyXmlFile"));
                    if (xmlFile != null)
                    {
                        ValidateXmlFile(xmlFile);

                        var schemaFilePath = GetConfigurationValue("XsdFileLoc");
                        ValidateXmlSchema(xmlFile, schemaFilePath);

                        var applicationNo = ExtractAndValidateApplicationNumber(xmlFile);

                        var extractPath = CreateExtractPath(applicationNo, _configuration);

                        ExtractFiles(zipArchive, extractPath);
                    }

                    Log.Information($"The ZIP file at {zipFilePath} was processed successfully.");
                    await NotifyProcessingSuccess(zipFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Information($"The processing of the ZIP file at {zipFilePath} failed.");
                if (zipFilePath != null) await NotifyProcessingFailure(zipFilePath, ex);
            }
        }

        private static void ValidateZipFile(string zipFilePath, ICollection<string> fileTypes)
        {
            if (!IsZipFile(zipFilePath))
            {
                throw new InvalidDataException("The file is not a ZIP file.");
            }

            if (!IsZipFileValid(zipFilePath, fileTypes))
            {
                throw new InvalidDataException("The ZIP file is corrupt or invalid.");
            }
        }
        
        private static bool IsZipFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        }
        
        private static bool IsZipFileValid(string filePath, ICollection<string> fileTypes)
        {
            var isValid = false;
            try
            {
                using var zipArchive = ZipFile.OpenRead(filePath);
                foreach (var entry in zipArchive.Entries)
                {
                    var extension = Path.GetExtension(entry.FullName).ToLower();
                    if (fileTypes.Contains(extension)) continue;
                    throw new InvalidDataException($"Invalid file type: {entry.FullName}");
                }
                isValid = true;
            }
            catch (IOException ex)
            {
                Log.Error("An I/O error occurred while opening the ZIP file: {message}", ex.Message);
            }
            catch (InvalidDataException ex)
            {
                Log.Error("The ZIP file is invalid or corrupted: {message}", ex.Message);
            }
            catch (Exception ex)
            {
                Log.Error("An unexpected error occurred: {message} ",ex.Message);
            }

            return isValid;
        }

        private static void ValidateZipFilePath(string zipFilePath)
        {
            if (File.Exists(zipFilePath)) return;
            Log.Error("ZIP file not found at path: {ZipFilePath}", zipFilePath);
            throw new FileNotFoundException("ZIP file not found.", zipFilePath);
        }

        private static void ValidateXmlFile(ZipArchiveEntry? xmlFile)
        {
            if (xmlFile != null) return;
            Log.Error("Missing XML file in ZIP archive.");
            throw new InvalidDataException("Missing XML file.");
        }

        private void ValidateXmlSchema(ZipArchiveEntry xmlFile, string schemaFilePath)
        {
            if (schemaFilePath == null)
            {
                Log.Error("Missing XML schema file path in configuration.");
                throw new InvalidOperationException("Missing XML schema file path.");
            }

            if (_xmlValidator.Validate(xmlFile, schemaFilePath)) return;
            Log.Error("XML validation failed.");
            throw new InvalidDataException("XML validation failed.");
        }

        private static string ExtractAndValidateApplicationNumber(ZipArchiveEntry xmlFile)
        {
            var xDocument = XDocument.Load(xmlFile.Open());
            var applicationNo = xDocument.Root?.Element("applicationno")?.Value
                ?? throw new InvalidDataException("Missing application number in XML.");
            return applicationNo;
        }

        private string CreateExtractPath(string applicationNo, IConfiguration configuration)
        {
            var guid = Guid.NewGuid().ToString();
            var extractPath = Path.Combine(GetConfigurationValue("ExtractedFilesLoc"), $"{applicationNo}-{guid}");
            Directory.CreateDirectory(extractPath);
            return extractPath;
        }

        private static void ExtractFiles(ZipArchive zipArchive, string extractPath)
        {
            foreach (var entry in zipArchive.Entries)
            {
                var destinationPath = Path.Combine(extractPath, entry.FullName);
                var directory = Path.GetDirectoryName(destinationPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Log.Information("Created directory: {DirectoryPath}", directory);
                }

                entry.ExtractToFile(destinationPath, overwrite: true);
                Log.Information("Extracted file: {FilePath}", destinationPath);
            }
        }

        private async Task NotifyProcessingSuccess(string zipFilePath)
        {
            await _emailNotification.SendNotification("ZIP File Processed Successfully",
                $"The ZIP file at {zipFilePath} was processed successfully.");
        }

        private async Task NotifyProcessingFailure(string zipFilePath, Exception ex)
        {
            await _emailNotification.SendNotification("ZIP File Processing Failed",
                $"The ZIP file at {zipFilePath} could not be processed. Error: {ex.Message}");
        }

        private string GetConfigurationValue(string key)
        {
            return _configuration.GetSection(key).Value
                ?? throw new InvalidOperationException($"Missing configuration value for key: {key}");
        }
    }
}