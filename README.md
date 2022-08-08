# Mjpeg Decoder NET

Modified version of this repo [https://github.com/arndre/MjpegDecoder](https://github.com/arndre/MjpegDecoder)

## C# Mjpeg Decoder
Modified version of article on coding4fun, not available  (https://channel9.msdn.com/coding4fun/articles/MJPEG-Decoder)
 
 * Removed preprocessor directives for XNA, WINRT,...
 * Fixed frame drops 
 * Increased performance with dynamic chunksize
 * Replaced boundary with jpeg EOI (https://en.wikipedia.org/wiki/JPEG_File_Interchange_Format)
 * Replaced whole code for image data processing
 * Increased maintainability :)
