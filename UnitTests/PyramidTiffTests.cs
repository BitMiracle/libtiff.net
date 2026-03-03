using System;
using System.IO;
using BitMiracle.LibTiff.Classic;
using NUnit.Framework;

namespace UnitTests
{
    public static class TestImages
    {
        public static byte[] CreateTileBytes(int tileSize, int channels)
        {
            // Size * size pixels * channels, interleaved
            var arr = new byte[tileSize * tileSize * channels];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = (byte)(i % 256);
            return arr;
        }
    }

    [TestFixture]
    public class PyramidTiffTests
    {
        private const int TileSize = 256;

        [TestCase(1)]
        [TestCase(2)]
        public void CanCreateAndReadPyramidalTiffWithSubIfds_ParameterizedChannels(int channelCount)
        {
            string path = Path.Combine(TestContext.CurrentContext.WorkDirectory,
                $"pyramid_test_{channelCount}ch.tif");

            if (File.Exists(path))
                File.Delete(path);

            // ---------- WRITE ----------
            using (var tiff = Tiff.Open(path, "w"))
            {
                // IFD0
                WriteLevel(tiff, 1024, 1024, channelCount);
                tiff.SetField(TiffTag.SUBIFD, 2, new long[2]);
                tiff.WriteDirectory();

                // SubIFD #1
                tiff.CreateDirectory();
                WriteLevel(tiff, 512, 512, channelCount);
                tiff.WriteDirectory();

                // SubIFD #2
                tiff.CreateDirectory();
                WriteLevel(tiff, 256, 256, channelCount);
                tiff.WriteDirectory();
            }

            // ---------- READ ----------
            using var read = Tiff.Open(path, "r");

            // IFD0
            Assert.AreEqual(1024, read.GetField(TiffTag.IMAGEWIDTH)[0].ToInt());
            Assert.AreEqual(1024, read.GetField(TiffTag.IMAGELENGTH)[0].ToInt());
            Assert.AreEqual(channelCount, read.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt(),
                "IFD0 channel count mismatch");

            var field = read.GetField(TiffTag.SUBIFD);
            Assert.NotNull(field, "Missing SUBIFD tag");

            long[] subIfds = (long[])field[1].Value;
            Assert.AreEqual(2, subIfds.Length);

            // SubIFD1
            read.SetSubDirectory(subIfds[0]);
            Assert.AreEqual(512, read.GetField(TiffTag.IMAGEWIDTH)[0].ToInt());
            Assert.AreEqual(512, read.GetField(TiffTag.IMAGELENGTH)[0].ToInt());
            Assert.AreEqual(channelCount, read.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt(),
                "SubIFD1 channel count mismatch");

            // SubIFD2
            read.SetSubDirectory(subIfds[1]);
            Assert.AreEqual(256, read.GetField(TiffTag.IMAGEWIDTH)[0].ToInt());
            Assert.AreEqual(256, read.GetField(TiffTag.IMAGELENGTH)[0].ToInt());
            Assert.AreEqual(channelCount, read.GetField(TiffTag.SAMPLESPERPIXEL)[0].ToInt(),
                "SubIFD2 channel count mismatch");
        }

        private static void WriteLevel(Tiff tiff, int width, int height, int channelCount)
        {
            tiff.SetField(TiffTag.IMAGEWIDTH, width);
            tiff.SetField(TiffTag.IMAGELENGTH, height);
            tiff.SetField(TiffTag.BITSPERSAMPLE, 8);
            tiff.SetField(TiffTag.SAMPLESPERPIXEL, channelCount);
            tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
            tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);

            tiff.SetField(TiffTag.TILEWIDTH, TileSize);
            tiff.SetField(TiffTag.TILELENGTH, TileSize);

            int tilesX = (width + TileSize - 1) / TileSize;
            int tilesY = (height + TileSize - 1) / TileSize;

            byte[] tile = TestImages.CreateTileBytes(TileSize, channelCount);

            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    int tileIndex = tiff.ComputeTile(tx * TileSize, ty * TileSize, 0, 0);
                    tiff.WriteEncodedTile(tileIndex, tile, tile.Length);
                }
            }
        }
    }
}