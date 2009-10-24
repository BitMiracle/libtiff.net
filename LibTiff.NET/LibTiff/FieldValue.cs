/* 
 * Copyright (C) 2008-2009, Bit Miracle
 * http://www.bitmiracle.com
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace BitMiracle.LibTiff
{
    /// <summary>
    /// Holds a value of Tiff tag.
    /// Simply put, it is a wrapper around System.Object, that helps to deal with
    /// unboxing and conversion of types a bit easier.
    /// 
    /// Please take a look at:
    /// http://blogs.msdn.com/ericlippert/archive/2009/03/19/representation-and-identity.aspx
    /// </summary>
    public struct FieldValue
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
                values[i] = new FieldValue(list[i]);

            return values;
        }

        internal void Set(object o)
        {
            m_value = o;
        }

        public object Value
        {
            get { return m_value; }
        }

        // sbyte
        // long
        // ulong
        // decimal
        // char
        // bool
        // object

        public byte ToByte()
        {
            return Convert.ToByte(m_value);
        }

        public short ToShort()
        {
            return Convert.ToInt16(m_value);
        }

        public ushort ToUShort()
        {
            return Convert.ToUInt16(m_value);
        }

        public int ToInt()
        {
            return Convert.ToInt32(m_value);
        }

        public uint ToUInt()
        {
            return Convert.ToUInt32(m_value);
        }

        public float ToFloat()
        {
            return Convert.ToSingle(m_value);
        }

        public double ToDouble()
        {
            return Convert.ToDouble(m_value);
        }

        public new string ToString()
        {
            return Convert.ToString(m_value);
        }

        public byte[] ToByteArray()
        {
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

            return null;
        }

        public short[] ToShortArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
                if (m_value is short[])
                    return m_value as short[];
                else if (m_value is byte[])
                {
                    byte[] temp = m_value as byte[];
                    short[] result = new short[temp.Length];
                    for (int i = 0; i < temp.Length; i++)
                        result[i] = (short)temp[i];

                    return result;
                }
            }

            return null;
        }

        public ushort[] ToUShortArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
            }

            return null;
        }

        public int[] ToIntArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
            }

            return null;
        }

        public uint[] ToUIntArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
            }

            return null;
        }

        public float[] ToFloatArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
            }

            return null;
        }

        public double[] ToDoubleArray()
        {
            Type t = m_value.GetType();
            if (t.IsArray)
            {
            }

            return null;
        }
    }
}
