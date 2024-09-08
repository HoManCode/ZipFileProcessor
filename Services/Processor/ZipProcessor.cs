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
            Log.Information("Starting ZIP file processing for {ZipFilePath}", zipFilePath);

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

                        var extractPath = CreateExtractPath(applicationNo);

                        ExtractFiles(zipArchive, extractPath);
                    }

                    await NotifyProcessingSuccess(zipFilePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while processing the ZIP file.");
                if (zipFilePath != null) await NotifyProcessingFailure(zipFilePath, ex);
                throw;
            }
        }

        private static void ValidateZipFile(string zipFilePath, ICollection<string> fileTypes)
        {
            if (!IsZipFile(zipFilePath))
            {
                var message = "The file is not a ZIP file.";
                Log.Error(message);
                throw new InvalidDataException(message);
            }

            if (!IsZipFileValid(zipFilePath, fileTypes))
            {
                var message = "The ZIP file is corrupt or invalid.";
                Log.Error(message);
                throw new InvalidDataException(message);
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
                ArgumentNullException.ThrowIfNull(fileTypes);
                foreach (var entry in zipArchive.Entries)
                {
                    var extension = Path.GetExtension(entry.FullName).ToLower();
                    if (fileTypes.Contains(extension)) continue;
                    Log.Error("Invalid file type detected: {FileName}", entry.FullName); 
                    throw new InvalidDataException($"Invalid file type: {entry.FullName}");
                }
                isValid = true;
            }
            catch (InvalidDataException ex)
            {
                Log.Error(ex, "The ZIP file is invalid or corrupted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while opening the ZIP file.");
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

        private static string CreateExtractPath(string applicationNo)
        {
            var guid = Guid.NewGuid().ToString();
            var extractPath = Path.Combine("extracted", $"{applicationNo}-{guid}");
            Directory.CreateDirectory(extractPath);
            return extractPath;
        }

        private static void ExtractFiles(ZipArchive zipArchive, string extractPath)
        {
            foreach (var entry in zipArchive.Entries)
            {
                var destinationPath = Path.Combine(extractPath, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException());
                entry.ExtractToFile(destinationPath, overwrite: true);
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