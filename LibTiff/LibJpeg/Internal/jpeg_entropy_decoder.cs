namespace BitMiracle.LibJpeg.Classic.Internal
{
    /// <summary>
    /// Entropy decoding
    /// </summary>
    abstract class jpeg_entropy_decoder
    {
        public delegate bool decode_mcu_delegate(JBLOCK[] MCU_data);
        public delegate void finish_pass_delegate();

        public decode_mcu_delegate decode_mcu;
        public finish_pass_delegate finish_pass;

        public abstract void start_pass();
    }
}
