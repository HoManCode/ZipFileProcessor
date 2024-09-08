using Microsoft.Extensions.Configuration;

namespace ZipFileProcessor.Services.Processor;

public interface IProcessor
{
    Task Process(string? filepath);
}