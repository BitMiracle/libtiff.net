using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;

using BitMiracle.LibTiff.Classic;

namespace SilverlightTestApplication
{
    // This is a demo application that shows how read the data bits of Tiff files and apply them to Silverlight's WriteableBitmap.
    // Developers can then encapsulate this functionality into their own custom controls.
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();

            this.Dispatcher.BeginInvoke(this.OnInitialized);
        }

        private void OnInitialized()
        {
            foreach (var fileName in this.GetTestTiffFileNames())
            {
                var uri = new Uri(String.Concat("TestTiffs/", fileName), UriKind.Relative);

                WebClient client = new WebClient();
                client.OpenReadCompleted += new OpenReadCompletedEventHandler(this.WebClient_OpenReadCompleted);
                client.OpenReadAsync(uri, uri);
            }
        }

        private void WebClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            (sender as WebClient).OpenReadCompleted -= this.WebClient_OpenReadCompleted;
            var uri = e.UserState as Uri;

            if (e.Error != null)
            {
                this.RenderResultFrames(uri.ToString(), new FrameworkElement[] { this.CreateErrorMessageTextBlock(e.Error) });

                return;
            }

            if (e.Cancelled)
            {
                this.RenderResultFrames(uri.ToString(), new FrameworkElement[] { this.CreateErrorMessageTextBlock("Request was cancelled.") });

                return;
            }

            try
            {
                FrameworkElement[] resultFramesWithInfo = null;

                using (Stream s = e.Result)
                {
                    using (Tiff tiffWorker = Tiff.ClientOpen(uri.ToString(), "read", e.Result, new TiffStream()))
                    {
                        short dirs = tiffWorker.NumberOfDirectories();

                        if (dirs > 0)
                        {
                            resultFramesWithInfo = new FrameworkElement[dirs];

                            for (int i = 0; i < dirs; i++)
                            {
                                if (tiffWorker.SetDirectory((short)i))
                                {
                                    int tileCount = tiffWorker.NumberOfTiles();
                                    int stripCount = tiffWorker.NumberOfStrips();

                                    var frameWidthField = tiffWorker.GetField(TiffTag.IMAGEWIDTH);
                                    var frameHeightField = tiffWorker.GetField(TiffTag.IMAGELENGTH);
                                    var compressionField = tiffWorker.GetField(TiffTag.COMPRESSION);
                                    var xResolutionField = tiffWorker.GetField(TiffTag.XRESOLUTION);
                                    var yResolutionField = tiffWorker.GetField(TiffTag.YRESOLUTION);
                                    var samplesPerPixelField = tiffWorker.GetField(TiffTag.SAMPLESPERPIXEL);

                                    int frameWidth = frameWidthField != null && frameWidthField.Length > 0 ? frameWidthField[0].ToInt() : 0;
                                    int frameHeight = frameHeightField != null && frameHeightField.Length > 0 ? frameHeightField[0].ToInt() : 0;
                                    var compression = compressionField != null && compressionField.Length > 0 ? (Compression)compressionField[0].Value : Compression.NONE;
                                    var xResolution = xResolutionField != null && xResolutionField.Length > 0 ? new double?(xResolutionField[0].ToDouble()) : null;
                                    var yResolution = yResolutionField != null && yResolutionField.Length > 0 ? new double?(yResolutionField[0].ToDouble()) : null;
                                    var samplesPerPixel = samplesPerPixelField != null && samplesPerPixelField.Length > 0 ? samplesPerPixelField[0].ToString() : String.Empty;

                                    if (xResolution != null && yResolution == null)
                                    {
                                        yResolution = xResolution;
                                    }

                                    FrameworkElement resultFrame = null;
                                    try
                                    {
#if NEVER // For illustrative purposes: A different way to read 2-bit Tiff frames
                                        if (compression == Compression.CCITT_T4 || compression == Compression.CCITT_T6 || compression == Compression.CCITTRLE || compression == Compression.CCITTRLEW)
                                        {
                                            var imageByteRows = new List<byte[]>(frameHeight);
                                            for (int row = 0; row < frameHeight; row++)
                                            {
                                                byte[] buffer = new byte[frameWidth / 8 + 1];
                                                if (!tiffWorker.ReadScanline(buffer, row))
                                                {
                                                    throw new InvalidOperationException(String.Concat("Could not read row ", row, "."));
                                                }

                                                imageByteRows.Add(buffer);
                                            }

                                            var bmp = new WriteableBitmap(frameWidth, frameHeight);

                                            for (int y = 0; y < frameHeight; y++)
                                            {
                                                for (int x = 0; x < frameWidth; x++)
                                                {
                                                    int value = (imageByteRows[y][x / 8] >> 7 - x % 8) % 2 > 0 ? 0 : 255;

                                                    bmp.Pixels[y * frameWidth + x] = (255 << 24) | (value << 16) | (value << 8) | value;
                                                }
                                            }

                                            resultFrame = this.CreateImageElement(bmp, xResolution, yResolution);
                                        }
                                        else
#endif
                                        {
                                            var buffer = new int[frameWidth * frameHeight];
                                            tiffWorker.ReadRGBAImage(frameWidth, frameHeight, buffer);

                                            var bmp = new WriteableBitmap(frameWidth, frameHeight);
                                            for (int y = 0; y < frameHeight; y++)
                                            {
                                                var ytif = y * frameWidth;
                                                var ybmp = (frameHeight - y - 1) * frameWidth;

                                                for (int x = 0; x < frameWidth; x++)
                                                {
                                                    var currentValue = buffer[ytif + x];

                                                    // Shift the Tiff's RGBA format to the Silverlight WriteableBitmap's ARGB format
                                                    bmp.Pixels[ybmp + x] = Tiff.GetB(currentValue) | Tiff.GetG(currentValue) << 8 | Tiff.GetR(currentValue) << 16 | Tiff.GetA(currentValue) << 24;
                                                }
                                            }

                                            resultFrame = this.CreateImageElement(bmp, xResolution, yResolution);
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        resultFrame = this.CreateErrorMessageTextBlock(exc);
                                    }

                                    resultFramesWithInfo[i] = this.CreateResultFrameWithInfo(resultFrame,
                                            String.Concat("Frame ", i, ", ",
                                                    frameWidth, "x", frameHeight,
                                                    xResolution == null ? ", " : String.Concat(" @ ", xResolution,
                                                            yResolution == null || xResolution == yResolution ? String.Empty : String.Concat("x", yResolution),
                                                            " dpi, "),
                                                    compression.ToString(), ", ",
                                                    samplesPerPixel, " spp, ",
                                                    tileCount, " tile(s), ",
                                                    stripCount, " strip(s)"));
                                }
                            }

                            this.RenderResultFrames(uri.ToString(), resultFramesWithInfo);
                        }
                        else
                        {
                            this.RenderResultFrames(uri.ToString(), new FrameworkElement[] { this.CreateErrorMessageTextBlock("No frames found.") });
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.RenderResultFrames(uri.ToString(), new FrameworkElement[] { this.CreateErrorMessageTextBlock(exc) });
            }
        }

        private Image CreateImageElement(WriteableBitmap bmp, double? xResolution, double? yResolution)
        {
            double widthMultiplicator = 1.0;
            double heightMultiplicator = 1.0;

            // DPI resolution can be different for x and y.
            // It's best to scale the Image UI element accordingly.
            if (xResolution != null)
            {
                if (yResolution == null)
                {
                    yResolution = xResolution;
                }

                if (xResolution > yResolution)
                {
                    widthMultiplicator = yResolution.Value / xResolution.Value;
                }
                else if (yResolution > xResolution)
                {
                    heightMultiplicator = xResolution.Value / yResolution.Value;
                }
            }

            return new Image()
            {
                Stretch = Stretch.Fill,
                Source = bmp,
                Width = bmp.PixelWidth * widthMultiplicator,
                Height = bmp.PixelHeight * heightMultiplicator
            };
        }

        private TextBlock CreateErrorMessageTextBlock(Exception exc)
        {
            return this.CreateErrorMessageTextBlock(String.Concat(exc.GetType().Name, ": ", exc.ToString()));
        }

        private TextBlock CreateErrorMessageTextBlock(string message)
        {
            return new TextBlock()
            {
                Margin = new Thickness(5),
                Text = message,
                Foreground = new SolidColorBrush(Colors.Red),
                FontWeight = FontWeights.Bold
            };
        }

        private FrameworkElement CreateResultFrameWithInfo(FrameworkElement frameElement, string infoText)
        {
            var resultPanel = new StackPanel()
            {
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Colors.LightGray)
            };

            resultPanel.Children.Add(new TextBlock()
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = infoText
            });

            resultPanel.Children.Add(new Border()
            {
                Margin = new Thickness(5, 0, 5, 5),
                BorderThickness = new Thickness(5),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Child = frameElement
            });

            return resultPanel;
        }

        private void RenderResultFrames(string fileInfoText, IEnumerable<FrameworkElement> resultFrames)
        {
            this.MainPanel.Children.Add(new TextBlock()
            {
                Text = String.Concat(fileInfoText, ": "),
                Margin = new Thickness(0, 13, 0, 0),
                FontWeight = FontWeights.Bold
            });

            if (resultFrames != null)
            {
                var horizontalPanel = new StackPanel()
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal
                };

                foreach (var frame in resultFrames)
                {
                    horizontalPanel.Children.Add(frame);
                }

                this.MainPanel.Children.Add(horizontalPanel);
            }
        }

        private string[] GetTestTiffFileNames()
        {
            return new string[]
            {
                "bitmap-zip-pc.tif",
                "cas.tif",
                "CCITT_1.TIF",
                "CCITT_2.TIF",
                "CCITT_3.TIF",
                "CCITT_4.TIF",
                "CCITT_5.TIF",
                "CCITT_6.TIF",
                "CCITT_7.TIF",
                "CCITT_8.TIF",
                "cmyk8-lzw.tif",
                "color64-lzw-mac.tif",
                "color64-lzw-pc.tif",
                "cramps-tile.tif",
                "cramps.tif",
                "dscf0013.tif",
                "fax2d.tif",
                "FLAG_T24.TIF",
                "G31D.TIF",
                "G31DS.TIF",
                "G32D.TIF",
                "G32DS.TIF",
                "g3test.tif",
                "G4.TIF",
                "G4S.TIF",
                "GMARBLES.TIF",
                "gray8-lzw-mac.tif",
                "gray8-packbits-be.tif",
                "gray8-packbits-le.tif",
                "gray8-zip-pc.tif",
                "jello.tif",
                "jim___ah.tif",
                "jim___cg.tif",
                "jim___dg.tif",
                "jim___gg.tif",
                "lab8-lzw.tif",
                "MARBIBM.TIF",
                "MARBLES.TIF",
                "multipage.tif",
                "oxford.tif",
                "penguin_jpeg.tif",
                "pc260001.tif",
                "quad-jpeg.tif",
                "quad-lzw.tif",
                "quad-tile.tif",
                "rgb8-jpeg-RGB.tif",
                "rgb8-jpeg-YCrCb.tif",
                "rgb8-lsb2msb.tif",
                "rgb8-lzw-mac.tif",
                "rgb8-lzw-pc.tif",
                "rgb8-msb2lsb.tif",
                "rgb8-separate.tif",
                "rgb8-zip-mac.tif",
                "rgb8-zip-pc.tif",
                "strike.tif",
                "tiger-minisblack-strip-01.tif",
                "tiger-minisblack-strip-02.tif",
                "tiger-minisblack-strip-04.tif",
                "tiger-minisblack-strip-08.tif",
                "tiger-minisblack-tile-01.tif",
                "tiger-minisblack-tile-02.tif",
                "tiger-minisblack-tile-04.tif",
                "tiger-minisblack-tile-08.tif",
                "tiger-palette-strip-01.tif",
                "tiger-palette-strip-02.tif",
                "tiger-palette-strip-03.tif",
                "tiger-palette-strip-04.tif",
                "tiger-palette-strip-05.tif",
                "tiger-palette-strip-06.tif",
                "tiger-palette-strip-07.tif",
                "tiger-palette-strip-08.tif",
                "tiger-palette-tile-01.tif",
                "tiger-palette-tile-02.tif",
                "tiger-palette-tile-03.tif",
                "tiger-palette-tile-04.tif",
                "tiger-palette-tile-05.tif",
                "tiger-palette-tile-06.tif",
                "tiger-palette-tile-07.tif",
                "tiger-palette-tile-08.tif",
                    "tiger-rgb-strip-contig-01.tif", // unsupported by LibTiff's ReadRGBAImage method
                    "tiger-rgb-strip-contig-02.tif", // unsupported by LibTiff's ReadRGBAImage method
                    "tiger-rgb-strip-contig-04.tif", // unsupported by LibTiff's ReadRGBAImage method
                "tiger-rgb-strip-contig-08.tif",
                "tiger-rgb-strip-planar-08.tif",
                    "tiger-rgb-tile-contig-01.tif", // unsupported by LibTiff's ReadRGBAImage method
                    "tiger-rgb-tile-contig-02.tif", // unsupported by LibTiff's ReadRGBAImage method
                    "tiger-rgb-tile-contig-04.tif", // unsupported by LibTiff's ReadRGBAImage method
                "tiger-rgb-tile-contig-08.tif",
                "tiger-rgb-tile-planar-08.tif",
                "tiger-separated-strip-contig-08.tif",
                    "tiger-separated-strip-planar-08.tif", // unsupported by LibTiff's ReadRGBAImage method
                "XING_T24.TIF",
                "ycbcr-cat.tif",
            };
        }
    }
}
