# Mjpeg Decoder NET

Modified version of this repo [https://github.com/arndre/MjpegDecoder](https://github.com/arndre/MjpegDecoder)
that is based in this project, not longer available (https://channel9.msdn.com/coding4fun/articles/MJPEG-Decode) 

### Use example

```c#
Example in a WPF app
```xml
<Window x:Class="Demo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:Test"
        mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <Image Name="player" Stretch="Fill"/>  <!-- image -->
    </Grid>
</Window>
```

```c#
public partial class MainWindow : Window
{
    Mjpeg_Decoder_NET.MjpegDecoder decoder = new Mjpeg_Decoder_NET.MjpegDecoder();
    
    public MainWindow()
    {
        InitializeComponent();
        decoder.ParseStream(new Uri(@"http://192.168.1.xxxx:8081/video"));
        decoder.FrameReady += Decoder_FrameReady;
    }
    
    private void Decoder_FrameReady(object sender, Mjpeg_Decoder_NET.FrameReadyEventArgs e)
    {
        var buffer = e.FrameBuffer;
        var image = ToImage(buffer);
        Dispatcher.Invoke(() => player.Source = image);
    }
    
    private BitmapImage ToImage(byte[] buffer)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(buffer);
        bitmapImage.EndInit();
        return bitmapImage;
    }
}
```


Original Repo readme:
### C# Mjpeg Decoder
Modified version of article on coding4fun (https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder)
 
 * Removed preprocessor directives for XNA, WINRT,...
 * Fixed frame drops 
 * Increased performance with dynamic chunksize
 * Replaced boundary with jpeg EOI (https://en.wikipedia.org/wiki/JPEG_File_Interchange_Format)
 * Replaced whole code for image data processing
 * Increased maintainability :)
