namespace ZipFileProcessor.Services.Validator;

public interface IValidator
{
    bool Validate(System.IO.Compression.ZipArchiveEntry file, string schemaFilePath);
}