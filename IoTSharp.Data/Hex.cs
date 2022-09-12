﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IoTSharp.Data
{
    //Org.BouncyCastle.Utilities.Encoders
    public sealed class Hex
    {
        private static readonly HexEncoder encoder = new HexEncoder();

        private Hex()
        {
        }

        public static string ToHexString(
            byte[] data)
        {
            return ToHexString(data, 0, data.Length);
        }

        public static string ToHexString(
            byte[] data,
            int off,
            int length)
        {
            byte[] hex = Encode(data, off, length);
            return Encoding.ASCII.GetString(hex, 0, hex.Length);
             
        }


        /**
         * encode the input data producing a Hex encoded byte array.
         *
         * @return a byte array containing the Hex encoded data.
         */
        public static byte[] Encode(
            byte[] data,
            int off,
            int length)
        {
            MemoryStream bOut = new MemoryStream(length * 2);

            encoder.Encode(data, off, length, bOut);

            return bOut.ToArray();
        }
        /**
		 * Hex encode the byte data writing it to the given output stream.
		 *
		 * @return the number of bytes produced.
		 */
        public static int Encode(
            byte[] data,
            int off,
            int length,
            Stream outStream)
        {
            return encoder.Encode(data, off, length, outStream);
        }

        /**
		 * decode the Hex encoded input data. It is assumed the input data is valid.
		 *
		 * @return a byte array representing the decoded data.
		 */
        public static byte[] Decode(
            byte[] data)
        {
            MemoryStream bOut = new MemoryStream((data.Length + 1) / 2);

            encoder.Decode(data, 0, data.Length, bOut);

            return bOut.ToArray();
        }

        /**
		 * decode the Hex encoded string data - whitespace will be ignored.
		 *
		 * @return a byte array representing the decoded data.
		 */
        public static byte[] Decode(
            string data)
        {
            MemoryStream bOut = new MemoryStream((data.Length + 1) / 2);

            encoder.DecodeString(data, bOut);

            return bOut.ToArray();
        }

        /**
		 * decode the Hex encoded string data writing it to the given output stream,
		 * whitespace characters will be ignored.
		 *
		 * @return the number of bytes produced.
		 */
        public static int Decode(
            string data,
            Stream outStream)
        {
            return encoder.DecodeString(data, outStream);
        }
    }
    public class HexEncoder
    {
        protected readonly byte[] encodingTable =
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f'
        };

        /*
         * set up the decoding table.
         */
        protected readonly byte[] decodingTable = new byte[128];

        protected void InitialiseDecodingTable()
        {
            Fill(decodingTable, (byte)0xff);
            for (int i = 0; i < encodingTable.Length; i++)
            {
                decodingTable[encodingTable[i]] = (byte)i;
            }

            decodingTable['A'] = decodingTable['a'];
            decodingTable['B'] = decodingTable['b'];
            decodingTable['C'] = decodingTable['c'];
            decodingTable['D'] = decodingTable['d'];
            decodingTable['E'] = decodingTable['e'];
            decodingTable['F'] = decodingTable['f'];
        }

        public HexEncoder()
        {
            InitialiseDecodingTable();
        }

        public int Encode(byte[] inBuf, int inOff, int inLen, byte[] outBuf, int outOff)
        {
            int inPos = inOff;
            int inEnd = inOff + inLen;
            int outPos = outOff;

            while (inPos < inEnd)
            {
                uint b = inBuf[inPos++];

                outBuf[outPos++] = encodingTable[b >> 4];
                outBuf[outPos++] = encodingTable[b & 0xF];
            }

            return outPos - outOff;
        }

        /**
        * encode the input data producing a Hex output stream.
        *
        * @return the number of bytes produced.
        */
        public int Encode(byte[] buf, int off, int len, Stream outStream)
        {
            if (len < 0)
                return 0;

            byte[] tmp = new byte[72];
            int remaining = len;
            while (remaining > 0)
            {
                int inLen = System.Math.Min(36, remaining);
                int outLen = Encode(buf, off, inLen, tmp, 0);
                outStream.Write(tmp, 0, outLen);
                off += inLen;
                remaining -= inLen;
            }
            return len * 2;
        }

        private static bool Ignore(char c)
        {
            return c == '\n' || c == '\r' || c == '\t' || c == ' ';
        }

        /**
        * decode the Hex encoded byte data writing it to the given output stream,
        * whitespace characters will be ignored.
        *
        * @return the number of bytes produced.
        */
        public int Decode(
            byte[] data,
            int off,
            int length,
            Stream outStream)
        {
            byte b1, b2;
            int outLen = 0;
            byte[] buf = new byte[36];
            int bufOff = 0;
            int end = off + length;

            while (end > off)
            {
                if (!Ignore((char)data[end - 1]))
                    break;

                end--;
            }

            int i = off;
            while (i < end)
            {
                while (i < end && Ignore((char)data[i]))
                {
                    i++;
                }

                b1 = decodingTable[data[i++]];

                while (i < end && Ignore((char)data[i]))
                {
                    i++;
                }

                b2 = decodingTable[data[i++]];

                if ((b1 | b2) >= 0x80)
                    throw new IOException("invalid characters encountered in Hex data");

                buf[bufOff++] = (byte)((b1 << 4) | b2);

                if (bufOff == buf.Length)
                {
                    outStream.Write(buf, 0, bufOff);
                    bufOff = 0;
                }

                outLen++;
            }

            if (bufOff > 0)
            {
                outStream.Write(buf, 0, bufOff);
            }

            return outLen;
        }

        /**
        * decode the Hex encoded string data writing it to the given output stream,
        * whitespace characters will be ignored.
        *
        * @return the number of bytes produced.
        */
        public int DecodeString(
            string data,
            Stream outStream)
        {
            byte b1, b2;
            int length = 0;
            byte[] buf = new byte[36];
            int bufOff = 0;
            int end = data.Length;

            while (end > 0)
            {
                if (!Ignore(data[end - 1]))
                    break;

                end--;
            }

            int i = 0;
            while (i < end)
            {
                while (i < end && Ignore(data[i]))
                {
                    i++;
                }

                b1 = decodingTable[data[i++]];

                while (i < end && Ignore(data[i]))
                {
                    i++;
                }

                b2 = decodingTable[data[i++]];

                if ((b1 | b2) >= 0x80)
                    throw new IOException("invalid characters encountered in Hex data");

                buf[bufOff++] = (byte)((b1 << 4) | b2);

                if (bufOff == buf.Length)
                {
                    outStream.Write(buf, 0, bufOff);
                    bufOff = 0;
                }

                length++;
            }

            if (bufOff > 0)
            {
                outStream.Write(buf, 0, bufOff);
            }

            return length;
        }

        internal byte[] DecodeStrict(string str, int off, int len)
        {
            if (null == str)
                throw new ArgumentNullException("str");
            if (off < 0 || len < 0 || off > (str.Length - len))
                throw new IndexOutOfRangeException("invalid offset and/or length specified");
            if (0 != (len & 1))
                throw new ArgumentException("a hexadecimal encoding must have an even number of characters", "len");

            int resultLen = len >> 1;
            byte[] result = new byte[resultLen];

            int strPos = off;
            for (int i = 0; i < resultLen; ++i)
            {
                byte b1 = decodingTable[str[strPos++]];
                byte b2 = decodingTable[str[strPos++]];

                if ((b1 | b2) >= 0x80)
                    throw new IOException("invalid characters encountered in Hex data");

                result[i] = (byte)((b1 << 4) | b2);
            }
            return result;
        }

        public static void Fill(
            byte[] buf,
            byte b)
        {
            int i = buf.Length;
            while (i > 0)
            {
                buf[--i] = b;
            }
        }

        public static string FromAsciiByteArray(
            byte[] bytes)
        {

            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }
    }
}
