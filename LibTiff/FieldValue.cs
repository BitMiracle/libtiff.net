/* 
 * Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff.Classic
{
    /// <summary>
    /// Holds a value of Tiff tag.
    /// Simply put, it is a wrapper around System.Object, that helps to deal with
    /// unboxing and conversion of types a bit easier.
    /// 
    /// Please take a look at:
    /// http://blogs.msdn.com/ericlippert/archive/2009/03/19/representation-and-identity.aspx
    /// </summary>
#if EXPOSE_LIBTIFF
    public
#endif
    struct FieldValue
    {
        private object m_value;
        
        internal FieldValue(object o)
        {
            m_value = o;
        }

        static internal FieldValue[] FromParams(params object[] list)
        {
            FieldValue[] values = new FieldValue[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] is FieldValue)
                    values[i] = new FieldValue(((FieldValue)(list[i])).Value);
                else
                    values[i] = new FieldValue(list[i]);
            }

            return values;
        }

        internal void Set(object o)
        {
            m_value = o;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value
        {
            get { return m_value; }
        }

        /// <summary>
        /// Retrieves value converted to byte
        /// </summary>
        public byte ToByte()
        {
            return Convert.ToByte(m_value);
        }

        /// <summary>
        /// Retrieves value converted to short
        /// </summary>
        public short ToShort()
        {
            return Convert.ToInt16(m_value);
        }

        /// <summary>
        /// Retrieves value converted to ushort
        /// </summary>
#if EXPOSE_LIBTIFF
        [CLSCompliant(false)]
#endif
        public ushort ToUShort()
        {
            return Convert.ToUInt16(m_value);
        }

        /// <summary>
        /// Retrieves value converted to int
        /// </summary>
        public int ToInt()
        {
            return Convert.ToInt32(m_value);
        }

        /// <summary>
        /// Retrieves value converted to uint
        /// </summary>
#if EXPOSE_LIBTIFF
        [CLSCompliant(false)]
#endif
        public uint ToUInt()
        {
            return Convert.ToUInt32(m_value);
        }

        /// <summary>
        /// Retrieves value converted to float
        /// </summary>
        public float ToFloat()
        {
            return Convert.ToSingle(m_value);
        }

        /// <summary>
        /// Retrieves value converted to double
        /// </summary>
        public double ToDouble()
        {
            return Convert.ToDouble(m_value);
        }

        /// <summary>
        /// Retrieves value converted to string.
        /// If value is a byte array, then it gets converted to string using 
        /// Latin1 encoding encoder.
        /// </summary>
        public new string ToString()
        {
            if (m_value is byte[])
                return Tiff.Latin1Encoding.GetString(m_value as byte[]);

            return Convert.ToString(m_value);
        }

        /// <summary>
        /// Retrieves value converted to byte array.
        /// If value is byte[] then it retrieved unaltered.
        /// If value is short[], ushort[], int[], uint[], float[] or double[] then
        /// each element of source array gets converted to byte and added to
        /// resulting array.
        /// If value is string then it gets converted to byte[] using Latin1 
        /// encoding encoder.
        /// If value is of any other type then null is returned.
        /// </summary>
        public byte[] GetBytes()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is byte[])
                    return m_value as byte[];
                else if (m_value is short[])
                {
                    short[] temp = m_value as short[];
                    byte[] result = new byte[temp.Length * sizeof(short)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
                else if (m_value is ushort[])
                {
                    ushort[] temp = m_value as ushort[];
                    byte[] result = new byte[temp.Length * sizeof(ushort)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
                else if (m_value is int[])
                {
                    int[] temp = m_value as int[];
                    byte[] result = new byte[temp.Length * sizeof(int)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
                else if (m_value is uint[])
                {
                    uint[] temp = m_value as uint[];
                    byte[] result = new byte[temp.Length * sizeof(uint)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
                else if (m_value is float[])
                {
                    float[] temp = m_value as float[];
                    byte[] result = new byte[temp.Length * sizeof(float)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
                else if (m_value is double[])
                {
                    double[] temp = m_value as double[];
                    byte[] result = new byte[temp.Length * sizeof(double)];
                    int resultOffset = 0;
                    for (int i = 0; i < temp.Length; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(temp[i]);
                        Array.Copy(bytes, 0, result, resultOffset, bytes.Length);
                        resultOffset += bytes.Length;
                    }
                    return result;
                }
            }
            else if (m_value is string)
                return Tiff.Latin1Encoding.GetBytes(m_value as string);

            return null;
        }

        /// <summary>
        /// Retrieves value converted to byte array.
        /// If value is byte[] then it retrieved unaltered.
        /// If value is short[], ushort[], int[] or uint[] then
        /// each element of source array gets converted to byte and added to
        /// resulting array.
        /// If value is string then it gets converted to byte[] using Latin1 
        /// encoding encoder.
        /// If value is of any other type then null is returned.
        /// </summary>
        public byte[] ToByteArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is byte[])
                    return m_value as byte[];
                else if (m_value is short[])
                {
                    short[] temp = m_value as short[];
                    byte[] result = new byte[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (byte)temp[i];

                    return result;
                }
                else if (m_value is ushort[])
                {
                    ushort[] temp = m_value as ushort[];
                    byte[] result = new byte[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (byte)temp[i];

                    return result;
                }
                else if (m_value is int[])
                {
                    int[] temp = m_value as int[];
                    byte[] result = new byte[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (byte)temp[i];

                    return result;
                }
                else if (m_value is uint[])
                {
                    uint[] temp = m_value as uint[];
                    byte[] result = new byte[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (byte)temp[i];

                    return result;
                }
            }
            else if (m_value is string)
                return Tiff.Latin1Encoding.GetBytes(m_value as string);

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of short.
        /// If value is short[] then it retrieved unaltered.
        /// If value is byte[] then each pair of bytes is converted to
        /// short and added to resulting array. If value contains odd amount of
        /// bytes, then null is returned.
        /// If value is ushort[], int[] or uint[] then
        /// each element of source array gets converted to short and added to
        /// resulting array.
        /// If value is of any other type then null is returned.
        /// </summary>
        public short[] ToShortArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is short[])
                    return m_value as short[];
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(short) != 0)
                        return null;

                    int totalShorts = temp.Length / sizeof(short);
                    short[] result = new short[totalShorts];

                    int byteOffset = 0;
                    for (int i = 0; i < totalShorts; i++)
                    {
                        short s = BitConverter.ToInt16(temp, byteOffset);
                        result[i] = s;
                        byteOffset += sizeof(short);
                    }

                    return result;
                }
                else if (m_value is ushort[])
                {
                    ushort[] temp = m_value as ushort[];
                    short[] result = new short[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (short)temp[i];

                    return result;
                }
                else if (m_value is int[])
                {
                    int[] temp = m_value as int[];
                    short[] result = new short[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (short)temp[i];

                    return result;
                }
                else if (m_value is uint[])
                {
                    uint[] temp = m_value as uint[];
                    short[] result = new short[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (short)temp[i];

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of ushort.
        /// If value is ushort[] then it retrieved unaltered.
        /// If value is byte[] then each pair of bytes is converted to
        /// ushort and added to resulting array. If value contains odd amount of
        /// bytes, then null is returned.
        /// If value is short[], int[] or uint[] then
        /// each element of source array gets converted to ushort and added to
        /// resulting array.
        /// If value is of any other type then null is returned.
        /// </summary>
#if EXPOSE_LIBTIFF
        [CLSCompliant(false)]
#endif
        public ushort[] ToUShortArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is ushort[])
                    return m_value as ushort[];
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(ushort) != 0)
                        return null;

                    int totalUShorts = temp.Length / sizeof(ushort);
                    ushort[] result = new ushort[totalUShorts];

                    int byteOffset = 0;
                    for (int i = 0; i < totalUShorts; i++)
                    {
                        ushort s = BitConverter.ToUInt16(temp, byteOffset);
                        result[i] = s;
                        byteOffset += sizeof(ushort);
                    }

                    return result;
                }
                else if (m_value is short[])
                {
                    short[] temp = m_value as short[];
                    ushort[] result = new ushort[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (ushort)temp[i];

                    return result;
                }
                else if (m_value is int[])
                {
                    int[] temp = m_value as int[];
                    ushort[] result = new ushort[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (ushort)temp[i];

                    return result;
                }
                else if (m_value is uint[])
                {
                    uint[] temp = m_value as uint[];
                    ushort[] result = new ushort[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (ushort)temp[i];

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of int.
        /// If value is int[] then it retrieved unaltered.
        /// If value is byte[] then each 4 bytes are converted to
        /// int and added to resulting array. If value contains amount of
        /// bytes that can't be divided by 4 without remainder, then null is returned.
        /// If value is short[], ushort[] or uint[] then
        /// each element of source array gets converted to int and added to
        /// resulting array.
        /// If value is of any other type then null is returned.
        /// </summary>
        public int[] ToIntArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is int[])
                    return m_value as int[];
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(int) != 0)
                        return null;

                    int totalInts = temp.Length / sizeof(int);
                    int[] result = new int[totalInts];

                    int byteOffset = 0;
                    for (int i = 0; i < totalInts; i++)
                    {
                        int s = BitConverter.ToInt32(temp, byteOffset);
                        result[i] = s;
                        byteOffset += sizeof(int);
                    }

                    return result;
                }
                else if (m_value is short[])
                {
                    short[] temp = m_value as short[];
                    int[] result = new int[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (int)temp[i];

                    return result;
                }
                else if (m_value is ushort[])
                {
                    ushort[] temp = m_value as ushort[];
                    int[] result = new int[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (int)temp[i];

                    return result;
                }
                else if (m_value is uint[])
                {
                    uint[] temp = m_value as uint[];
                    int[] result = new int[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (int)temp[i];

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of uint.
        /// If value is uint[] then it retrieved unaltered.
        /// If value is byte[] then each 4 bytes are converted to
        /// uint and added to resulting array. If value contains amount of
        /// bytes that can't be divided by 4 without remainder, then null is returned.
        /// If value is short[], ushort[] or int[] then
        /// each element of source array gets converted to uint and added to
        /// resulting array.
        /// If value is of any other type then null is returned.
        /// </summary>
#if EXPOSE_LIBTIFF
        [CLSCompliant(false)]
#endif
        public uint[] ToUIntArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is uint[])
                    return m_value as uint[];
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(uint) != 0)
                        return null;

                    int totalUInts = temp.Length / sizeof(uint);
                    uint[] result = new uint[totalUInts];

                    int byteOffset = 0;
                    for (int i = 0; i < totalUInts; i++)
                    {
                        uint s = BitConverter.ToUInt32(temp, byteOffset);
                        result[i] = s;
                        byteOffset += sizeof(uint);
                    }

                    return result;
                }
                else if (m_value is short[])
                {
                    short[] temp = m_value as short[];
                    uint[] result = new uint[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (uint)temp[i];

                    return result;
                }
                else if (m_value is ushort[])
                {
                    ushort[] temp = m_value as ushort[];
                    uint[] result = new uint[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (uint)temp[i];

                    return result;
                }
                else if (m_value is int[])
                {
                    int[] temp = m_value as int[];
                    uint[] result = new uint[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (uint)temp[i];

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of float.
        /// If value is float[] then it retrieved unaltered.
        /// If value is double[] then each element of source array gets 
        /// converted to float and added to resulting array.
        /// If value is byte[] then each 4 bytes are converted to float
        /// and added to resulting array. If value contains amount of
        /// bytes that can't be divided by 4 without remainder, then null is returned.
        /// If value is of any other type then null is returned.
        /// </summary>
        public float[] ToFloatArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is float[])
                    return m_value as float[];
                else if (m_value is double[])
                {
                    double[] temp = m_value as double[];
                    float[] result = new float[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (float)temp[i];

                    return result;
                }
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(float) != 0)
                        return null;

                    int tempPos = 0; 
                    
                    int floatCount = temp.Length / sizeof(float);
                    float[] result = new float[floatCount];
                    
                    for (int i = 0; i < floatCount; i++)
                    {
                        float f = BitConverter.ToSingle(temp, tempPos);
                        result[i] = f;
                        tempPos += sizeof(float);
                    }

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves value converted to array of double.
        /// If value is double[] then it retrieved unaltered.
        /// If value is float[] then each element of source array gets 
        /// converted to double and added to resulting array.
        /// If value is byte[] then each 8 bytes are converted to double
        /// and added to resulting array. If value contains amount of
        /// bytes that can't be divided by 8 without remainder, then null is returned.
        /// If value is of any other type then null is returned.
        /// </summary>
        public double[] ToDoubleArray()
        {
            if (m_value == null)
                return null;

            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is double[])
                    return m_value as double[];
                else if (m_value is float[])
                {
                    float[] temp = m_value as float[];
                    double[] result = new double[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (double)temp[i];

                    return result;
                }
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    if (temp.Length % sizeof(double) != 0)
                        return null;

                    int tempPos = 0;

                    int floatCount = temp.Length / sizeof(double);
                    double[] result = new double[floatCount];

                    for (int i = 0; i < floatCount; i++)
                    {
                        double d = BitConverter.ToDouble(temp, tempPos);
                        result[i] = d;
                        tempPos += sizeof(double);
                    }

                    return result;
                }
            }

            return null;
        }
    }
}
