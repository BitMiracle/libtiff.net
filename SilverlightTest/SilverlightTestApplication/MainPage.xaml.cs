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
                                    var imageDepthField = tiffWorker.GetField(TiffTag.IMAGEDEPTH);
                                    var samplesPerPixelField = tiffWorker.GetField(TiffTag.SAMPLESPERPIXEL);

                                    int frameWidth = frameWidthField != null && frameWidthField.Length > 0 ? frameWidthField[0].ToInt() : 0;
                                    int frameHeight = frameHeightField != null && frameHeightField.Length > 0 ? frameHeightField[0].ToInt() : 0;
                                    var compression = compressionField != null && compressionField.Length > 0 ? (Compression)compressionField[0].Value : Compression.NONE;
                                    var imageDepth = imageDepthField != null && imageDepthField.Length > 0 ? imageDepthField[0].ToString() : String.Empty;
                                    var samplesPerPixel = samplesPerPixelField != null && samplesPerPixelField.Length > 0 ? samplesPerPixelField[0].ToString() : String.Empty;

                                    FrameworkElement resultFrame = null;

                                    switch (compression)
                                    {
                                        case Compression.CCITT_T4:
                                        case Compression.CCITT_T6:
                                        case Compression.CCITTRLE:
                                        case Compression.CCITTRLEW:

                                            try
                                            {
                                                resultFrame = this.CreateImage(this.CreateBmpFrom2Bit(tiffWorker, frameWidth, frameHeight));
                                            }
                                            catch (Exception exc)
                                            {
                                                resultFrame = this.CreateErrorMessageTextBlock(exc);
                                            }

                                            break;

                                        case Compression.ADOBE_DEFLATE:
                                        case Compression.DCS:
                                        case Compression.DEFLATE:
                                        case Compression.IT8BL:
                                        case Compression.IT8CTPAD:
                                        case Compression.IT8LW:
                                        case Compression.IT8MP:
                                        case Compression.JBIG:
                                        case Compression.JP2000:
                                        case Compression.JPEG:
                                        case Compression.LZW:
                                        case Compression.NEXT:
                                        case Compression.NONE:
                                        case Compression.OJPEG:
                                        case Compression.PACKBITS:
                                        case Compression.PIXARFILM:
                                        case Compression.PIXARLOG:
                                        case Compression.SGILOG:
                                        case Compression.SGILOG24:
                                        case Compression.THUNDERSCAN:

                                            try
                                            {
                                                resultFrame = this.CreateImage(this.CreateBmpFromRgba(tiffWorker, frameWidth, frameHeight));
                                            }
                                            catch (Exception exc)
                                            {
                                                resultFrame = this.CreateErrorMessageTextBlock(exc);
                                            }

                                            break;
                                            
                                        default:

                                            resultFrame = this.CreateErrorMessageTextBlock("Compression not supported.");

                                            break;
                                    }

                                    resultFramesWithInfo[i] = this.CreateResultFrameWithInfo(i, frameWidth, frameHeight, tileCount, stripCount, compression.ToString(), String.Concat("[", samplesPerPixel, " spp]"), resultFrame);
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

        private WriteableBitmap CreateBmpFrom2Bit(Tiff tiffWorker, int frameWidth, int frameHeight)
        {
            var imageByteRows = new List<byte[]>(frameHeight);
            for (int row = 0; row < frameHeight; row++)
            {
                //byte[] buffer = new byte[frameWidth / 8 + 1];
                int size = tiffWorker.ScanlineSize();
                byte[] buffer = new byte[size];

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

            return bmp;
        }

        private WriteableBitmap CreateBmpFromRgba(Tiff tiffWorker, int frameWidth, int frameHeight)
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

                    bmp.Pixels[ybmp + x] = Tiff.GetB(currentValue) | Tiff.GetG(currentValue) << 8 | Tiff.GetR(currentValue) << 16 | Tiff.GetA(currentValue) << 24;
                }
            }

            return bmp;
        }

        private Image CreateImage(WriteableBitmap bmp)
        {
            return new Image()
            {
                Stretch = Stretch.None,
                Source = bmp,
                Width = bmp.PixelWidth,
                Height = bmp.PixelHeight
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

        private FrameworkElement CreateResultFrameWithInfo(int frameNo, int frameWidth, int frameHeight, int tileCount, int stripCount, string compressionInfo, string additionalInfo, FrameworkElement frameElement)
        {
            var resultPanel = new StackPanel()
            {
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush(Colors.LightGray)
            };

            resultPanel.Children.Add(new TextBlock()
            {
                Margin = new Thickness(5, 0, 0, 0),
                Text = String.Concat("Frame ", frameNo, ", ", frameWidth, "x", frameHeight, ", ", tileCount, " tile(s), ", stripCount, " strip(s), ", compressionInfo, " ", additionalInfo)
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
                    "tiger-rgb-strip-contig-01.tif", // unsupported by LibTiff
                    "tiger-rgb-strip-contig-02.tif", // unsupported by LibTiff
                    "tiger-rgb-strip-contig-04.tif", // unsupported by LibTiff
                "tiger-rgb-strip-contig-08.tif",
                "tiger-rgb-strip-planar-08.tif",
                    "tiger-rgb-tile-contig-01.tif", // unsupported by LibTiff
                    "tiger-rgb-tile-contig-02.tif", // unsupported by LibTiff
                    "tiger-rgb-tile-contig-04.tif", // unsupported by LibTiff
                "tiger-rgb-tile-contig-08.tif",
                "tiger-rgb-tile-planar-08.tif",
                "tiger-separated-strip-contig-08.tif",
                    "tiger-separated-strip-planar-08.tif", // unsupported by LibTiff
                "XING_T24.TIF",
                "ycbcr-cat.tif",
            };
        }
    }
}
