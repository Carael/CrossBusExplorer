using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public class MessageExportDialogResult
{
    public string? FolderPath { get; set; }
    public MessageFileType MessageFileType { get; set; }
    public SubQueue SubQueue { get; set; }
    public ReceiveMode ReceiveMode { get; set; }
    public ReceiveType ReceiveType { get; set; }
    public int? MessagesCount { get; set; }
    public long? FromSequenceNumber { get; set; }
}
