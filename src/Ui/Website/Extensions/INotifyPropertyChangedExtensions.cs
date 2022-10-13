using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace CrossBusExplorer.Website.Extensions;
//source: http://www.blackwasp.co.uk/INotifyPropertyChangedExt.aspx

public static class INotifyPropertyChangedExtensions
{
    public static void Notify(
        this INotifyPropertyChanged sender,
        PropertyChangedEventHandler handler,
        [CallerMemberName] string propertyName = "")
    {
        if (handler != null)
        {
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            handler(sender, args);
        }
    }
}