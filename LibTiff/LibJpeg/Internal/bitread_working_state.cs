namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// Bitreading working state within an MCU
    /// </summary>
    struct bitread_working_state
    {
        public int get_buffer;    /* current bit-extraction buffer */
        public int bits_left;      /* # of unused bits in it */

        /* Pointer needed by jpeg_fill_bit_buffer. */
        public jpeg_decompress_struct cinfo;  /* back link to decompress master record */
    }
}
