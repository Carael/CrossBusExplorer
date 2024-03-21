using System.Collections.Generic;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public record MessagesUploadDialogResult(MessageFileType Type, IList<string> FilesContent);
