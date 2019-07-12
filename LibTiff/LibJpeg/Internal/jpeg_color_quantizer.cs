namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// Color quantization or color precision reduction
    /// </summary>
    interface jpeg_color_quantizer
    {
        void start_pass(bool is_pre_scan);

        void color_quantize(byte[][] input_buf, int in_row, byte[][] output_buf, int out_row, int num_rows);

        void finish_pass();
        void new_color_map();
    }
}
