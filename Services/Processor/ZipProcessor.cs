using System.IO.Compression;
using System.Xml.Linq;
using System.Xml.Schema;
using Microsoft.Extensions.Configuration;
using Serilog;
using ZipFileProcessor.Services.Notification;
using ZipFileProcessor.Services.Validator;

namespace ZipFileProcessor.Services.Processor;

public class ZipProcessor : IProcessor
{
    private readonly IValidator _xmlValidator;
    private readonly INotification _emailNotification;

    public ZipProcessor(
        IValidator xmlValidator,
        INotification emailNotification)
    {
        _xmlValidator = xmlValidator;
        _emailNotification = emailNotification;
    }

    public async Task Process(string zipFilePath, IConfiguration configuration)
    {
        Log.Information("********************Starting zip file process method********************");
        
        var fileTypes = configuration.GetSection("FileTypes").Get<List<string>>();
        if (fileTypes == null)
        {
            Log.Error("File types array is empty");
            return;
        }
        try
        {
            if (!File.Exists(zipFilePath))
            {
                Log.Error("ZIP file not found in path: {path}", zipFilePath);
                throw new FileNotFoundException("ZIP file not found.", zipFilePath);
            }

            using (var zipArchive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (var entry in zipArchive.Entries)
                {
                    var extension = Path.GetExtension(entry.FullName);
                    if (fileTypes.Contains(extension.ToLower())) continue;
                    Log.Error("Invalid file type: {entry.FullName}", entry.FullName);
                    throw new InvalidDataException($"Invalid file type or content: {entry.FullName}");

                }

                var xmlFile = zipArchive.GetEntry(configuration.GetSection("PartyXmlFile").Value ?? string.Empty);
                if (xmlFile == null)
                {
                    Log.Error("Missing party.XML file.");
                    throw new InvalidDataException($"Invalid file type or content: Missing party.XML file");
                }

                /*
                using (var xmlStream = xmlFile.Open())
                {
                    var xDoc = XDocument.Load(xmlStream);
                    var schemaSet = new XmlSchemaSet();
                    schemaSet.Add("", "party.xsd");

                    if (!_xmlValidator.Validate(xDoc, schemaSet))
                    {
                        throw new InvalidDataException("Invalid XML format.");
                    }
                }

                // Extract ZIP contents
                string applicationNo = xDoc.Root.Element("applicationno")?.Value ?? throw new InvalidDataException("Missing application number.");
                string guid = Guid.NewGuid().ToString();
                string extractPath = Path.Combine("extracted", $"{applicationNo}-{guid}");

                Directory.CreateDirectory(extractPath);
                foreach (var entry in zipArchive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException()); 
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }*/
                
            }

            await _emailNotification.SendNotification("ZIP File Processed Successfully", $"The ZIP file at {zipFilePath} was processed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while processing the ZIP file.");
            await _emailNotification.SendNotification("ZIP File Processing Failed", $"The ZIP file at {zipFilePath} could not be processed. Error: {ex.Message}");
            throw;
        }
    }
}