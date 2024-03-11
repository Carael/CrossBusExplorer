using System.Collections.Generic;
using CrossBusExplorer.ServiceBus.Contracts.Types;
namespace CrossBusExplorer.Website.Models;

public record MessagesUploadDialogResult(UploadFileType Type, IList<string> FilesContent);
