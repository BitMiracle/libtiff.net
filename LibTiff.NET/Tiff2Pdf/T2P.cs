using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using BitMiracle.LibTiff;

namespace BitMiracle.Tiff2Pdf
{
    /// <summary>
    /// This is the context of a function to generate PDF from a TIFF.
    /// </summary>
    class T2P
    {
        public bool m_error;

        public t2p_compress_t m_pdf_defaultcompression;
        public UInt16 m_pdf_defaultcompressionquality;

        public bool m_pdf_nopassthrough;
        public bool m_pdf_colorspace_invert;
        public bool m_pdf_fitwindow;
        public bool m_pdf_image_interpolate; /* false (default) : do not interpolate, true : interpolate */

        public bool m_pdf_centimeters;
        public bool m_pdf_overrideres;
        public bool m_pdf_overridepagesize;

        public float m_pdf_defaultxres;
        public float m_pdf_defaultyres;

        public float m_pdf_defaultpagewidth;
        public float m_pdf_defaultpagelength;

        public byte[] m_pdf_datetime;
        public byte[] m_pdf_creator;
        public byte[] m_pdf_author;
        public byte[] m_pdf_title;
        public byte[] m_pdf_subject;
        public byte[] m_pdf_keywords;

        /* fields for custom read/write procedures */
        public Stream m_outputfile;
        public bool m_outputdisable;
        public int m_outputwritten;

        public Tiff2PdfErrorHandler m_errorHandler;
        public Tiff2PdfStream m_stream;

        private T2P_PAGE[] m_tiff_pages;
        private T2P_TILES[] m_tiff_tiles;
        private UInt16 m_tiff_pagecount;
        private UInt16 m_tiff_compression;
        private UInt16 m_tiff_photometric;
        private UInt16 m_tiff_fillorder;
        private UInt16 m_tiff_bitspersample;
        private UInt16 m_tiff_samplesperpixel;
        private UInt16 m_tiff_planar;
        private uint m_tiff_width;
        private uint m_tiff_length;
        private float m_tiff_xres;
        private float m_tiff_yres;
        private UInt16 m_tiff_orientation;
        private int m_tiff_datasize;
        private UInt16 m_tiff_resunit;
        
        private float m_pdf_xres;
        private float m_pdf_yres;
        
        private float m_pdf_pagewidth;
        private float m_pdf_pagelength;
        private float m_pdf_imagewidth;
        private float m_pdf_imagelength;
        private T2P_BOX m_pdf_mediabox;
        private T2P_BOX m_pdf_imagebox;
        private UInt16 m_pdf_majorversion;
        private UInt16 m_pdf_minorversion;
        private uint m_pdf_catalog;
        private uint m_pdf_pages;
        private uint m_pdf_info;
        private uint m_pdf_palettecs;
        
        private int m_pdf_startxref;
        private byte[] m_pdf_fileid;
        
        private t2p_cs_t m_pdf_colorspace;
        
        private bool m_pdf_switchdecode;
        private UInt16 m_pdf_palettesize;
        private byte[] m_pdf_palette;
        private int[] m_pdf_labrange = new int[4];
        
        private t2p_compress_t m_pdf_compression;
        private UInt16 m_pdf_compressionquality;
        
        private t2p_transcode_t m_pdf_transcode;
        private t2p_sample_t m_pdf_sample;
        private int[] m_pdf_xrefoffsets;
        private uint m_pdf_xrefcount;
        private UInt16 m_pdf_page;
        private float[] m_tiff_whitechromaticities = new float[2];
        private float[] m_tiff_primarychromaticities = new float[6];
        private float[][] m_tiff_transferfunction = new float[3][];
        
        private UInt16 m_tiff_transferfunctioncount;
        private uint m_pdf_icccs;
        private uint m_tiff_iccprofilelength;
        private byte[] m_tiff_iccprofile;

        private Tiff m_output;

        public T2P()
        {
            m_errorHandler = new Tiff2PdfErrorHandler();
            Tiff.SetErrorHandler(m_errorHandler);

            m_stream = new Tiff2PdfStream();
            m_pdf_majorversion = 1;
            m_pdf_minorversion = 1;
            m_pdf_defaultxres = 300.0f;
            m_pdf_defaultyres = 300.0f;
            m_pdf_defaultpagewidth = 612.0f;
            m_pdf_defaultpagelength = 792.0f;
            m_pdf_xrefcount = 3; /* Catalog, Info, Pages */
        }
        
        /*
        This function validates the values of a T2P context struct pointer
        before calling write_pdf with it.
        */
        public void validate()
        {
            if (m_pdf_defaultcompression == t2p_compress_t.T2P_COMPRESS_JPEG)
            {
                if (m_pdf_defaultcompressionquality > 100 || m_pdf_defaultcompressionquality < 1)
                    m_pdf_defaultcompressionquality = 0;
            }
            
            if (m_pdf_defaultcompression == t2p_compress_t.T2P_COMPRESS_ZIP)
            {
                int m = m_pdf_defaultcompressionquality % 100;
                if (m_pdf_defaultcompressionquality / 100 > 9 || (m > 1 && m < 10) || m > 15)
                    m_pdf_defaultcompressionquality = 0;

                if (m_pdf_defaultcompressionquality % 100 != 0)
                {
                    m_pdf_defaultcompressionquality /= 100;
                    m_pdf_defaultcompressionquality *= 100;
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "PNG Group predictor differencing not implemented, assuming compression quality %u", m_pdf_defaultcompressionquality);
                }
                
                m_pdf_defaultcompressionquality %= 100;
                if (m_pdf_minorversion < 2)
                    m_pdf_minorversion = 2;
            }
        }

        /*

        This function writes a PDF to a file given a pointer to a TIFF.

        The idea with using a Tiff as output for a PDF file is that the file 
        can be created with ClientOpen for memory-mapped use within the TIFF 
        library, and WriteEncodedStrip can be used to write compressed data to 
        the output.  The output is not actually a TIFF file, it is a PDF file.  

        This function uses only writeToFile and WriteEncodedStrip to write to 
        the output TIFF file.  When libtiff would otherwise be writing data to the 
        output file, the write procedure of the TIFF structure is replaced with an 
        empty implementation.

        The first argument to the function is an initialized and validated T2P 
        context struct pointer.

        The second argument to the function is the Tiff that is the input that has 
        been opened for reading and no other functions have been called upon it.

        The third argument to the function is the Tiff that is the output that has 
        been opened for writing.  It has to be opened so that it hasn't written any 
        data to the output.  If the output is seekable then it's OK to seek to the 
        beginning of the file.  The function only writes to the output PDF and does 
        not seek.  See the example usage in the main() function.

        Tiff output = Open("output.pdf", "w");
        assert(output != null);

        if(output.tif_seekproc != null){
        t2pSeekFile(output, (toff_t) 0, SEEK_SET);
        }

        This function returns the file size of the output PDF file.  On error it 
        returns zero and the t2p.m_error variable is set to true.

        After this function completes, delete t2p, TIFFClose on input, 
        and TIFFClose on output.
        */
        public int write_pdf(Tiff input, Tiff output)
        {
            read_tiff_init(input);
            if (m_error)
                return 0;

            m_pdf_xrefoffsets = new int [m_pdf_xrefcount];
            if (m_pdf_xrefoffsets == null)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for write_pdf", m_pdf_xrefcount * sizeof(uint));
                return 0;
            }

            m_output = output;
            m_pdf_xrefcount = 0;
            m_pdf_catalog = 1;
            m_pdf_info = 2;
            m_pdf_pages = 3;

            int written = write_pdf_header();
            m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
            m_pdf_catalog = m_pdf_xrefcount;
            written += write_pdf_obj_start(m_pdf_xrefcount);
            written += write_pdf_catalog();
            written += write_pdf_obj_end();
            m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
            m_pdf_info = m_pdf_xrefcount;
            written += write_pdf_obj_start(m_pdf_xrefcount);
            written += write_pdf_info(input);
            written += write_pdf_obj_end();
            m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
            m_pdf_pages = m_pdf_xrefcount;
            written += write_pdf_obj_start(m_pdf_xrefcount);
            written += write_pdf_pages();
            written += write_pdf_obj_end();
            for (m_pdf_page = 0; m_pdf_page < m_tiff_pagecount; m_pdf_page++)
            {
                read_tiff_data(input);
                if (m_error)
                    return 0;

                m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                written += write_pdf_obj_start(m_pdf_xrefcount);
                written += write_pdf_page(m_pdf_xrefcount);
                written += write_pdf_obj_end();
                m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                written += write_pdf_obj_start(m_pdf_xrefcount);
                written += write_pdf_stream_dict_start();
                written += write_pdf_stream_dict(0, m_pdf_xrefcount + 1);
                written += write_pdf_stream_dict_end();
                written += write_pdf_stream_start();
                int streamlen = written;
                written += write_pdf_page_content_stream();
                streamlen = written - streamlen;
                written += write_pdf_stream_end();
                written += write_pdf_obj_end();
                m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                written += write_pdf_obj_start(m_pdf_xrefcount);
                written += write_pdf_stream_length(streamlen);
                written += write_pdf_obj_end();
                
                if (m_tiff_transferfunctioncount != 0)
                {
                    m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                    written += write_pdf_obj_start(m_pdf_xrefcount);
                    written += write_pdf_transfer();
                    written += write_pdf_obj_end();
                    
                    for (UInt16 i = 0; i < m_tiff_transferfunctioncount; i++)
                    {
                        m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                        written += write_pdf_obj_start(m_pdf_xrefcount);
                        written += write_pdf_stream_dict_start();
                        written += write_pdf_transfer_dict();
                        written += write_pdf_stream_dict_end();
                        written += write_pdf_stream_start();
                        streamlen = written;
                        written += write_pdf_transfer_stream(i);
                        streamlen = written - streamlen;
                        written += write_pdf_stream_end();
                        written += write_pdf_obj_end();
                    }
                }

                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_PALETTE) != 0)
                {
                    m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                    m_pdf_palettecs = m_pdf_xrefcount;
                    written += write_pdf_obj_start(m_pdf_xrefcount);
                    written += write_pdf_stream_dict_start();
                    written += write_pdf_stream_dict(m_pdf_palettesize, 0);
                    written += write_pdf_stream_dict_end();
                    written += write_pdf_stream_start();
                    streamlen = written;
                    written += write_pdf_xobject_palettecs_stream();
                    streamlen = written - streamlen;
                    written += write_pdf_stream_end();
                    written += write_pdf_obj_end();
                }

                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_ICCBASED) != 0)
                {
                    m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                    m_pdf_icccs = m_pdf_xrefcount;
                    written += write_pdf_obj_start(m_pdf_xrefcount);
                    written += write_pdf_stream_dict_start();
                    written += write_pdf_xobject_icccs_dict();
                    written += write_pdf_stream_dict_end();
                    written += write_pdf_stream_start();
                    streamlen = written;
                    written += write_pdf_xobject_icccs_stream();
                    streamlen = written - streamlen;
                    written += write_pdf_stream_end();
                    written += write_pdf_obj_end();
                }

                if (m_tiff_tiles[m_pdf_page].tiles_tilecount != 0)
                {
                    for (uint i2 = 0; i2 < m_tiff_tiles[m_pdf_page].tiles_tilecount; i2++)
                    {
                        m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                        written += write_pdf_obj_start(m_pdf_xrefcount);
                        written += write_pdf_stream_dict_start();
                        written += write_pdf_xobject_stream_dict(i2 + 1);
                        written += write_pdf_stream_dict_end();
                        written += write_pdf_stream_start();
                        streamlen = written;
                        read_tiff_size_tile(input, i2);
                        written += readwrite_pdf_image_tile(input, i2);
                        write_advance_directory();
                        if (m_error)
                            return 0;

                        streamlen = written - streamlen;
                        written += write_pdf_stream_end();
                        written += write_pdf_obj_end();
                        m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                        written += write_pdf_obj_start(m_pdf_xrefcount);
                        written += write_pdf_stream_length(streamlen);
                        written += write_pdf_obj_end();
                    }
                }
                else
                {
                    m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                    written += write_pdf_obj_start(m_pdf_xrefcount);
                    written += write_pdf_stream_dict_start();
                    written += write_pdf_xobject_stream_dict(0);
                    written += write_pdf_stream_dict_end();
                    written += write_pdf_stream_start();
                    streamlen = written;
                    read_tiff_size(input);
                    written += readwrite_pdf_image(input);
                    write_advance_directory();
                    
                    if (m_error)
                        return 0;

                    streamlen = written - streamlen;
                    written += write_pdf_stream_end();
                    written += write_pdf_obj_end();
                    m_pdf_xrefoffsets[m_pdf_xrefcount++] = written;
                    written += write_pdf_obj_start(m_pdf_xrefcount);
                    written += write_pdf_stream_length(streamlen);
                    written += write_pdf_obj_end();
                }
            }

            m_pdf_startxref = written;
            written += write_pdf_xreftable();
            written += write_pdf_trailer();
            disable(output);
            return written;
        }

        /*
        This function scans the input TIFF file for pages.  It attempts
        to determine which IFD's of the TIFF file contain image document
        pages.  For each, it gathers some information that has to do
        with the output of the PDF document as a whole.  
        */
        private void read_tiff_init(Tiff input)
        {
            UInt16 directorycount = input.NumberOfDirectories();
            m_tiff_pages = new T2P_PAGE [directorycount];
            if (m_tiff_pages == null)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for tiff_pages array, %s", directorycount * sizeof(T2P_PAGE), input.FileName());
                m_error = true;
                return;
            }

            memset(m_tiff_pages, 0x00, directorycount * sizeof(T2P_PAGE));
            m_tiff_tiles = new T2P_TILES [directorycount];
            if (m_tiff_tiles == null)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for tiff_tiles array, %s", directorycount * sizeof(T2P_TILES), input.FileName());
                m_error = true;
                return;
            }

            memset(m_tiff_tiles, 0x00, directorycount * sizeof(T2P_TILES));
            for (UInt16 i = 0; i < directorycount; i++)
            {
                uint subfiletype = 0;

                if (!input.SetDirectory(i))
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't set directory %u of input file %s", i, input.FileName());
                    return ;
                }

                UInt16 pagen = 0;
                UInt16 paged = 0;
                if (input.GetField(TIFFTAG_PAGENUMBER, &pagen, &paged))
                {
                    if ((pagen > paged) && (paged != 0))
                        m_tiff_pages[m_tiff_pagecount].page_number = paged;
                    else
                        m_tiff_pages[m_tiff_pagecount].page_number = pagen;
                }
                else
                {
                    if (input.GetField(TIFFTAG_SUBFILETYPE, &subfiletype))
                    {
                        if (((subfiletype & FILETYPE_PAGE) == 0) && (subfiletype != 0))
                            continue;
                    }
                    else if (input.GetField(TIFFTAG_OSUBFILETYPE, &subfiletype))
                    {
                        if ((subfiletype != OFILETYPE_IMAGE) && (subfiletype != OFILETYPE_PAGE) && (subfiletype != 0))
                            continue;
                    }

                    m_tiff_pages[m_tiff_pagecount].page_number = m_tiff_pagecount;
                }

                m_tiff_pages[m_tiff_pagecount].page_directory = i;

                if (input.IsTiled())
                    m_tiff_pages[m_tiff_pagecount].page_tilecount = input.NumberOfTiles();
                
                m_tiff_pagecount++;
            }

            qsort((void*)m_tiff_pages, m_tiff_pagecount, sizeof(T2P_PAGE), cmp_t2p_page);

            for (UInt16 i = 0; i < m_tiff_pagecount; i++)
            {
                m_pdf_xrefcount += 5;
                input.SetDirectory(m_tiff_pages[i].page_directory);

                UInt16 xuint16 = 0;
                if ((input.GetField(TIFFTAG_PHOTOMETRIC, &xuint16) && (xuint16 == PHOTOMETRIC_PALETTE)) || input.GetField(TIFFTAG_INDEXED, &xuint16))
                {
                    m_tiff_pages[i].page_extra++;
                    m_pdf_xrefcount++;
                }

                if (input.GetField(TIFFTAG_COMPRESSION, &xuint16))
                {
                    if ((xuint16 == COMPRESSION_DEFLATE || xuint16 == COMPRESSION_ADOBE_DEFLATE) && ((m_tiff_pages[i].page_tilecount != 0) || input.NumberOfStrips() == 1) && !m_pdf_nopassthrough)
                    {
                        if (m_pdf_minorversion < 2)
                            m_pdf_minorversion = 2;
                    }
                }

                if (input.GetField(TIFFTAG_TRANSFERFUNCTION, &m_tiff_transferfunction[0], &m_tiff_transferfunction[1], &m_tiff_transferfunction[2]))
                {
                    if (m_tiff_transferfunction[1] != m_tiff_transferfunction[0])
                    {
                        m_tiff_transferfunctioncount = 3;
                        m_tiff_pages[i].page_extra += 4;
                        m_pdf_xrefcount += 4;
                    }
                    else
                    {
                        m_tiff_transferfunctioncount = 1;
                        m_tiff_pages[i].page_extra += 2;
                        m_pdf_xrefcount += 2;
                    }

                    if (m_pdf_minorversion < 2)
                        m_pdf_minorversion = 2;
                }
                else
                {
                    m_tiff_transferfunctioncount = 0;
                }

                if (input.GetField(TIFFTAG_ICCPROFILE, &m_tiff_iccprofilelength, &m_tiff_iccprofile))
                {
                    m_tiff_pages[i].page_extra++;
                    m_pdf_xrefcount++;
                    if (m_pdf_minorversion < 3)
                        m_pdf_minorversion = 3;
                }

                m_tiff_tiles[i].tiles_tilecount = m_tiff_pages[i].page_tilecount;

                if ((input.GetField(TIFFTAG_PLANARCONFIG, &xuint16) != 0) && (xuint16 == PLANARCONFIG_SEPARATE))
                {
                    input.GetField(TIFFTAG_SAMPLESPERPIXEL, &xuint16);
                    m_tiff_tiles[i].tiles_tilecount /= xuint16;
                }
                
                if (m_tiff_tiles[i].tiles_tilecount > 0)
                {
                    m_pdf_xrefcount += (m_tiff_tiles[i].tiles_tilecount - 1) * 2;
                    input.GetField(TIFFTAG_TILEWIDTH, &m_tiff_tiles[i].tiles_tilewidth);
                    input.GetField(TIFFTAG_TILELENGTH, &m_tiff_tiles[i].tiles_tilelength);
                    m_tiff_tiles[i].tiles_tiles = new T2P_TILE [m_tiff_tiles[i].tiles_tilecount];
                    if (m_tiff_tiles[i].tiles_tiles == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for read_tiff_init, %s", m_tiff_tiles[i].tiles_tilecount * sizeof(T2P_TILE), input.FileName());
                        m_error = true;
                        return ;
                    }
                }
            }
        }

        /*
        This function sets the input directory to the directory of a given
        page and determines information about the image.  It checks
        the image characteristics to determine if it is possible to convert
        the image data into a page of PDF output, setting values of the T2P
        struct for this page.  It determines what color space is used in
        the output PDF to represent the image.

        It determines if the image can be converted as raw data without
        requiring transcoding of the image data.
        */
        private void read_tiff_data(Tiff input)
        {
            m_pdf_transcode = T2P_TRANSCODE_ENCODE;
            m_pdf_sample = T2P_SAMPLE_NOTHING;
            m_pdf_switchdecode = m_pdf_colorspace_invert;

            input.SetDirectory(m_tiff_pages[m_pdf_page].page_directory);

            input.GetField(TIFFTAG_IMAGEWIDTH, &m_tiff_width);
            if (m_tiff_width == 0)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with zero width", input.FileName());
                m_error = true;
                return ;
            }

            input.GetField(TIFFTAG_IMAGELENGTH, &m_tiff_length);
            if (m_tiff_length == 0)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with zero length", input.FileName());
                m_error = true;
                return ;
            }

            if (input.GetField(TIFFTAG_COMPRESSION, &m_tiff_compression) == 0)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with no compression tag", input.FileName());
                m_error = true;
                return ;

            }
            if (input.IsCodecConfigured(m_tiff_compression) == 0)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with compression type %u:  not configured", input.FileName(), m_tiff_compression);
                m_error = true;
                return ;

            }

            input.GetFieldDefaulted(TIFFTAG_BITSPERSAMPLE, &m_tiff_bitspersample);
            
            switch (m_tiff_bitspersample)
            {
            case 1:
            case 2:
            case 4:
            case 8:
                break;
            case 0:
                Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Image %s has 0 bits per sample, assuming 1", input.FileName());
                m_tiff_bitspersample = 1;
                break;
            default:
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with %u bits per sample", input.FileName(), m_tiff_bitspersample);
                m_error = true;
                return ;
            }

            input.GetFieldDefaulted(TIFFTAG_SAMPLESPERPIXEL, &m_tiff_samplesperpixel);
            if (m_tiff_samplesperpixel > 4)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with %u samples per pixel", input.FileName(), m_tiff_samplesperpixel);
                m_error = true;
                return ;
            }

            if (m_tiff_samplesperpixel == 0)
            {
                Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Image %s has 0 samples per pixel, assuming 1", input.FileName());
                m_tiff_samplesperpixel = 1;
            }

            UInt16 xuint16;
            if (input.GetField(TIFFTAG_SAMPLEFORMAT, &xuint16) != 0)
            {
                switch (xuint16)
                {
                case 0:
                case 1:
                case 4:
                    break;
                default:
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with sample format %u", input.FileName(), xuint16);
                    m_error = true;
                    return ;
                    break;
                }
            }

            input.GetFieldDefaulted(TIFFTAG_FILLORDER, &m_tiff_fillorder);

            if (input.GetField(TIFFTAG_PHOTOMETRIC, &m_tiff_photometric) == 0)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with no photometric interpretation tag", input.FileName());
                m_error = true;
                return ;

            }

            UInt16* r;
            UInt16* g;
            UInt16* b;
            UInt16* a;
            bool photometric_palette;
            bool photometric_palette_cmyk;

            switch (m_tiff_photometric)
            {
            case PHOTOMETRIC_MINISWHITE:
            case PHOTOMETRIC_MINISBLACK:
                if (m_tiff_bitspersample == 1)
                {
                    m_pdf_colorspace = t2p_cs_t.T2P_CS_BILEVEL;
                    if (m_tiff_photometric == PHOTOMETRIC_MINISWHITE)
                        m_pdf_switchdecode ^= 1;
                }
                else
                {
                    m_pdf_colorspace = t2p_cs_t.T2P_CS_GRAY;
                    if (m_tiff_photometric == PHOTOMETRIC_MINISWHITE)
                        m_pdf_switchdecode ^= 1;
                }
                break;
           
            case PHOTOMETRIC_RGB:
            case PHOTOMETRIC_PALETTE:
                photometric_palette = (m_tiff_photometric == PHOTOMETRIC_PALETTE);
                if (!photometric_palette)
                {
                    m_pdf_colorspace = t2p_cs_t.T2P_CS_RGB;
                    if (m_tiff_samplesperpixel == 3)
                        break;

                    if (input.GetField(TIFFTAG_INDEXED, &xuint16))
                    {
                        if (xuint16 == 1)
                            photometric_palette = true;
                    }
                }

                if (!photometric_palette)
                {
                    if (m_tiff_samplesperpixel > 3)
                    {
                        if (m_tiff_samplesperpixel == 4)
                        {
                            m_pdf_colorspace = t2p_cs_t.T2P_CS_RGB;

                            UInt16* xuint16p;
                            if (input.GetField(TIFFTAG_EXTRASAMPLES, &xuint16, &xuint16p) && xuint16 == 1)
                            {
                                if (xuint16p[0] == EXTRASAMPLE_ASSOCALPHA)
                                {
                                    m_pdf_sample = T2P_SAMPLE_RGBAA_TO_RGB;
                                    break;
                                }

                                if (xuint16p[0] == EXTRASAMPLE_UNASSALPHA)
                                {
                                    m_pdf_sample = T2P_SAMPLE_RGBA_TO_RGB;
                                    break;
                                }
                                
                                Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "RGB image %s has 4 samples per pixel, assuming RGBA", input.FileName());
                                break;
                            }

                            m_pdf_colorspace = t2p_cs_t.T2P_CS_CMYK;
                            m_pdf_switchdecode ^= 1;
                            Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "RGB image %s has 4 samples per pixel, assuming inverse CMYK", input.FileName());
                            break;
                        }
                        else
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for RGB image %s with %u samples per pixel", input.FileName(), m_tiff_samplesperpixel);
                            m_error = true;
                            break;
                        }
                    }
                    else
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for RGB image %s with %u samples per pixel", input.FileName(), m_tiff_samplesperpixel);
                        m_error = true;
                        break;
                    }
                }

                if (photometric_palette)
                {
                    if (m_tiff_samplesperpixel != 1)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for palettized image %s with not one sample per pixel", input.FileName());
                        m_error = true;
                        return ;
                    }

                    m_pdf_colorspace = (t2p_cs_t)(t2p_cs_t.T2P_CS_RGB | t2p_cs_t.T2P_CS_PALETTE);
                    m_pdf_palettesize = 0x0001 << m_tiff_bitspersample;

                    if (!input.GetField(TIFFTAG_COLORMAP, &r, &g, &b))
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Palletized image %s has no color map", input.FileName());
                        m_error = true;
                        return ;
                    }

                    delete m_pdf_palette;
                    m_pdf_palette = new byte [m_pdf_palettesize * 3];

                    if (m_pdf_palette == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for t2p_read_tiff_image, %s", m_pdf_palettesize, input.FileName());
                        m_error = true;
                        return ;
                    }

                    for (int i = 0; i < m_pdf_palettesize; i++)
                    {
                        m_pdf_palette[i * 3] = (byte)(r[i] >> 8);
                        m_pdf_palette[i * 3 + 1] = (byte)(g[i] >> 8);
                        m_pdf_palette[i * 3 + 2] = (byte)(b[i] >> 8);
                    }

                    m_pdf_palettesize *= 3;
                }
                break;

            case PHOTOMETRIC_SEPARATED:
                photometric_palette_cmyk = false;
                if (input.GetField(TIFFTAG_INDEXED, &xuint16))
                {
                    if (xuint16 == 1)
                        photometric_palette_cmyk = true;
                }

                if (!photometric_palette_cmyk)
                {
                    if (input.GetField(TIFFTAG_INKSET, &xuint16))
                    {
                        if (xuint16 != INKSET_CMYK)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s because its inkset is not CMYK", input.FileName());
                            m_error = true;
                            return ;
                        }
                    }
                    
                    if (m_tiff_samplesperpixel == 4)
                    {
                        m_pdf_colorspace = t2p_cs_t.T2P_CS_CMYK;
                    }
                    else
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s because it has %u samples per pixel", input.FileName(), m_tiff_samplesperpixel);
                        m_error = true;
                        return ;
                    }
                }
                else
                {
                    if (m_tiff_samplesperpixel != 1)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for palletized CMYK image %s with not one sample per pixel", input.FileName());
                        m_error = true;
                        return ;
                    }
                    
                    m_pdf_colorspace = (t2p_cs_t)(t2p_cs_t.T2P_CS_CMYK | t2p_cs_t.T2P_CS_PALETTE);
                    m_pdf_palettesize = 0x0001 << m_tiff_bitspersample;
                    
                    if (!input.GetField(TIFFTAG_COLORMAP, &r, &g, &b, &a))
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Palletized image %s has no color map", input.FileName());
                        m_error = true;
                        return ;
                    }
                    
                    delete m_pdf_palette;
                    m_pdf_palette = new byte [m_pdf_palettesize * 4];
                    
                    if (m_pdf_palette == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for t2p_read_tiff_image, %s", m_pdf_palettesize, input.FileName());
                        m_error = true;
                        return ;
                    }
                    
                    for (int i = 0; i < m_pdf_palettesize; i++)
                    {
                        m_pdf_palette[i * 4] = (byte)(r[i] >> 8);
                        m_pdf_palette[i * 4 + 1] = (byte)(g[i] >> 8);
                        m_pdf_palette[i * 4 + 2] = (byte)(b[i] >> 8);
                        m_pdf_palette[i * 4 + 3] = (byte)(a[i] >> 8);
                    }

                    m_pdf_palettesize *= 4;
                }
                break;
            
            case PHOTOMETRIC_YCBCR:
                m_pdf_colorspace = t2p_cs_t.T2P_CS_RGB;
                if (m_tiff_samplesperpixel == 1)
                {
                    m_pdf_colorspace = t2p_cs_t.T2P_CS_GRAY;
                    m_tiff_photometric = PHOTOMETRIC_MINISBLACK;
                    break;
                }

                m_pdf_sample = T2P_SAMPLE_YCBCR_TO_RGB;
                if (m_pdf_defaultcompression == t2p_compress_t.T2P_COMPRESS_JPEG)
                    m_pdf_sample = T2P_SAMPLE_NOTHING;

                break;

            case PHOTOMETRIC_CIELAB:
                m_pdf_labrange[0] = -127;
                m_pdf_labrange[1] = 127;
                m_pdf_labrange[2] = -127;
                m_pdf_labrange[3] = 127;
                m_pdf_sample = T2P_SAMPLE_LAB_SIGNED_TO_UNSIGNED;
                m_pdf_colorspace = t2p_cs_t.T2P_CS_LAB;
                break;

            case PHOTOMETRIC_ICCLAB:
                m_pdf_labrange[0] = 0;
                m_pdf_labrange[1] = 255;
                m_pdf_labrange[2] = 0;
                m_pdf_labrange[3] = 255;
                m_pdf_colorspace = t2p_cs_t.T2P_CS_LAB;
                break;

            case PHOTOMETRIC_ITULAB:
                m_pdf_labrange[0] = -85;
                m_pdf_labrange[1] = 85;
                m_pdf_labrange[2] = -75;
                m_pdf_labrange[3] = 124;
                m_pdf_sample = T2P_SAMPLE_LAB_SIGNED_TO_UNSIGNED;
                m_pdf_colorspace = t2p_cs_t.T2P_CS_LAB;
                break;

            case PHOTOMETRIC_LOGL:
            case PHOTOMETRIC_LOGLUV:
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with photometric interpretation LogL/LogLuv", input.FileName());
                m_error = true;
                return ;
            default:
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with photometric interpretation %u", input.FileName(), m_tiff_photometric);
                m_error = true;
                return ;
            }

            if (input.GetField(TIFFTAG_PLANARCONFIG, &m_tiff_planar))
            {
                switch (m_tiff_planar)
                {
                case 0:
                    Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Image %s has planar configuration 0, assuming 1", input.FileName());
                    m_tiff_planar = PLANARCONFIG_CONTIG;

                case PLANARCONFIG_CONTIG:
                    break;
                
                case PLANARCONFIG_SEPARATE:
                    m_pdf_sample = T2P_SAMPLE_PLANAR_SEPARATE_TO_CONTIG;
                    if (m_tiff_bitspersample != 8)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with separated planar configuration and %u bits per sample", input.FileName(), m_tiff_bitspersample);
                        m_error = true;
                        return ;
                    }
                    break;
                
                default:
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with planar configuration %u", input.FileName(), m_tiff_planar);
                    m_error = true;
                    return ;
                }
            }

            input.GetFieldDefaulted(TIFFTAG_ORIENTATION, &(m_tiff_orientation));
            if (m_tiff_orientation > ORIENTATION_LEFTBOT)
            {
                Tiff.Warning(Tiff2PdfConstants.TIFF2PDF_MODULE, "Image %s has orientation %u, assuming 0", input.FileName(), m_tiff_orientation);
                m_tiff_orientation = 0;
            }

            if (input.GetField(TIFFTAG_XRESOLUTION, &m_tiff_xres) == 0)
                m_tiff_xres = 0.0;

            if (input.GetField(TIFFTAG_YRESOLUTION, &m_tiff_yres) == 0)
                m_tiff_yres = 0.0;

            input.GetFieldDefaulted(TIFFTAG_RESOLUTIONUNIT, &m_tiff_resunit);
            if (m_tiff_resunit == RESUNIT_CENTIMETER)
            {
                m_tiff_xres *= 2.54F;
                m_tiff_yres *= 2.54F;
            }
            else if (m_tiff_resunit != RESUNIT_INCH && m_pdf_centimeters)
            {
                m_tiff_xres *= 2.54F;
                m_tiff_yres *= 2.54F;
            }

            compose_pdf_page();

            m_pdf_transcode = T2P_TRANSCODE_ENCODE;
            if (!m_pdf_nopassthrough)
            {
                if (m_tiff_compression == COMPRESSION_CCITTFAX4)
                {
                    if (input.IsTiled() || (input.NumberOfStrips() == 1))
                    {
                        m_pdf_transcode = T2P_TRANSCODE_RAW;
                        m_pdf_compression = t2p_compress_t.T2P_COMPRESS_G4;
                    }
                }

                if (m_tiff_compression == COMPRESSION_ADOBE_DEFLATE || m_tiff_compression == COMPRESSION_DEFLATE)
                {
                    if (input.IsTiled() || (input.NumberOfStrips() == 1))
                    {
                        m_pdf_transcode = T2P_TRANSCODE_RAW;
                        m_pdf_compression = t2p_compress_t.T2P_COMPRESS_ZIP;
                    }
                }

                if (m_tiff_compression == COMPRESSION_JPEG)
                {
                    m_pdf_transcode = T2P_TRANSCODE_RAW;
                    m_pdf_compression = t2p_compress_t.T2P_COMPRESS_JPEG;
                }
            }

            if (m_pdf_transcode != T2P_TRANSCODE_RAW)
                m_pdf_compression = m_pdf_defaultcompression;

            if (m_pdf_defaultcompression == t2p_compress_t.T2P_COMPRESS_JPEG)
            {
                if (m_pdf_colorspace & t2p_cs_t.T2P_CS_PALETTE)
                {
                    m_pdf_sample = (t2p_sample_t)(m_pdf_sample | T2P_SAMPLE_REALIZE_PALETTE);
                    m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace ^ t2p_cs_t.T2P_CS_PALETTE);
                    m_tiff_pages[m_pdf_page].page_extra--;
                }
            }

            if (m_tiff_compression == COMPRESSION_JPEG)
            {
                if (m_tiff_planar == PLANARCONFIG_SEPARATE)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for %s with JPEG compression and separated planar configuration", input.FileName());
                    m_error = true;
                    return ;
                }
            }

            if ((m_pdf_sample & T2P_SAMPLE_REALIZE_PALETTE) != 0)
            {
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CMYK) != 0)
                {
                    m_tiff_samplesperpixel = 4;
                    m_tiff_photometric = PHOTOMETRIC_SEPARATED;
                }
                else
                {
                    m_tiff_samplesperpixel = 3;
                    m_tiff_photometric = PHOTOMETRIC_RGB;
                }
            }

            if (input.GetField(TIFFTAG_TRANSFERFUNCTION, &m_tiff_transferfunction[0], &m_tiff_transferfunction[1], &m_tiff_transferfunction[2]))
            {
                if (m_tiff_transferfunction[1] != m_tiff_transferfunction[0])
                    m_tiff_transferfunctioncount = 3;
                else
                    m_tiff_transferfunctioncount = 1;
            }
            else
            {
                m_tiff_transferfunctioncount = 0;
            }

            float* xfloatp;
            if (input.GetField(TIFFTAG_WHITEPOINT, &xfloatp))
            {
                m_tiff_whitechromaticities[0] = xfloatp[0];
                m_tiff_whitechromaticities[1] = xfloatp[1];
                
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_GRAY) != 0)
                    m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_CALGRAY);

                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_RGB) != 0)
                    m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_CALRGB);
            }
            
            if (input.GetField(TIFFTAG_PRIMARYCHROMATICITIES, &xfloatp) != 0)
            {
                m_tiff_primarychromaticities[0] = xfloatp[0];
                m_tiff_primarychromaticities[1] = xfloatp[1];
                m_tiff_primarychromaticities[2] = xfloatp[2];
                m_tiff_primarychromaticities[3] = xfloatp[3];
                m_tiff_primarychromaticities[4] = xfloatp[4];
                m_tiff_primarychromaticities[5] = xfloatp[5];
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_RGB) != 0)
                    m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_CALRGB);
            }

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_LAB) != 0)
            {
                if (input.GetField(TIFFTAG_WHITEPOINT, &xfloatp) != 0)
                {
                    m_tiff_whitechromaticities[0] = xfloatp[0];
                    m_tiff_whitechromaticities[1] = xfloatp[1];
                }
                else
                {
                    m_tiff_whitechromaticities[0] = 0.3457F; /* 0.3127F; */
                    m_tiff_whitechromaticities[1] = 0.3585F; /* 0.3290F; */
                }
            }

            if (input.GetField(TIFFTAG_ICCPROFILE, &m_tiff_iccprofilelength, &m_tiff_iccprofile))
            {
                m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_ICCBASED);
            }
            else
            {
                m_tiff_iccprofilelength = 0;
                m_tiff_iccprofile = null;
            }

            if (m_tiff_bitspersample == 1 && m_tiff_samplesperpixel == 1)
                m_pdf_compression = t2p_compress_t.T2P_COMPRESS_G4;
        }

        /*
        This function returns the necessary size of a data buffer to contain the raw or 
        uncompressed image data from the input TIFF for a page.
        */
        private void read_tiff_size(Tiff input)
        {
            if (m_pdf_transcode == T2P_TRANSCODE_RAW)
            {
                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_G4)
                {
                    uint* sbc = null;
                    input.GetField(TIFFTAG_STRIPBYTECOUNTS, &sbc);
                    m_tiff_datasize = sbc[0];
                    return ;
                }

                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_ZIP)
                {
                    uint* sbc = null;
                    input.GetField(TIFFTAG_STRIPBYTECOUNTS, &sbc);
                    m_tiff_datasize = sbc[0];
                    return ;
                }
                
                if (m_tiff_compression == COMPRESSION_JPEG)
                {
                    uint count = 0;
                    byte* jpt = null;
                    if (input.GetField(TIFFTAG_JPEGTABLES, &count, &jpt))
                    {
                        if (count > 4)
                        {
                            m_tiff_datasize += count;
                            m_tiff_datasize -= 2; /* don't use EOI of header */
                        }
                    }
                    else
                    {
                        m_tiff_datasize = 2; /* SOI for first strip */
                    }

                    uint stripcount = input.NumberOfStrips();
                    uint* sbc = null;
                    if (!input.GetField(TIFFTAG_STRIPBYTECOUNTS, &sbc))
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Input file %s missing field: TIFFTAG_STRIPBYTECOUNTS", input.FileName());
                        m_error = true;
                        return ;
                    }

                    for (uint i = 0; i < stripcount; i++)
                    {
                        m_tiff_datasize += sbc[i];
                        m_tiff_datasize -= 4; /* don't use SOI or EOI of strip */
                    }
                    
                    m_tiff_datasize += 2; /* use EOI of last strip */
                }
            }

            m_tiff_datasize = input.ScanlineSize() * m_tiff_length;
            if (m_tiff_planar == PLANARCONFIG_SEPARATE)
                m_tiff_datasize *= m_tiff_samplesperpixel;
        }
        
        /*
        This function returns the necessary size of a data buffer to contain the raw or 
        uncompressed image data from the input TIFF for a tile of a page.
        */
        private void read_tiff_size_tile(Tiff input, uint tile)
        {
            bool edge = false;
            edge |= tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile);
            edge |= tile_is_bottom_edge(m_tiff_tiles[m_pdf_page], tile);

            if (m_pdf_transcode == T2P_TRANSCODE_RAW)
            {
                if (edge && m_pdf_compression != t2p_compress_t.T2P_COMPRESS_JPEG)
                {
                    m_tiff_datasize = input.TileSize();
                    return;
                }
                else
                {
                    uint* tbc = null;
                    input.GetField(TIFFTAG_TILEBYTECOUNTS, &tbc);
                    m_tiff_datasize = tbc[tile];
                    if (m_tiff_compression == COMPRESSION_JPEG)
                    {
                        uint count = 0;
                        byte* jpt;
                        if (input.GetField(TIFFTAG_JPEGTABLES, &count, &jpt) != 0)
                        {
                            if (count > 4)
                            {
                                m_tiff_datasize += count;
                                m_tiff_datasize -= 4; /* don't use EOI of header or SOI of tile */
                            }
                        }
                    }
                    return;
                }
            }

            m_tiff_datasize = input.TileSize();
            if (m_tiff_planar == PLANARCONFIG_SEPARATE)
                m_tiff_datasize *= m_tiff_samplesperpixel;
        }
        
        /*
        This function reads the raster image data from the input TIFF for an image and writes 
        the data to the output PDF XObject image dictionary stream.  It returns the amount written 
        or zero on error.
        */
        private int readwrite_pdf_image(Tiff input)
        {
            byte* buffer = null;
            int bufferoffset = 0;
            uint stripcount = 0;
            uint max_striplength = 0;

            if (m_pdf_transcode == T2P_TRANSCODE_RAW)
            {
                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_G4)
                {
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    input.ReadRawStrip(0, buffer, 0, m_tiff_datasize);
                    if (m_tiff_fillorder == FILLORDER_LSB2MSB)
                    {
                        /*
                        * make sure is lsb-to-msb
                        * bit-endianness fill order
                        */
                        Tiff.ReverseBits(buffer, m_tiff_datasize);
                    }
                    
                    writeToFile(buffer, m_tiff_datasize);
                    delete buffer;
                    return m_tiff_datasize;
                }

                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_ZIP)
                {
                    buffer = new byte [m_tiff_datasize];
                    memset(buffer, 0, m_tiff_datasize);
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    input.ReadRawStrip(0, buffer, 0, m_tiff_datasize);
                    if (m_tiff_fillorder == FILLORDER_LSB2MSB)
                        Tiff.ReverseBits(buffer, m_tiff_datasize);

                    writeToFile(buffer, m_tiff_datasize);
                    delete buffer;
                    return m_tiff_datasize;
                }
                
                if (m_tiff_compression == COMPRESSION_JPEG)
                {
                    uint count = 0;
                    buffer = new byte [m_tiff_datasize];
                    memset(buffer, 0, m_tiff_datasize);
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    byte* jpt;
                    if (input.GetField(TIFFTAG_JPEGTABLES, &count, &jpt) != 0)
                    {
                        if (count > 4)
                        {
                            memcpy(buffer, jpt, count);
                            bufferoffset += count - 2;
                        }
                    }

                    stripcount = input.NumberOfStrips();
                    uint* sbc;
                    input.GetField(TIFFTAG_STRIPBYTECOUNTS, &sbc);
                    for (uint i = 0; i < stripcount; i++)
                    {
                        if (sbc[i] > max_striplength)
                            max_striplength = sbc[i];
                    }
                    
                    byte* stripbuffer = new byte [max_striplength];
                    if (stripbuffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for readwrite_pdf_image, %s", max_striplength, input.FileName());
                        delete buffer;
                        m_error = true;
                        return 0;
                    }

                    for (uint i = 0; i < stripcount; i++)
                    {
                        int striplength = input.ReadRawStrip(i, stripbuffer, 0, -1);
                        if (!process_jpeg_strip(stripbuffer, striplength, buffer, bufferoffset, i, m_tiff_length))
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't process JPEG data in input file %s", input.FileName());
                            delete stripbuffer;
                            delete buffer;
                            m_error = true;
                            return 0;
                        }
                    }

                    buffer[bufferoffset++] = 0xff;
                    buffer[bufferoffset++] = 0xd9;
                    writeToFile(buffer, bufferoffset);
                    delete stripbuffer;
                    delete buffer;
                    return bufferoffset;
                }
            }

            int stripsize = 0;
            if (m_pdf_sample == T2P_SAMPLE_NOTHING)
            {
                buffer = new byte [m_tiff_datasize];
                memset(buffer, 0, m_tiff_datasize);
                if (buffer == null)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                    m_error = true;
                    return 0;
                }

                stripsize = input.StripSize();
                stripcount = input.NumberOfStrips();
                for (uint i = 0; i < stripcount; i++)
                {
                    int read = input.ReadEncodedStrip(i, buffer, bufferoffset, stripsize);
                    if (read == -1)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding strip %u of %s", i, input.FileName());
                        delete buffer;
                        m_error = true;
                        return 0;
                    }

                    bufferoffset += read;
                }
            }
            else
            {
                byte* samplebuffer = null;
                bool dataready = false;

                if ((m_pdf_sample & T2P_SAMPLE_PLANAR_SEPARATE_TO_CONTIG) != 0)
                {
                    int sepstripsize = input.StripSize();
                    int sepstripcount = input.NumberOfStrips();

                    stripsize = sepstripsize * m_tiff_samplesperpixel;
                    stripcount = sepstripcount / m_tiff_samplesperpixel;

                    buffer = new byte [m_tiff_datasize];
                    memset(buffer, 0, m_tiff_datasize);
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    samplebuffer = new byte [stripsize];
                    if (samplebuffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }
                    
                    for (uint i = 0; i < stripcount; i++)
                    {
                        int samplebufferoffset = 0;
                        for (uint j = 0; j < m_tiff_samplesperpixel; j++)
                        {
                            int read = input.ReadEncodedStrip(i + j * stripcount, samplebuffer, samplebufferoffset, sepstripsize);
                            if (read == -1)
                            {
                                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding strip %u of %s", i + j * stripcount, input.FileName());
                                delete buffer;
                                m_error = true;
                                return 0;
                            }
                            samplebufferoffset += read;
                        }

                        sample_planar_separate_to_contig(buffer, bufferoffset, samplebuffer, samplebufferoffset);
                        bufferoffset += samplebufferoffset;
                    }

                    delete samplebuffer;
                    dataready = true;
                }

                if (!dataready)
                {
                    buffer = new byte [m_tiff_datasize];
                    memset(buffer, 0, m_tiff_datasize);
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    stripsize = input.StripSize();
                    stripcount = input.NumberOfStrips();
                    for (uint i = 0; i < stripcount; i++)
                    {
                        int read = input.ReadEncodedStrip(i, buffer, bufferoffset, stripsize);
                        if (read == -1)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding strip %u of %s", i, input.FileName());
                            delete samplebuffer;
                            delete buffer;
                            m_error = true;
                            return 0;
                        }

                        bufferoffset += read;
                    }

                    if ((m_pdf_sample & T2P_SAMPLE_REALIZE_PALETTE) != 0)
                    {
                        samplebuffer = Tiff.Realloc(buffer, m_tiff_datasize, m_tiff_datasize * m_tiff_samplesperpixel);
                        if (samplebuffer == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                            m_error = true;
                            delete buffer;
                        }
                        else
                        {
                            buffer = samplebuffer;
                            m_tiff_datasize *= m_tiff_samplesperpixel;
                        }
                        
                        sample_realize_palette(buffer);
                    }

                    if ((m_pdf_sample & T2P_SAMPLE_RGBA_TO_RGB) != 0)
                        m_tiff_datasize = sample_rgba_to_rgb(buffer, m_tiff_width* m_tiff_length);

                    if ((m_pdf_sample & T2P_SAMPLE_RGBAA_TO_RGB) != 0)
                        m_tiff_datasize = sample_rgbaa_to_rgb(buffer, m_tiff_width* m_tiff_length);

                    if ((m_pdf_sample & T2P_SAMPLE_YCBCR_TO_RGB) != 0)
                    {
                        samplebuffer = Tiff.Realloc(buffer, m_tiff_datasize, m_tiff_width * m_tiff_length * 4);
                        if (samplebuffer == null)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image, %s", m_tiff_datasize, input.FileName());
                            m_error = true;
                            delete buffer;
                            return 0;
                        }
                        else
                        {
                            buffer = samplebuffer;
                        }

                        uint* buffer32 = byteArrayToUInt(buffer, 0, m_tiff_width * m_tiff_length * 4);
                        if (!input.ReadRGBAImageOriented(m_tiff_width, m_tiff_length, buffer32, ORIENTATION_TOPLEFT, 0))
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't use ReadRGBAImageOriented to extract RGB image from %s", input.FileName());
                            m_error = true;
                            delete buffer32;
                            return 0;
                        }

                        uintToByteArray(buffer32, 0, m_tiff_width * m_tiff_length, buffer, 0);
                        delete buffer32;

                        m_tiff_datasize = sample_abgr_to_rgb(buffer, m_tiff_width * m_tiff_length);
                    }

                    if ((m_pdf_sample & T2P_SAMPLE_LAB_SIGNED_TO_UNSIGNED) != 0)
                        m_tiff_datasize = sample_lab_signed_to_unsigned(buffer, m_tiff_width * m_tiff_length);
                }
            }

            disable(m_output);
            m_output.SetField(TIFFTAG_PHOTOMETRIC, m_tiff_photometric);
            m_output.SetField(TIFFTAG_BITSPERSAMPLE, m_tiff_bitspersample);
            m_output.SetField(TIFFTAG_SAMPLESPERPIXEL, m_tiff_samplesperpixel);
            m_output.SetField(TIFFTAG_IMAGEWIDTH, m_tiff_width);
            m_output.SetField(TIFFTAG_IMAGELENGTH, m_tiff_length);
            m_output.SetField(TIFFTAG_ROWSPERSTRIP, m_tiff_length);
            m_output.SetField(TIFFTAG_PLANARCONFIG, PLANARCONFIG_CONTIG);
            m_output.SetField(TIFFTAG_FILLORDER, FILLORDER_MSB2LSB);

            switch (m_pdf_compression)
            {
            case t2p_compress_t.T2P_COMPRESS_NONE:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_NONE);
                break;
            
            case t2p_compress_t.T2P_COMPRESS_G4:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_CCITTFAX4);
                break;
            
            case t2p_compress_t.T2P_COMPRESS_JPEG:
                if (m_tiff_photometric == PHOTOMETRIC_YCBCR)
                {
                    UInt16 hor = 0;
                    UInt16 ver = 0;
                    if (input.GetField(TIFFTAG_YCBCRSUBSAMPLING, &hor, &ver))
                    {
                        if (hor != 0 && ver != 0)
                            m_output.SetField(TIFFTAG_YCBCRSUBSAMPLING, hor, ver);
                    }

                    float* xfloatp;
                    if (input.GetField(TIFFTAG_REFERENCEBLACKWHITE, &xfloatp))
                        m_output.SetField(TIFFTAG_REFERENCEBLACKWHITE, xfloatp);
                }

                if (!m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_JPEG))
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Unable to use JPEG compression for input %s and output %s", input.FileName(), m_output.FileName());
                    delete buffer;
                    m_error = true;
                    return 0;
                }

                m_output.SetField(TIFFTAG_JPEGTABLESMODE, 0);

                if ((m_pdf_colorspace & (t2p_cs_t.T2P_CS_RGB | t2p_cs_t.T2P_CS_LAB)) != 0)
                {
                    m_output.SetField(TIFFTAG_PHOTOMETRIC, PHOTOMETRIC_YCBCR);

                    if (m_tiff_photometric != PHOTOMETRIC_YCBCR)
                        m_output.SetField(TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RGB);
                    else
                        m_output.SetField(TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RAW);
                }

                if (m_pdf_defaultcompressionquality != 0)
                    m_output.SetField(TIFFTAG_JPEGQUALITY, m_pdf_defaultcompressionquality);

                break;
            
            case t2p_compress_t.T2P_COMPRESS_ZIP:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_DEFLATE);
                if (m_pdf_defaultcompressionquality % 100 != 0)
                    m_output.SetField(TIFFTAG_PREDICTOR, m_pdf_defaultcompressionquality % 100);
                
                if (m_pdf_defaultcompressionquality / 100 != 0)
                    m_output.SetField(TIFFTAG_ZIPQUALITY, (m_pdf_defaultcompressionquality / 100));

                break;
            
            default:
                break;
            }

            enable(m_output);
            m_outputwritten = 0;

            if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_JPEG && m_tiff_photometric == PHOTOMETRIC_YCBCR)
                bufferoffset = m_output.WriteEncodedStrip(0, buffer, stripsize * stripcount);
            else
                bufferoffset = m_output.WriteEncodedStrip(0, buffer, m_tiff_datasize);

            delete buffer;
            buffer = null;

            if (bufferoffset == (int)-1)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error writing encoded strip to output PDF %s", m_output.FileName());
                m_error = true;
                return 0;
            }

            return m_outputwritten;
        }

        /*
        * This function reads the raster image data from the input TIFF for an image
        * tile and writes the data to the output PDF XObject image dictionary stream
        * for the tile.  It returns the amount written or zero on error.
        */
        private int readwrite_pdf_image_tile(Tiff input, uint tile)
        {
            bool edge = false;
            edge |= tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile);
            edge |= tile_is_bottom_edge(m_tiff_tiles[m_pdf_page], tile);

            byte* buffer = null;
            int bufferoffset = 0;

            if ((m_pdf_transcode == T2P_TRANSCODE_RAW) && (!edge || (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_JPEG)))
            {
                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_G4)
                {
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    input.ReadRawTile(tile, buffer, 0, m_tiff_datasize);
                    if (m_tiff_fillorder == FILLORDER_LSB2MSB)
                        Tiff.ReverseBits(buffer, m_tiff_datasize);

                    writeToFile(buffer, m_tiff_datasize);
                    delete buffer;
                    return m_tiff_datasize;
                }
                
                if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_ZIP)
                {
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    input.ReadRawTile(tile, buffer, 0, m_tiff_datasize);
                    if (m_tiff_fillorder == FILLORDER_LSB2MSB)
                        Tiff.ReverseBits(buffer, m_tiff_datasize);

                    writeToFile(buffer, m_tiff_datasize);
                    delete buffer;
                    return m_tiff_datasize;
                }
                
                if (m_tiff_compression == COMPRESSION_JPEG)
                {
                    byte table_end[2];
                    uint count = 0;
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    byte* jpt;
                    if (input.GetField(TIFFTAG_JPEGTABLES, &count, &jpt) != 0)
                    {
                        if (count > 0)
                        {
                            memcpy(buffer, jpt, count);
                            bufferoffset += count - 2;
                            table_end[0] = buffer[bufferoffset - 2];
                            table_end[1] = buffer[bufferoffset - 1];
                        }

                        if (count > 0)
                        {
                            uint xuint32 = bufferoffset;
                            bufferoffset += input.ReadRawTile(tile, buffer, bufferoffset - 2, -1);
                            buffer[xuint32 - 2] = table_end[0];
                            buffer[xuint32 - 1] = table_end[1];
                        }
                        else
                        {
                            bufferoffset += input.ReadRawTile(tile, buffer, bufferoffset, -1);
                        }
                    }

                    writeToFile(buffer, bufferoffset);
                    delete buffer;
                    return bufferoffset;
                }
            }

            if (m_pdf_sample == T2P_SAMPLE_NOTHING)
            {
                buffer = new byte [m_tiff_datasize];
                if (buffer == null)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                    m_error = true;
                    return 0;
                }

                int read = input.ReadEncodedTile(tile, buffer, bufferoffset, m_tiff_datasize);
                if (read == -1)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding tile %u of %s", tile, input.FileName());
                    delete buffer;
                    m_error = true;
                    return 0;
                }
            }
            else
            {
                if (m_pdf_sample == T2P_SAMPLE_PLANAR_SEPARATE_TO_CONTIG)
                {
                    int septilesize = input.TileSize();
                    uint septilecount = input.NumberOfTiles();
                    uint tilecount = septilecount / m_tiff_samplesperpixel;
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    byte* samplebuffer = new byte [m_tiff_datasize];
                    if (samplebuffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return 0;
                    }

                    int samplebufferoffset = 0;
                    for (UInt16 i = 0; i < m_tiff_samplesperpixel; i++)
                    {
                        int read = input.ReadEncodedTile(tile + i * tilecount, samplebuffer, samplebufferoffset, septilesize);
                        if (read == -1)
                        {
                            Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding tile %u of %s", tile + i * tilecount, input.FileName());
                            delete samplebuffer;
                            delete buffer;
                            m_error = true;
                            return 0;
                        }

                        samplebufferoffset += read;
                    }

                    sample_planar_separate_to_contig(buffer, bufferoffset, samplebuffer, samplebufferoffset);
                    bufferoffset += samplebufferoffset;
                    delete samplebuffer;
                }

                if (buffer == null)
                {
                    buffer = new byte [m_tiff_datasize];
                    if (buffer == null)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %lu bytes of memory for readwrite_pdf_image_tile, %s", m_tiff_datasize, input.FileName());
                        m_error = true;
                        return (0);
                    }
                    
                    int read = input.ReadEncodedTile(tile, buffer, bufferoffset, m_tiff_datasize);
                    if (read == -1)
                    {
                        Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error on decoding tile %u of %s", tile, input.FileName());
                        delete buffer;
                        m_error = true;
                        return 0;
                    }
                }

                if ((m_pdf_sample & T2P_SAMPLE_RGBA_TO_RGB) != 0)
                    m_tiff_datasize = sample_rgba_to_rgb(buffer, m_tiff_tiles[m_pdf_page].tiles_tilewidth * m_tiff_tiles[m_pdf_page].tiles_tilelength);

                if ((m_pdf_sample & T2P_SAMPLE_RGBAA_TO_RGB) != 0)
                    m_tiff_datasize = sample_rgbaa_to_rgb(buffer, m_tiff_tiles[m_pdf_page].tiles_tilewidth * m_tiff_tiles[m_pdf_page].tiles_tilelength);

                if (m_pdf_sample & T2P_SAMPLE_YCBCR_TO_RGB)
                {
                    Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "No support for YCbCr to RGB in tile for %s", input.FileName());
                    delete buffer;
                    m_error = true;
                    return 0;
                }

                if ((m_pdf_sample & T2P_SAMPLE_LAB_SIGNED_TO_UNSIGNED) != 0)
                    m_tiff_datasize = sample_lab_signed_to_unsigned(buffer, m_tiff_tiles[m_pdf_page].tiles_tilewidth * m_tiff_tiles[m_pdf_page].tiles_tilelength);
            }

            if (tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile) != 0)
                tile_collapse_left(buffer, input.TileRowSize(), m_tiff_tiles[m_pdf_page].tiles_tilewidth, m_tiff_tiles[m_pdf_page].tiles_edgetilewidth, m_tiff_tiles[m_pdf_page].tiles_tilelength);

            disable(m_output);
            m_output.SetField(TIFFTAG_PHOTOMETRIC, m_tiff_photometric);
            m_output.SetField(TIFFTAG_BITSPERSAMPLE, m_tiff_bitspersample);
            m_output.SetField(TIFFTAG_SAMPLESPERPIXEL, m_tiff_samplesperpixel);

            if (tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile) == 0)
                m_output.SetField(TIFFTAG_IMAGEWIDTH, m_tiff_tiles[m_pdf_page].tiles_tilewidth);
            else
                m_output.SetField(TIFFTAG_IMAGEWIDTH, m_tiff_tiles[m_pdf_page].tiles_edgetilewidth);

            if (tile_is_bottom_edge(m_tiff_tiles[m_pdf_page], tile) == 0)
            {
                m_output.SetField(TIFFTAG_IMAGELENGTH, m_tiff_tiles[m_pdf_page].tiles_tilelength);
                m_output.SetField(TIFFTAG_ROWSPERSTRIP, m_tiff_tiles[m_pdf_page].tiles_tilelength);
            }
            else
            {
                m_output.SetField(TIFFTAG_IMAGELENGTH, m_tiff_tiles[m_pdf_page].tiles_edgetilelength);
                m_output.SetField(TIFFTAG_ROWSPERSTRIP, m_tiff_tiles[m_pdf_page].tiles_edgetilelength);
            }

            m_output.SetField(TIFFTAG_PLANARCONFIG, PLANARCONFIG_CONTIG);
            m_output.SetField(TIFFTAG_FILLORDER, FILLORDER_MSB2LSB);

            switch (m_pdf_compression)
            {
            case t2p_compress_t.T2P_COMPRESS_NONE:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_NONE);
                break;

            case t2p_compress_t.T2P_COMPRESS_G4:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_CCITTFAX4);
                break;
            
            case t2p_compress_t.T2P_COMPRESS_JPEG:
                if (m_tiff_photometric == PHOTOMETRIC_YCBCR)
                {
                    UInt16 hor = 0;
                    UInt16 ver = 0;
                    if (input.GetField(TIFFTAG_YCBCRSUBSAMPLING, &hor, &ver))
                    {
                        if (hor != 0 && ver != 0)
                            m_output.SetField(TIFFTAG_YCBCRSUBSAMPLING, hor, ver);
                    }

                    float* xfloatp;
                    if (input.GetField(TIFFTAG_REFERENCEBLACKWHITE, &xfloatp))
                        m_output.SetField(TIFFTAG_REFERENCEBLACKWHITE, xfloatp);
                }
                
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_JPEG);
                m_output.SetField(TIFFTAG_JPEGTABLESMODE, 0); /* JPEGTABLESMODE_NONE */

                if ((m_pdf_colorspace & (t2p_cs_t.T2P_CS_RGB | t2p_cs_t.T2P_CS_LAB)) != 0)
                {
                    m_output.SetField(TIFFTAG_PHOTOMETRIC, PHOTOMETRIC_YCBCR);
                    if (m_tiff_photometric != PHOTOMETRIC_YCBCR)
                        m_output.SetField(TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RGB);
                    else
                        m_output.SetField(TIFFTAG_JPEGCOLORMODE, JPEGCOLORMODE_RAW);
                }

                if (m_pdf_defaultcompressionquality != 0)
                    m_output.SetField(TIFFTAG_JPEGQUALITY, m_pdf_defaultcompressionquality);

                break;

            case t2p_compress_t.T2P_COMPRESS_ZIP:
                m_output.SetField(TIFFTAG_COMPRESSION, COMPRESSION_DEFLATE);
                if (m_pdf_defaultcompressionquality % 100 != 0)
                    m_output.SetField(TIFFTAG_PREDICTOR, m_pdf_defaultcompressionquality % 100);

                if (m_pdf_defaultcompressionquality / 100 != 0)
                    m_output.SetField(TIFFTAG_ZIPQUALITY, (m_pdf_defaultcompressionquality / 100));

                break;

            default:
                break;
            }

            enable(m_output);
            m_outputwritten = 0;
            bufferoffset = m_output.WriteEncodedStrip(0, buffer, m_output.StripSize());
            delete buffer;
            buffer = null;

            if (bufferoffset == -1)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error writing encoded tile to output PDF %s", m_output.FileName());
                m_error = true;
                return 0;
            }

            return m_outputwritten;
        }
        
        /*
        * This function calls WriteDirectory on the output after blanking its
        * output by replacing the read, write, and seek procedures with empty
        * implementations, then it replaces the original implementations.
        */
        private void write_advance_directory()
        {
            disable(m_output);
            if (!m_output.WriteDirectory())
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Error writing virtual directory to output PDF %s", m_output.FileName());
                m_error = true;
                return ;
            }

            enable(m_output);
        }

        private void sample_planar_separate_to_contig(byte[] buffer, int bufferOffset, byte[] samplebuffer, int samplebuffersize)
        {
            int stride = samplebuffersize / m_tiff_samplesperpixel;
            for (int i = 0; i < stride; i++)
            {
                for (int j = 0; j < m_tiff_samplesperpixel; j++)
                {
                    buffer[bufferOffset + i * m_tiff_samplesperpixel + j] = samplebuffer[i + j * stride];
                }
            }
        }

        private void sample_realize_palette(byte[] buffer)
        {
            uint sample_count = m_tiff_width * m_tiff_length;
            UInt16 component_count = m_tiff_samplesperpixel;

            for (uint i = sample_count; i > 0; i--)
            {
                uint palette_offset = buffer[i - 1] * component_count;
                uint sample_offset = (i - 1) * component_count;
                for (uint j = 0; j < component_count; j++)
                {
                    buffer[sample_offset + j] = m_pdf_palette[palette_offset + j];
                }
            }
        }

        /* 
        This function writes the PDF header to output.
        */
        private int write_pdf_header()
        {
            char buffer[16];
            sprintf(buffer, "%%PDF-%u.%u ", m_pdf_majorversion & 0xff, m_pdf_minorversion & 0xff);
            
            int written = writeToFile(buffer);
            written += writeToFile("\n%");

            byte[] octals = new byte
            { 
                Convert.ToByte("342", 8),
                Convert.ToByte("343", 8),
                Convert.ToByte("317", 8),
                Convert.ToByte("323", 8)
            };

            written += writeToFile(octals, octals.Length);
            written += writeToFile("\n");

            return written;
        }

        /*
        This function writes the PDF Catalog structure to output.
        */
        private int write_pdf_catalog()
        {
            int written = writeToFile("<< \n/Type /Catalog \n/Pages ");
    
            char buffer[16];
            sprintf(buffer, "%lu", m_pdf_pages);
            written += writeToFile(buffer);
            written += writeToFile(" 0 R \n");

            if (m_pdf_fitwindow)
                written += writeToFile("/ViewerPreferences <</FitWindow true>>\n");

            written += writeToFile(">>\n");
            return written;
        }

        /*
        This function writes the PDF Info structure to output.
        */
        private int write_pdf_info(Tiff input)
        {
            if (m_pdf_datetime == null)
                pdf_tifftime(input);

            int written = 0;
            if (strlen((char*)m_pdf_datetime) > 0)
            {
                written += writeToFile("<< \n/CreationDate ");
                written += write_pdf_string(m_pdf_datetime);
                written += writeToFile("\n/ModDate ");
                written += write_pdf_string(m_pdf_datetime);
            }

            /*written += t2pWriteFile("\n/Producer ");

            //char buffer[512];
            memset(buffer, 0x00, sizeof(buffer));
            sprintf(buffer, "libtiff / tiff2pdf - %d", TIFFLIB_VERSION);
            written += write_pdf_string(buffer);*/
            written += writeToFile("\n");
            
            if (m_pdf_creator != null)
            {
                if (strlen((char*)m_pdf_creator) > 0)
                {
                    if (strlen((char*)m_pdf_creator) > 511)
                        m_pdf_creator[512] = '\0';

                    written += writeToFile("/Creator ");
                    written += write_pdf_string(m_pdf_creator);
                    written += writeToFile("\n");
                }
            }
            else
            {
                byte* info;
                if (input.GetField(TIFFTAG_SOFTWARE, &info))
                {
                    if (strlen((char*)info) > 511)
                        info[512] = '\0';

                    written += writeToFile("/Creator ");
                    written += write_pdf_string(info);
                    written += writeToFile("\n");
                }
            }

            if (m_pdf_author != null)
            {
                if (strlen((char*)m_pdf_author) > 0)
                {
                    if (strlen((char*)m_pdf_author) > 511)
                        m_pdf_author[512] = '\0';

                    written += writeToFile("/Author ");
                    written += write_pdf_string(m_pdf_author);
                    written += writeToFile("\n");
                }
            }
            else
            {
                byte* info;
                if (input.GetField(TIFFTAG_ARTIST, &info))
                {
                    if (strlen((char*)info) > 511)
                        info[512] = '\0';

                    written += writeToFile("/Author ");
                    written += write_pdf_string(info);
                    written += writeToFile("\n");
                }
                else if (input.GetField(TIFFTAG_COPYRIGHT, &info))
                {
                    if (strlen((char*)info) > 511)
                        info[512] = '\0';

                    written += writeToFile("/Author ");
                    written += write_pdf_string(info);
                    written += writeToFile("\n");
                }
            }

            if (m_pdf_title != null)
            {
                if (strlen((char*)m_pdf_title) > 0)
                {
                    if (strlen((char*)m_pdf_title) > 511)
                        m_pdf_title[512] = '\0';

                    written += writeToFile("/Title ");
                    written += write_pdf_string(m_pdf_title);
                    written += writeToFile("\n");
                }
            }
            else
            {
                byte* info;
                if (input.GetField(TIFFTAG_DOCUMENTNAME, &info))
                {
                    if (strlen((char*)info) > 511)
                        info[512] = '\0';

                    written += writeToFile("/Title ");
                    written += write_pdf_string(info);
                    written += writeToFile("\n");
                }
            }
            
            if (m_pdf_subject != null)
            {
                if (strlen((char*)m_pdf_subject) > 0)
                {
                    if (strlen((char*)m_pdf_subject) > 511)
                        m_pdf_subject[512] = '\0';

                    written += writeToFile("/Subject ");
                    written += write_pdf_string(m_pdf_subject);
                    written += writeToFile("\n");
                }
            }
            else
            {
                byte* info;
                if (input.GetField(TIFFTAG_IMAGEDESCRIPTION, &info))
                {
                    if (strlen((char*)info) > 511)
                        info[512] = '\0';

                    written += writeToFile("/Subject ");
                    written += write_pdf_string(info);
                    written += writeToFile("\n");
                }
            }

            if (m_pdf_keywords != null)
            {
                if (strlen((char*)m_pdf_keywords) > 0)
                {
                    if (strlen((char*)m_pdf_keywords) > 511)
                        m_pdf_keywords[512] = '\0';

                    written += writeToFile("/Keywords ");
                    written += write_pdf_string(m_pdf_keywords);
                    written += writeToFile("\n");
                }
            }

            written += writeToFile(">> \n");
            return written;
        }
        
        /*
        * This function fills a string of a T2P struct with the current time as a PDF
        * date string, it is called by pdf_tifftime.
        */
        private void pdf_currenttime()
        {
            int timenow = 1247603070; // 15-07-2009 XXXX
            DateTime dt = new DateTime(1970, 1, 1).AddSeconds(timenow);

            //timenow=time(0);
            //struct tm* currenttime = localtime(&timenow);
            //sprintf((char*)m_pdf_datetime, "D:%.4d%.2d%.2d%.2d%.2d%.2d", (currenttime.tm_year + 1900) % 65536, (currenttime.tm_mon + 1) % 256, (currenttime.tm_mday) % 256, (currenttime.tm_hour) % 256, (currenttime.tm_min) % 256, (currenttime.tm_sec) % 256);
        }
        
        /*
        * This function fills a string of a T2P struct with the date and time of a
        * TIFF file if it exists or the current time as a PDF date string.
        */
        private void pdf_tifftime(Tiff input)
        {
            m_pdf_datetime = new byte [19];
            if (m_pdf_datetime == null)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for t2p_pdf_tiff_time", 17);
                m_error = true;
                return ;
            } 
            
            m_pdf_datetime[16] = '\0';

            char* datetime;
            if (input.GetField(TIFFTAG_DATETIME, &datetime) && (strlen(datetime) >= 19))
            {
                m_pdf_datetime[0] = 'D';
                m_pdf_datetime[1] = ':';
                m_pdf_datetime[2] = datetime[0];
                m_pdf_datetime[3] = datetime[1];
                m_pdf_datetime[4] = datetime[2];
                m_pdf_datetime[5] = datetime[3];
                m_pdf_datetime[6] = datetime[5];
                m_pdf_datetime[7] = datetime[6];
                m_pdf_datetime[8] = datetime[8];
                m_pdf_datetime[9] = datetime[9];
                m_pdf_datetime[10] = datetime[11];
                m_pdf_datetime[11] = datetime[12];
                m_pdf_datetime[12] = datetime[14];
                m_pdf_datetime[13] = datetime[15];
                m_pdf_datetime[14] = datetime[17];
                m_pdf_datetime[15] = datetime[18];
            }
            else
            {
                pdf_currenttime();
            }
        }

        /*
        * This function writes a PDF Pages Tree structure to output.
        */
        private int write_pdf_pages()
        {
            int written = writeToFile("<< \n/Type /Pages \n/Kids [ ");
            int page = m_pdf_pages + 1;

            char buffer[16];
            for (UInt16 i = 0; i < m_tiff_pagecount; i++)
            {
                sprintf(buffer, "%d", page);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");

                if (((i + 1) % 8) == 0)
                    written += writeToFile("\n");

                page += 3;
                page += m_tiff_pages[i].page_extra;

                if (m_tiff_pages[i].page_tilecount > 0)
                    page += (2 * m_tiff_pages[i].page_tilecount);
                else
                    page += 2;
            }

            written += writeToFile("] \n/Count ");
            memset(buffer, 0x00, 16);
            sprintf(buffer, "%d", m_tiff_pagecount);
            written += writeToFile(buffer);
            written += writeToFile(" \n>> \n");

            return written;
        }

        /*
        This function composes the page size and image and tile locations on a page.
        */
        private void compose_pdf_page()
        {
            m_pdf_xres = m_tiff_xres;
            m_pdf_yres = m_tiff_yres;

            if (m_pdf_overrideres)
            {
                m_pdf_xres = m_pdf_defaultxres;
                m_pdf_yres = m_pdf_defaultyres;
            }
            
            if (m_pdf_xres == 0.0)
                m_pdf_xres = m_pdf_defaultxres;
            
            if (m_pdf_yres == 0.0)
                m_pdf_yres = m_pdf_defaultyres;

            if ((m_tiff_resunit != RESUNIT_CENTIMETER && m_tiff_resunit != RESUNIT_INCH) && (m_tiff_xres < PS_UNIT_SIZE && m_tiff_yres < PS_UNIT_SIZE))
            {
                // apply special processing for case when resolution 
                // unit is unspecified and resolution is "very low" (less then PS_UNIT_SIZE)
                m_pdf_imagewidth = ((float)m_tiff_width) / m_pdf_xres;
                m_pdf_imagelength = ((float)m_tiff_length) / m_pdf_yres;
            }
            else
            {
                m_pdf_imagewidth = ((float)m_tiff_width) * PS_UNIT_SIZE / m_pdf_xres;
                m_pdf_imagelength = ((float)m_tiff_length) * PS_UNIT_SIZE / m_pdf_yres;
            }

            if (m_pdf_overridepagesize)
            {
                m_pdf_pagewidth = m_pdf_defaultpagewidth;
                m_pdf_pagelength = m_pdf_defaultpagelength;
            }
            else
            {
                m_pdf_pagewidth = m_pdf_imagewidth;
                m_pdf_pagelength = m_pdf_imagelength;
            }

            m_pdf_mediabox.x1 = 0.0;
            m_pdf_mediabox.y1 = 0.0;
            m_pdf_mediabox.x2 = m_pdf_pagewidth;
            m_pdf_mediabox.y2 = m_pdf_pagelength;
            m_pdf_imagebox.x1 = 0.0;
            m_pdf_imagebox.y1 = 0.0;
            m_pdf_imagebox.x2 = m_pdf_imagewidth;
            m_pdf_imagebox.y2 = m_pdf_imagelength;

            if (m_pdf_overridepagesize)
            {
                m_pdf_imagebox.x1 += (m_pdf_pagewidth - m_pdf_imagewidth) / 2.0F;
                m_pdf_imagebox.y1 += (m_pdf_pagelength - m_pdf_imagelength) / 2.0F;
                m_pdf_imagebox.x2 += (m_pdf_pagewidth - m_pdf_imagewidth) / 2.0F;
                m_pdf_imagebox.y2 += (m_pdf_pagelength - m_pdf_imagelength) / 2.0F;
            }

            if (m_tiff_orientation > ORIENTATION_BOTLEFT)
            {
                float f = m_pdf_mediabox.x2;
                m_pdf_mediabox.x2 = m_pdf_mediabox.y2;
                m_pdf_mediabox.y2 = f;
            }

            T2P_TILE* tiles = null;
            int istiled = ((m_tiff_tiles[m_pdf_page]).tiles_tilecount == 0) ? 0 : 1;
            if (istiled == 0)
            {
                compose_pdf_page_orient(&m_pdf_imagebox, m_tiff_orientation);
                return ;
            }
            else
            {
                uint tilewidth = m_tiff_tiles[m_pdf_page].tiles_tilewidth;
                uint tilelength = m_tiff_tiles[m_pdf_page].tiles_tilelength;
                uint tilecountx = (m_tiff_width + tilewidth - 1) / tilewidth;
                m_tiff_tiles[m_pdf_page].tiles_tilecountx = tilecountx;
                uint tilecounty = (m_tiff_length + tilelength - 1) / tilelength;
                m_tiff_tiles[m_pdf_page].tiles_tilecounty = tilecounty;
                m_tiff_tiles[m_pdf_page].tiles_edgetilewidth = m_tiff_width % tilewidth;
                m_tiff_tiles[m_pdf_page].tiles_edgetilelength = m_tiff_length % tilelength;
                tiles = m_tiff_tiles[m_pdf_page].tiles_tiles;
                
                uint i = 0;
                uint i2 = 0;
                for (i2 = 0; i2 < tilecounty - 1; i2++)
                {
                    for (i = 0; i < tilecountx - 1; i++)
                    {
                        T2P_BOX* boxp = &tiles[i2 * tilecountx + i].tile_box;
                        boxp.x1 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth * i * tilewidth) / (float)m_tiff_width);
                        boxp.x2 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth * (i + 1) * tilewidth) / (float)m_tiff_width);
                        boxp.y1 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * (i2 + 1) * tilelength) / (float)m_tiff_length);
                        boxp.y2 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * i2 * tilelength) / (float)m_tiff_length);
                    }

                    T2P_BOX* boxp = &tiles[i2 * tilecountx + i].tile_box;
                    boxp.x1 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth * i * tilewidth) / (float)m_tiff_width);
                    boxp.x2 = m_pdf_imagebox.x2;
                    boxp.y1 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * (i2 + 1) * tilelength) / (float)m_tiff_length);
                    boxp.y2 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * i2 * tilelength) / (float)m_tiff_length);
                }

                for (i = 0; i < tilecountx - 1; i++)
                {
                    T2P_BOX* boxp = &tiles[i2 * tilecountx + i].tile_box;
                    boxp.x1 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth * i * tilewidth) / (float)m_tiff_width);
                    boxp.x2 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth *(i + 1) * tilewidth) / (float)m_tiff_width);
                    boxp.y1 = m_pdf_imagebox.y1;
                    boxp.y2 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * i2 * tilelength) / (float)m_tiff_length);
                }

                T2P_BOX* boxp = &tiles[i2 * tilecountx + i].tile_box;
                boxp.x1 = m_pdf_imagebox.x1 + ((float)(m_pdf_imagewidth * i * tilewidth) / (float)m_tiff_width);
                boxp.x2 = m_pdf_imagebox.x2;
                boxp.y1 = m_pdf_imagebox.y1;
                boxp.y2 = m_pdf_imagebox.y2 - ((float)(m_pdf_imagelength * i2 * tilelength) / (float)m_tiff_length);
            }

            if (m_tiff_orientation == 0 || m_tiff_orientation == ORIENTATION_TOPLEFT)
            {
                for (uint i = 0; i < m_tiff_tiles[m_pdf_page].tiles_tilecount; i++)
                    compose_pdf_page_orient(&tiles[i].tile_box, 0);

                return ;
            }

            for (uint i = 0; i < m_tiff_tiles[m_pdf_page].tiles_tilecount; i++)
            {
                T2P_BOX* boxp = &tiles[i].tile_box;
                boxp.x1 -= m_pdf_imagebox.x1;
                boxp.x2 -= m_pdf_imagebox.x1;
                boxp.y1 -= m_pdf_imagebox.y1;
                boxp.y2 -= m_pdf_imagebox.y1;

                if (m_tiff_orientation == ORIENTATION_TOPRIGHT || m_tiff_orientation == ORIENTATION_BOTRIGHT)
                {
                    boxp.x1 = m_pdf_imagebox.x2 - m_pdf_imagebox.x1 - boxp.x1;
                    boxp.x2 = m_pdf_imagebox.x2 - m_pdf_imagebox.x1 - boxp.x2;
                }
                
                if (m_tiff_orientation == ORIENTATION_BOTRIGHT || m_tiff_orientation == ORIENTATION_BOTLEFT)
                {
                    boxp.y1 = m_pdf_imagebox.y2 - m_pdf_imagebox.y1 - boxp.y1;
                    boxp.y2 = m_pdf_imagebox.y2 - m_pdf_imagebox.y1 - boxp.y2;
                }
                
                if (m_tiff_orientation == ORIENTATION_LEFTBOT || m_tiff_orientation == ORIENTATION_LEFTTOP)
                {
                    boxp.y1 = m_pdf_imagebox.y2 - m_pdf_imagebox.y1 - boxp.y1;
                    boxp.y2 = m_pdf_imagebox.y2 - m_pdf_imagebox.y1 - boxp.y2;
                }
                
                if (m_tiff_orientation == ORIENTATION_LEFTTOP || m_tiff_orientation == ORIENTATION_RIGHTTOP)
                {
                    boxp.x1 = m_pdf_imagebox.x2 - m_pdf_imagebox.x1 - boxp.x1;
                    boxp.x2 = m_pdf_imagebox.x2 - m_pdf_imagebox.x1 - boxp.x2;
                }
                
                if (m_tiff_orientation > ORIENTATION_BOTLEFT)
                {
                    float f = boxp.x1;
                    boxp.x1 = boxp.y1;
                    boxp.y1 = f;
                    f = boxp.x2;
                    boxp.x2 = boxp.y2;
                    boxp.y2 = f;
                    compose_pdf_page_orient_flip(boxp, m_tiff_orientation);
                }
                else
                {
                    compose_pdf_page_orient(boxp, m_tiff_orientation);
                }
            }
        }
        
        /*
        * 	This function writes a PDF Image XObject Colorspace name to output.
        */
        private int write_pdf_xobject_cs()
        {
            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_ICCBASED) != 0)
                return write_pdf_xobject_icccs();

            int written = 0;
            char buffer[128];

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_PALETTE) != 0)
            {
                written += writeToFile("[ /Indexed ");
                m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace ^ t2p_cs_t.T2P_CS_PALETTE);
                written += write_pdf_xobject_cs();
                m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_PALETTE);
                sprintf(buffer, "%u", (0x0001 << m_tiff_bitspersample) - 1);
                written += writeToFile(buffer);
                written += writeToFile(" ");
                memset(buffer, 0x00, 16);
                sprintf(buffer, "%lu", m_pdf_palettecs);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ]\n");
                return written;
            }

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_BILEVEL) != 0)
                written += writeToFile("/DeviceGray \n");

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_GRAY) != 0)
            {
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALGRAY) != 0)
                    written += write_pdf_xobject_calcs();
                else
                    written += writeToFile("/DeviceGray \n");
            }

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_RGB) != 0)
            {
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALRGB) != 0)
                    written += write_pdf_xobject_calcs();
                else
                    written += writeToFile("/DeviceRGB \n");
            }

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CMYK) != 0)
                written += writeToFile("/DeviceCMYK \n");

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_LAB) != 0)
            {
                written += writeToFile("[/Lab << \n");
                written += writeToFile("/WhitePoint ");
                
                float X_W = m_tiff_whitechromaticities[0];
                float Y_W = m_tiff_whitechromaticities[1];
                float Z_W = 1.0F - (X_W + Y_W);
                X_W /= Y_W;
                Z_W /= Y_W;
                Y_W = 1.0F;
                sprintf(buffer, "[%.4f %.4f %.4f] \n", X_W, Y_W, Z_W);
                written += writeToFile(buffer);
                
                X_W = 0.3457F; /* 0.3127F; */ /* D50, commented D65 */
                Y_W = 0.3585F; /* 0.3290F; */
                Z_W = 1.0F - (X_W + Y_W);
                X_W /= Y_W;
                Z_W /= Y_W;
                Y_W = 1.0F;
                sprintf(buffer, "[%.4f %.4f %.4f] \n", X_W, Y_W, Z_W);
                written += writeToFile(buffer);
                written += writeToFile("/Range ");
                sprintf(buffer, "[%d %d %d %d] \n", m_pdf_labrange[0], m_pdf_labrange[1], m_pdf_labrange[2], m_pdf_labrange[3]);
                written += writeToFile(buffer);
                written += writeToFile(">>] \n");
            }

            return written;
        }
        
        private int write_pdf_transfer()
        {
            char buffer[16];
            int written = writeToFile("<< /Type /ExtGState \n/TR ");

            if (m_tiff_transferfunctioncount == 1)
            {
                sprintf(buffer, "%lu", m_pdf_xrefcount + 1);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
            }
            else
            {
                written += writeToFile("[ ");
                sprintf(buffer, "%lu", m_pdf_xrefcount + 1);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
                sprintf(buffer, "%lu", m_pdf_xrefcount + 2);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
                sprintf(buffer, "%lu", m_pdf_xrefcount + 3);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
                written += writeToFile("/Identity ] ");
            }

            written += writeToFile(" >> \n");
            return written;
        }

        private int write_pdf_transfer_dict()
        {
            int written = writeToFile("/FunctionType 0 \n");
            written += writeToFile("/Domain [0.0 1.0] \n");
            written += writeToFile("/Range [0.0 1.0] \n");

            char buffer[32];
            sprintf(buffer, "/Size [%u] \n", (1 << m_tiff_bitspersample));
            written += writeToFile(buffer);
            written += writeToFile("/BitsPerSample 16 \n");
            written += write_pdf_stream_dict(((int)1) << (m_tiff_bitspersample + 1), 0);

            return written;
        }

        private int write_pdf_transfer_stream(UInt16 i)
        {
            return write_pdf_stream((byte*)m_tiff_transferfunction[i], (((int)1) << (m_tiff_bitspersample + 1)));
        }
        
        /*
        This function writes a PDF Image XObject Colorspace array to output.
        */
        private int write_pdf_xobject_calcs()
        {
            int written = writeToFile("[");

            float X_W = 0.0;
            float Y_W = 0.0;
            float Z_W = 0.0;    
            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALGRAY) != 0)
            {
                written += writeToFile("/CalGray ");
                X_W = m_tiff_whitechromaticities[0];
                Y_W = m_tiff_whitechromaticities[1];
                Z_W = 1.0F - (X_W + Y_W);
                X_W /= Y_W;
                Z_W /= Y_W;
                Y_W = 1.0F;
            }

            float X_R = 0.0;
            float Y_R = 0.0;
            float Z_R = 0.0;
            float X_G = 0.0;
            float Y_G = 0.0;
            float Z_G = 0.0;
            float X_B = 0.0;
            float Y_B = 0.0;
            float Z_B = 0.0;
            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALRGB) != 0)
            {
                written += writeToFile("/CalRGB ");
                float x_w = m_tiff_whitechromaticities[0];
                float y_w = m_tiff_whitechromaticities[1];
                float x_r = m_tiff_primarychromaticities[0];
                float y_r = m_tiff_primarychromaticities[1];
                float x_g = m_tiff_primarychromaticities[2];
                float y_g = m_tiff_primarychromaticities[3];
                float x_b = m_tiff_primarychromaticities[4];
                float y_b = m_tiff_primarychromaticities[5];

                const float R = 1.0;
                const float G = 1.0;
                const float B = 1.0;

                float z_w = y_w * ((x_g - x_b) * y_r - (x_r - x_b) * y_g + (x_r - x_g) * y_b);
                Y_R = (y_r / R) * ((x_g - x_b) * y_w - (x_w - x_b) * y_g + (x_w - x_g) * y_b) / z_w;
                X_R = Y_R * x_r / y_r;
                Z_R = Y_R * (((1 - x_r) / y_r) - 1);
                Y_G = ((0.0F - y_g) / G) * ((x_r - x_b) * y_w - (x_w - x_b) * y_r + (x_w - x_r) * y_b) / z_w;
                X_G = Y_G * x_g / y_g;
                Z_G = Y_G * (((1 - x_g) / y_g) - 1);
                Y_B = (y_b / B) * ((x_r - x_g) * y_w - (x_w - x_g) * y_r + (x_w - x_r) * y_g) / z_w;
                X_B = Y_B * x_b / y_b;
                Z_B = Y_B * (((1 - x_b) / y_b) - 1);
                X_W = (X_R * R) + (X_G * G) + (X_B * B);
                Y_W = (Y_R * R) + (Y_G * G) + (Y_B * B);
                Z_W = (Z_R * R) + (Z_G * G) + (Z_B * B);
                X_W /= Y_W;
                Z_W /= Y_W;
                Y_W = 1.0;
            }

            written += writeToFile("<< \n");
            
            char buffer[128];
            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALGRAY) != 0)
            {
                written += writeToFile("/WhitePoint ");
                sprintf(buffer, "[%.4f %.4f %.4f] \n", X_W, Y_W, Z_W);
                written += writeToFile(buffer);
                written += writeToFile("/Gamma 2.2 \n");
            }

            if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_CALRGB) != 0)
            {
                written += writeToFile("/WhitePoint ");
                sprintf(buffer, "[%.4f %.4f %.4f] \n", X_W, Y_W, Z_W);
                written += writeToFile(buffer);
                written += writeToFile("/Matrix ");
                sprintf(buffer, "[%.4f %.4f %.4f %.4f %.4f %.4f %.4f %.4f %.4f] \n", X_R, Y_R, Z_R, X_G, Y_G, Z_G, X_B, Y_B, Z_B);
                written += writeToFile(buffer);
                written += writeToFile("/Gamma [2.2 2.2 2.2] \n");
            }

            written += writeToFile(">>] \n");
            return written;
        }
        
        /*
        This function writes a PDF Image XObject Colorspace array to output.
        */
        private int write_pdf_xobject_icccs()
        {
            int written = writeToFile("[/ICCBased ");

            char buffer[16];
            sprintf(buffer, "%lu", m_pdf_icccs);
            written += writeToFile(buffer);
            written += writeToFile(" 0 R] \n");

            return written;
        }
        
        private int write_pdf_xobject_icccs_dict()
        {
            int written = writeToFile("/N ");

            char buffer[16];
            sprintf(buffer, "%u \n", m_tiff_samplesperpixel);
            written += writeToFile(buffer);
            written += writeToFile("/Alternate ");
            m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace ^ t2p_cs_t.T2P_CS_ICCBASED);
            written += write_pdf_xobject_cs();
            m_pdf_colorspace = (t2p_cs_t)(m_pdf_colorspace | t2p_cs_t.T2P_CS_ICCBASED);
            written += write_pdf_stream_dict(m_tiff_iccprofilelength, 0);

            return written;
        }

        private int write_pdf_xobject_icccs_stream()
        {
            return write_pdf_stream(m_tiff_iccprofile, m_tiff_iccprofilelength);
        }
        
        /*
        This function writes a PDF Image XObject Decode array to output.
        */
        private int write_pdf_xobject_decode()
        {
            int written = writeToFile("/Decode [ ");
            for (int i = 0; i < m_tiff_samplesperpixel; i++)
                written += writeToFile("1 0 ");
         
            written += writeToFile("]\n");
            return written;
        }
        
        /*
        This function writes a PDF xref table to output.
        */
        private int write_pdf_xreftable()
        {
            int written = writeToFile("xref\n0 ");
    
            char buffer[21];
            sprintf(buffer, "%lu", m_pdf_xrefcount + 1);
            written += writeToFile(buffer);
            written += writeToFile(" \n0000000000 65535 f \n");
            for (uint i = 0; i < m_pdf_xrefcount; i++)
            {
                sprintf(buffer, "%.10lu 00000 n \n", m_pdf_xrefoffsets[i]);
                written += writeToFile(buffer);
            }

            return written;
        }
        
        /*
        * This function writes a PDF trailer to output.
        */
        private int write_pdf_trailer()
        {
            char* fileidbuf = "2900000023480000FF180000FF670000";

            m_pdf_fileid = new byte [33];
            if (m_pdf_fileid == null)
            {
                Tiff.Error(Tiff2PdfConstants.TIFF2PDF_MODULE, "Can't allocate %u bytes of memory for write_pdf_trailer", 33);
                m_error = true;
                return 0;
            }

            memset(m_pdf_fileid, 0x00, 33);
            for (int i = 0; i < 16; i++)
            {
                m_pdf_fileid[2 * i] = fileidbuf[2 * i];
                m_pdf_fileid[2 * i + 1] = fileidbuf[2 * i + 1];
            }

            int written = writeToFile("trailer\n<<\n/Size ");

            char buffer[32];
            sprintf(buffer, "%lu", m_pdf_xrefcount + 1);
            written += writeToFile(buffer);
            memset(buffer, 0x00, 32);
            written += writeToFile("\n/Root ");
            sprintf(buffer, "%lu", m_pdf_catalog);
            written += writeToFile(buffer);
            memset(buffer, 0x00, 32);
            written += writeToFile(" 0 R \n/Info ");
            sprintf(buffer, "%lu", m_pdf_info);
            written += writeToFile(buffer);
            memset(buffer, 0x00, 32);
            written += writeToFile(" 0 R \n/ID[<");
            written += writeToFile(m_pdf_fileid, 32);
            written += writeToFile("><");
            written += writeToFile(m_pdf_fileid, 32);
            written += writeToFile(">]\n>>\nstartxref\n");
            sprintf(buffer, "%lu", m_pdf_startxref);
            written += writeToFile(buffer);
            memset(buffer, 0x00, 32);
            written += writeToFile("\n%%EOF\n");

            return written;
        }
        
        /*
        This function writes a PDF Image XObject stream dictionary to output. 
        */
        private int write_pdf_xobject_stream_dict(uint tile)
        {
            int written = write_pdf_stream_dict(0, m_pdf_xrefcount + 1);
            written += writeToFile("/Type /XObject \n/Subtype /Image \n/Name /Im");

            char buffer[16];
            sprintf(buffer, "%u", m_pdf_page + 1);
            written += writeToFile(buffer);
            if (tile != 0)
            {
                written += writeToFile("_");
                sprintf(buffer, "%lu", tile);
                written += writeToFile(buffer);
            }

            written += writeToFile("\n/Width ");
            memset(buffer, 0x00, 16);
            if (tile == 0)
            {
                sprintf(buffer, "%lu", m_tiff_width);
            }
            else
            {
                if (tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile - 1) != 0)
                    sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_edgetilewidth);
                else
                    sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_tilewidth);
            }

            written += writeToFile(buffer);
            written += writeToFile("\n/Height ");
            memset(buffer, 0x00, 16);
            if (tile == 0)
            {
                sprintf(buffer, "%lu", m_tiff_length);
            }
            else
            {
                if (tile_is_bottom_edge(m_tiff_tiles[m_pdf_page], tile - 1) != 0)
                    sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_edgetilelength);
                else
                    sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_tilelength);
            }

            written += writeToFile(buffer);
            written += writeToFile("\n/BitsPerComponent ");
            memset(buffer, 0x00, 16);
            sprintf(buffer, "%u", m_tiff_bitspersample);
            written += writeToFile(buffer);
            written += writeToFile("\n/ColorSpace ");
            written += write_pdf_xobject_cs();

            if (m_pdf_image_interpolate)
                written += writeToFile("\n/Interpolate true");
            
            if (m_pdf_switchdecode && !(m_pdf_colorspace == t2p_cs_t.T2P_CS_BILEVEL && m_pdf_compression == t2p_compress_t.T2P_COMPRESS_G4))
                written += write_pdf_xobject_decode();

            written += write_pdf_xobject_stream_filter(tile);
            return written;
        }
        
        /*
        This function writes a PDF Image XObject stream filter name and parameters to 
        output.
        */
        private int write_pdf_xobject_stream_filter(uint tile)
        {
            if (m_pdf_compression == t2p_compress_t.T2P_COMPRESS_NONE)
                return 0;
         
            int written = writeToFile("/Filter ");
            char buffer[16];

            switch (m_pdf_compression)
            {
            case t2p_compress_t.T2P_COMPRESS_G4:
                written += writeToFile("/CCITTFaxDecode ");
                written += writeToFile("/DecodeParms ");
                written += writeToFile("<< /K -1 ");
                
                if (tile == 0)
                {
                    written += writeToFile("/Columns ");
                    sprintf(buffer, "%lu", m_tiff_width);
                    written += writeToFile(buffer);
                    written += writeToFile(" /Rows ");
                    sprintf(buffer, "%lu", m_tiff_length);
                    written += writeToFile(buffer);
                }
                else
                {
                    if (tile_is_right_edge(m_tiff_tiles[m_pdf_page], tile - 1) == 0)
                    {
                        written += writeToFile("/Columns ");
                        sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_tilewidth);
                        written += writeToFile(buffer);
                    }
                    else
                    {
                        written += writeToFile("/Columns ");
                        sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_edgetilewidth);
                        written += writeToFile(buffer);
                    }

                    if (tile_is_bottom_edge(m_tiff_tiles[m_pdf_page], tile - 1) == 0)
                    {
                        written += writeToFile(" /Rows ");
                        sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_tilelength);
                        written += writeToFile(buffer);
                    }
                    else
                    {
                        written += writeToFile(" /Rows ");
                        sprintf(buffer, "%lu", m_tiff_tiles[m_pdf_page].tiles_edgetilelength);
                        written += writeToFile(buffer);
                    }
                }

                if (!m_pdf_switchdecode)
                    written += writeToFile(" /BlackIs1 true ");

                written += writeToFile(">>\n");
                break;
            
            case t2p_compress_t.T2P_COMPRESS_JPEG:
                written += writeToFile("/DCTDecode ");

                if (m_tiff_photometric != PHOTOMETRIC_YCBCR)
                {
                    written += writeToFile("/DecodeParms ");
                    written += writeToFile("<< /ColorTransform 0 >>\n");
                }
                break;

            case t2p_compress_t.T2P_COMPRESS_ZIP:
                written += writeToFile("/FlateDecode ");
                if (m_pdf_compressionquality % 100)
                {
                    written += writeToFile("/DecodeParms ");
                    written += writeToFile("<< /Predictor ");
                    memset(buffer, 0x00, 16);
                    sprintf(buffer, "%u", m_pdf_compressionquality % 100);
                    written += writeToFile(buffer);
                    written += writeToFile(" /Columns ");
                    memset(buffer, 0x00, 16);
                    sprintf(buffer, "%lu", m_tiff_width);
                    written += writeToFile(buffer);
                    written += writeToFile(" /Colors ");
                    memset(buffer, 0x00, 16);
                    sprintf(buffer, "%u", m_tiff_samplesperpixel);
                    written += writeToFile(buffer);
                    written += writeToFile(" /BitsPerComponent ");
                    memset(buffer, 0x00, 16);
                    sprintf(buffer, "%u", m_tiff_bitspersample);
                    written += writeToFile(buffer);
                    written += writeToFile(">>\n");
                }
                break;

            default:
                break;
            }

            return written;
        }
        
        /*
        This function writes a PDF Contents stream to output.
        */
        private int write_pdf_page_content_stream()
        {
            char buffer[512];
            int written = 0;
            if (m_tiff_tiles[m_pdf_page].tiles_tilecount > 0)
            {
                for (uint i = 0; i < m_tiff_tiles[m_pdf_page].tiles_tilecount; i++)
                {
                    T2P_BOX box = m_tiff_tiles[m_pdf_page].tiles_tiles[i].tile_box;
                    int buflen = sprintf(buffer, "q %s %.4f %.4f %.4f %.4f %.4f %.4f cm /Im%d_%ld Do Q\n", m_tiff_transferfunctioncount ? "/GS1 gs " : "", box.mat[0], box.mat[1], box.mat[3], box.mat[4], box.mat[6], box.mat[7], m_pdf_page + 1, i + 1);
                    written += write_pdf_stream((byte*)buffer, buflen);
                }
            }
            else
            {
                T2P_BOX box = m_pdf_imagebox;
                int buflen = sprintf(buffer, "q %s %.4f %.4f %.4f %.4f %.4f %.4f cm /Im%d Do Q\n", m_tiff_transferfunctioncount ? "/GS1 gs " : "", box.mat[0], box.mat[1], box.mat[3], box.mat[4], box.mat[6], box.mat[7], m_pdf_page + 1);
                written += write_pdf_stream((byte*)buffer, buflen);
            }

            return written;
        }
        
        /*
        This function writes a palette stream for an indexed color space to output.
        */
        private int write_pdf_xobject_palettecs_stream()
        {
            return write_pdf_stream(m_pdf_palette, m_pdf_palettesize);
        }

        /*
        * This function is used by qsort to sort a T2P_PAGE array of page structures
        * by page number.
        */
        private static int cmp_t2p_page(object e1, object e2)
        {
            return (((T2P_PAGE*)e1).page_number - ((T2P_PAGE*)e2).page_number);
        }

        /*
        * This functions returns a non-zero value when the tile is on the right edge
        * and does not have full imaged tile width.
        */
        private static bool tile_is_right_edge(T2P_TILES tiles, uint tile)
        {
            if (((tile + 1) % tiles.tiles_tilecountx == 0) && (tiles.tiles_edgetilewidth != 0))
                return true;

            return false;
        }

        /*
        * This functions returns a non-zero value when the tile is on the bottom edge
        * and does not have full imaged tile length.
        */
        private static bool tile_is_bottom_edge(T2P_TILES tiles, uint tile)
        {
            if (((tile + 1) > (tiles.tiles_tilecount - tiles.tiles_tilecountx)) && (tiles.tiles_edgetilelength != 0))
                return true;

            return false;
        }

        private static bool process_jpeg_strip(byte[] strip, int striplength, byte[] buffer, ref int bufferoffset, uint no, uint height)
        {
            int i = 1;
            while (i < striplength)
            {
                switch (strip[i])
                {
                case 0xd8:
                    i += 2;
                    break;

                case 0xc0:
                case 0xc1:
                case 0xc3:
                case 0xc9:
                case 0xca:
                    if (no == 0)
                    {
                        memcpy(&buffer[bufferoffset], &strip[i - 1], strip[i + 2] + 2);
                        
                        UInt16 v_samp = 1;
                        UInt16 h_samp = 1;
                        for (int j = 0; j < buffer[bufferoffset + 9]; j++)
                        {
                            if ((buffer[bufferoffset + 11 + (2 * j)] >> 4) > h_samp)
                                h_samp = buffer[bufferoffset + 11 + (2 * j)] >> 4;

                            if ((buffer[bufferoffset + 11 + (2 * j)] & 0x0f) > v_samp)
                                v_samp = buffer[bufferoffset + 11 + (2 * j)] & 0x0f;
                        }

                        v_samp *= 8;
                        h_samp *= 8;
                        UInt16 ri = (((((UInt16)(buffer[bufferoffset + 5]) << 8) | (UInt16)buffer[bufferoffset + 6]) + v_samp - 1) / v_samp);
                        ri *= (((((UInt16)(buffer[bufferoffset + 7]) << 8) | (UInt16)buffer[bufferoffset + 8]) + h_samp - 1) / h_samp);
                        buffer[bufferoffset + 5] = (byte)((height >> 8) & 0xff);
                        buffer[bufferoffset + 6] = (byte)(height & 0xff);
                        bufferoffset += strip[i + 2] + 2;
                        i += strip[i + 2] + 2;

                        buffer[bufferoffset++] = 0xff;
                        buffer[bufferoffset++] = 0xdd;
                        buffer[bufferoffset++] = 0x00;
                        buffer[bufferoffset++] = 0x04;
                        buffer[bufferoffset++] = (ri >> 8) & 0xff;
                        buffer[bufferoffset++] = ri & 0xff;
                    }
                    else
                    {
                        i += strip[i + 2] + 2;
                    }
                    break;

                case 0xc4:
                case 0xdb:
                    memcpy(&buffer[bufferoffset], &strip[i - 1], strip[i + 2] + 2);
                    bufferoffset += strip[i + 2] + 2;
                    i += strip[i + 2] + 2;
                    break;

                case 0xda:
                    if (no == 0)
                    {
                        memcpy(&buffer[bufferoffset], &strip[i - 1], strip[i + 2] + 2);
                        bufferoffset += strip[i + 2] + 2;
                        i += strip[i + 2] + 2;
                    }
                    else
                    {
                        buffer[bufferoffset++] = 0xff;
                        buffer[bufferoffset++] = (byte)(0xd0 | ((no - 1) % 8));
                        i += strip[i + 2] + 2;
                    }

                    memcpy(&buffer[bufferoffset], &strip[i - 1], striplength - i - 1);
                    bufferoffset += striplength - i - 1;
                    return true;

                default:
                    i += strip[i + 2] + 2;
                }
            }

            return false;
        }
        
        /*
        * This functions converts in place a buffer of RGBA interleaved data
        * into RGB interleaved data, adding 255-A to each component sample.
        */
        private static int sample_rgba_to_rgb(byte[] data, uint samplecount)
        {
            uint* data32 = byteArrayToUInt(data, 0, samplecount * sizeof(uint));

            uint i = 0;
            for ( ; i < samplecount; i++)
            {
                uint sample = data32[i];
                byte alpha = 255 - (sample & 0xff);
                data[i * 3] = (byte)((sample >> 24) & 0xff) + alpha;
                data[i * 3 + 1] = (byte)((sample >> 16) & 0xff) + alpha;
                data[i * 3 + 2] = (byte)((sample >> 8) & 0xff) + alpha;
            }

            delete data32;
            return (i * 3);
        }
        
        /*
        * This functions converts in place a buffer of RGBA interleaved data
        * into RGB interleaved data, discarding A.
        */
        private static int sample_rgbaa_to_rgb(byte[] data, uint samplecount)
        {
            uint i = 0;
            for ( ; i < samplecount; i++)
                memcpy(data + i * 3, data + i * 4, 3);

            return (i * 3);
        }
        
        /*
        This functions converts in place a buffer of ABGR interleaved data
        into RGB interleaved data, discarding A.
        */
        private static int sample_abgr_to_rgb(byte[] data, uint samplecount)
        {
            uint* data32 = byteArrayToUInt(data, 0, samplecount * sizeof(uint));

            uint i = 0;
            for ( ; i < samplecount; i++)
            {
                uint sample = data32[i];
                data[i * 3] = sample & 0xff;
                data[i * 3 + 1] = (sample >> 8) & 0xff;
                data[i * 3 + 2] = (sample >> 16) & 0xff;
            }

            delete data32;
            return (i * 3);
        }
        
        /*
        This function converts the a and b samples of Lab data from signed
        to unsigned.
        */
        private static int sample_lab_signed_to_unsigned(byte[] buffer, uint samplecount)
        {
            for (uint i = 0; i < samplecount; i++)
            {
                if ((buffer[i * 3 + 1] & 0x80) != 0)
                    buffer[i * 3 + 1] = (byte)(0x80 + (sbyte)buffer[i * 3 + 1]); // cast to signed int is important
                else
                    buffer[i * 3 + 1] |= 0x80;

                if ((buffer[i * 3 + 2] & 0x80) != 0)
                    buffer[i * 3 + 2] = (byte)(0x80 + (sbyte)buffer[i * 3 + 2]);
                else
                    buffer[i * 3 + 2] |= 0x80;
            }

            return (samplecount * 3);
        }
        
        private static void disable(Tiff tif)
        {
            T2P* t2p = (T2P*)tif.Clientdata();
            t2p.m_outputdisable = true;
        }

        private static void enable(Tiff tif)
        {
            T2P* t2p = (T2P*)tif.Clientdata();
            t2p.m_outputdisable = false;
        }

        /*
        This functions converts a tilewidth x tilelength buffer of samples into an edgetilewidth x 
        tilelength buffer of samples.
        */
        private static void tile_collapse_left(byte[] buffer, int scanwidth, uint tilewidth, uint edgetilewidth, uint tilelength)
        {
            int edgescanwidth = (scanwidth * edgetilewidth + tilewidth - 1) / tilewidth;
            for (uint i = 0; i < tilelength; i++)
                memcpy(&buffer[edgescanwidth * i], &buffer[scanwidth * i], edgescanwidth);
        }
        
        /*
        This function writes a PDF string object to output.
        */
        private int write_pdf_string(byte[] pdfstr)
        {
            int written = writeToFile("(");
            size_t len = strlen((char*)pdfstr);
            for (uint i = 0; i < len; i++)
            {
                if ((pdfstr[i] & 0x80) != 0 || (pdfstr[i] == 127) || (pdfstr[i] < 32))
                {
                    char buffer[64];
                    sprintf(buffer, "\\%.3hho", pdfstr[i]);
                    buffer[sizeof(buffer) - 1] = '\0';
                    written += writeToFile(buffer);
                }
                else
                {
                    switch (pdfstr[i])
                    {
                    case 0x08:
                        written += writeToFile("\\b");
                        break;
                    case 0x09:
                        written += writeToFile("\\t");
                        break;
                    case 0x0A:
                        written += writeToFile("\\n");
                        break;
                    case 0x0C:
                        written += writeToFile("\\f");
                        break;
                    case 0x0D:
                        written += writeToFile("\\r");
                        break;
                    case 0x28:
                        written += writeToFile("\\(");
                        break;
                    case 0x29:
                        written += writeToFile("\\)");
                        break;
                    case 0x5C:
                        written += writeToFile("\\\\");
                        break;
                    default:
                        byte b[1];
                        b[0] = pdfstr[i];
                        written += writeToFile(b, 1);
                    }
                }
            }

            written += writeToFile(")");
            return written;
        }
        
        private static void compose_pdf_page_orient(T2P_BOX boxp, UInt16 orientation)
        {
            if (boxp.x1 > boxp.x2)
            {
                float f = boxp.x1;
                boxp.x1 = boxp.x2;
                boxp.x2 = f;
            }

            if (boxp.y1 > boxp.y2)
            {
                float f = boxp.y1;
                boxp.y1 = boxp.y2;
                boxp.y2 = f;
            }
            
            float m1[9];
            boxp.mat[0] = m1[0] = boxp.x2 - boxp.x1;
            boxp.mat[1] = m1[1] = 0.0;
            boxp.mat[2] = m1[2] = 0.0;
            boxp.mat[3] = m1[3] = 0.0;
            boxp.mat[4] = m1[4] = boxp.y2 - boxp.y1;
            boxp.mat[5] = m1[5] = 0.0;
            boxp.mat[6] = m1[6] = boxp.x1;
            boxp.mat[7] = m1[7] = boxp.y1;
            boxp.mat[8] = m1[8] = 1.0;
            
            switch (orientation)
            {
            case 0:
            case ORIENTATION_TOPLEFT:
                break;

            case ORIENTATION_TOPRIGHT:
                boxp.mat[0] = 0.0F - m1[0];
                boxp.mat[6] += m1[0];
                break;

            case ORIENTATION_BOTRIGHT:
                boxp.mat[0] = 0.0F - m1[0];
                boxp.mat[4] = 0.0F - m1[4];
                boxp.mat[6] += m1[0];
                boxp.mat[7] += m1[4];
                break;

            case ORIENTATION_BOTLEFT:
                boxp.mat[4] = 0.0F - m1[4];
                boxp.mat[7] += m1[4];
                break;

            case ORIENTATION_LEFTTOP:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = 0.0F - m1[0];
                boxp.mat[3] = 0.0F - m1[4];
                boxp.mat[4] = 0.0F;
                boxp.mat[6] += m1[4];
                boxp.mat[7] += m1[0];
                break;

            case ORIENTATION_RIGHTTOP:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = 0.0F - m1[0];
                boxp.mat[3] = m1[4];
                boxp.mat[4] = 0.0F;
                boxp.mat[7] += m1[0];
                break;

            case ORIENTATION_RIGHTBOT:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = m1[0];
                boxp.mat[3] = m1[4];
                boxp.mat[4] = 0.0F;
                break;

            case ORIENTATION_LEFTBOT:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = m1[0];
                boxp.mat[3] = 0.0F - m1[4];
                boxp.mat[4] = 0.0F;
                boxp.mat[6] += m1[4];
                break;
            }
        }

        private static void compose_pdf_page_orient_flip(T2P_BOX boxp, UInt16 orientation)
        {
            if (boxp.x1 > boxp.x2)
            {
                float f = boxp.x1;
                boxp.x1 = boxp.x2;
                boxp.x2 = f;
            }

            if (boxp.y1 > boxp.y2)
            {
                float f = boxp.y1;
                boxp.y1 = boxp.y2;
                boxp.y2 = f;
            }
            
            float m1[9];
            boxp.mat[0] = m1[0] = boxp.x2 - boxp.x1;
            boxp.mat[1] = m1[1] = 0.0F;
            boxp.mat[2] = m1[2] = 0.0F;
            boxp.mat[3] = m1[3] = 0.0F;
            boxp.mat[4] = m1[4] = boxp.y2 - boxp.y1;
            boxp.mat[5] = m1[5] = 0.0F;
            boxp.mat[6] = m1[6] = boxp.x1;
            boxp.mat[7] = m1[7] = boxp.y1;
            boxp.mat[8] = m1[8] = 1.0F;

            switch (orientation)
            {
            case ORIENTATION_LEFTTOP:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = 0.0F - m1[4];
                boxp.mat[3] = 0.0F - m1[0];
                boxp.mat[4] = 0.0F;
                boxp.mat[6] += m1[0];
                boxp.mat[7] += m1[4];
                break;

            case ORIENTATION_RIGHTTOP:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = 0.0F - m1[4];
                boxp.mat[3] = m1[0];
                boxp.mat[4] = 0.0F;
                boxp.mat[7] += m1[4];
                break;

            case ORIENTATION_RIGHTBOT:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = m1[4];
                boxp.mat[3] = m1[0];
                boxp.mat[4] = 0.0F;
                break;

            case ORIENTATION_LEFTBOT:
                boxp.mat[0] = 0.0F;
                boxp.mat[1] = m1[4];
                boxp.mat[3] = 0.0F - m1[0];
                boxp.mat[4] = 0.0F;
                boxp.mat[6] += m1[0];
                break;
            }
        }

        /*
        This function writes a buffer of data to output.
        */
        private int write_pdf_stream(byte[] buffer, int len)
        {
            return writeToFile(buffer, len);
        }
        
        /*
        This functions writes the beginning of a PDF stream to output.
        */
        private int write_pdf_stream_start()
        {
            return writeToFile("stream\n");
        }

        /*
        This function writes the end of a PDF stream to output. 
        */
        private int write_pdf_stream_end()
        {
            return writeToFile("\nendstream\n");
        }
        
        /*
        This function writes a stream dictionary for a PDF stream to output.
        */
        private int write_pdf_stream_dict(int len, uint number)
        {
            int written = writeToFile("/Length ");
            if (len != 0)
            {
                written += write_pdf_stream_length(len);
            }
            else
            {
                char buffer[16];
                sprintf(buffer, "%lu", number);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R \n");
            }

            return written;
        }

        /*
        This functions writes the beginning of a PDF stream dictionary to output.
        */
        private int write_pdf_stream_dict_start()
        {
            return writeToFile("<< \n");
        }
        
        /*
        This function writes the end of a PDF stream dictionary to output. 
        */
        private int write_pdf_stream_dict_end()
        {
            return writeToFile(" >>\n");
        }
        
        /*
        This function writes a number to output.
        */
        private int write_pdf_stream_length(int len)
        {
            char buffer[16];
            sprintf(buffer, "%lu", len);
            int written = writeToFile(buffer);
            written += writeToFile("\n");
            return written;
        }

        /*
        This function writes the beginning of a PDF object to output.
        */
        private int write_pdf_obj_start(uint number)
        {
            char buffer[16];
            sprintf(buffer, "%lu", number);
            int written = writeToFile(buffer);
            written += writeToFile(" 0 obj\n");
            return written;
        }

        /*
        This function writes the end of a PDF object to output.
        */
        private int write_pdf_obj_end()
        {
            return writeToFile("endobj\n");
        }
        
        /*
        This function writes a PDF Page structure to output.
        */
        private int write_pdf_page(uint obj)
        {
            int written = writeToFile("<<\n/Type /Page \n/Parent ");
    
            char buffer[16];
            sprintf(buffer, "%lu", m_pdf_pages);
            written += writeToFile(buffer);
            written += writeToFile(" 0 R \n");
            written += writeToFile("/MediaBox [");
            sprintf(buffer, "%.4f", m_pdf_mediabox.x1);
            written += writeToFile(buffer);
            written += writeToFile(" ");
            sprintf(buffer, "%.4f", m_pdf_mediabox.y1);
            written += writeToFile(buffer);
            written += writeToFile(" ");
            sprintf(buffer, "%.4f", m_pdf_mediabox.x2);
            written += writeToFile(buffer);
            written += writeToFile(" ");
            sprintf(buffer, "%.4f", m_pdf_mediabox.y2);
            written += writeToFile(buffer);
            written += writeToFile("] \n");
            written += writeToFile("/Contents ");
            sprintf(buffer, "%lu", object + 1);
            written += writeToFile(buffer);
            written += writeToFile(" 0 R \n");
            written += writeToFile("/Resources << \n");

            if (m_tiff_tiles[m_pdf_page].tiles_tilecount != 0)
            {
                written += writeToFile("/XObject <<\n");
                for (unsigned int i = 0; i < m_tiff_tiles[m_pdf_page].tiles_tilecount; i++)
                {
                    written += writeToFile("/Im");
                    sprintf(buffer, "%u", m_pdf_page + 1);
                    written += writeToFile(buffer);
                    written += writeToFile("_");
                    sprintf(buffer, "%u", i + 1);
                    written += writeToFile(buffer);
                    written += writeToFile(" ");
                    sprintf(buffer, "%lu", object + 3 + 2 * i + m_tiff_pages[m_pdf_page].page_extra);
                    written += writeToFile(buffer);
                    written += writeToFile(" 0 R ");
                    if (i % 4 == 3)
                        written += writeToFile("\n");
                }
                
                written += writeToFile(">>\n");
            }
            else
            {
                written += writeToFile("/XObject <<\n");
                written += writeToFile("/Im");
                sprintf(buffer, "%u", m_pdf_page + 1);
                written += writeToFile(buffer);
                written += writeToFile(" ");
                sprintf(buffer, "%lu", object + 3 + m_tiff_pages[m_pdf_page].page_extra);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
                written += writeToFile(">>\n");
            }

            if (m_tiff_transferfunctioncount != 0)
            {
                written += writeToFile("/ExtGState <<");
                writeToFile("/GS1 ");
                sprintf(buffer, "%lu", object + 3);
                written += writeToFile(buffer);
                written += writeToFile(" 0 R ");
                written += writeToFile(">> \n");
            }

            written += writeToFile("/ProcSet [ ");
            if (m_pdf_colorspace == t2p_cs_t.T2P_CS_BILEVEL || m_pdf_colorspace == t2p_cs_t.T2P_CS_GRAY)
            {
                written += writeToFile("/ImageB ");
            }
            else
            {
                written += writeToFile("/ImageC ");
                if ((m_pdf_colorspace & t2p_cs_t.T2P_CS_PALETTE) != 0)
                    written += writeToFile("/ImageI ");
            }

            written += writeToFile("]\n>>\n>>\n");
            return written;
        }

        private int writeToFile(byte[] data, int size)
        {
            if (data == null || size == 0)
                return 0;

            TiffStream* stream = m_output.GetStream();
            if (stream)
            {
                thandle_t client = m_output.Clientdata();
                return stream.Write(client, data, size);
            }

            return -1;
        }

        private int writeToFile(string data)
        {
            return writeToFile((byte*)data, strlen(data));
        }
    }
}
