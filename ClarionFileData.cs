using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;

namespace ClarionDatConnector
{
    public class ClarionFileData
    {
        /// <summary>
        /// ClarionFileData reads a Clarion "DAT" file and creates an in-memory database of the information. 
        /// the DataRows allow you to use LINQ 
        /// </summary>
        
        //private properties
        private readonly string filename;

        private DataTable dataTable = new DataTable();
        public DataTable ClarionData { get { return dataTable; } }

        private List<ClarionFileFields> fldsList = new List<ClarionFileFields>();
        private List<ClarionFileKeySect> keys = new List<ClarionFileKeySect>();
        private ClarionFileHeader fh;

        //public properties
        public enum ClarionDataTypes
        {
            LONG = 1,
            REAL = 2,
            STRING = 3,
            STRING_WITH_PICTURE_TOKEN = 4,
            BYTE = 5,
            SHORT = 6,
            GROUP = 7,
            DECIMAL = 8,
            OTHER 
        }

        // constructor
        public ClarionFileData(string filename)
        {
            this.filename = filename;
        }

        // private methods
        private void setupDatatable()
        {
            Encoding Mazovia = new MazoviaEncoding();
            int dupa = 1;
            ReadDataFile((br) => //begin read the file for the header
                {
                    #region SetClarionFileHeader
                    fh = new ClarionFileHeader // begin - set file header info
                    {
                        filesig = br.ReadUInt16(),
                        sfatr = br.ReadUInt16(),
                        numkeys = br.ReadByte(),
                        numrecs = br.ReadUInt32(),
                        numdels = br.ReadUInt32(),
                        numflds = br.ReadUInt16(),
                        numpics = br.ReadUInt16(),
                        numarrs = br.ReadUInt16(),
                        reclen = br.ReadUInt16(),
                        offset = br.ReadUInt32(),
                        logeof = br.ReadUInt32(),
                        logbof = br.ReadUInt32(),
                        freerec = br.ReadUInt32(),
                        recname = Mazovia.GetString(br.ReadBytes(12)),
                        memname = Mazovia.GetString(br.ReadBytes(12)),
                        fileprefix = Mazovia.GetString(br.ReadBytes(3)),
                        recprefix = Mazovia.GetString(br.ReadBytes(3)),
                        memolen = br.ReadUInt16(),
                        memowid = br.ReadUInt16(),
                        reserved = br.ReadUInt32(),
                        chgtime = br.ReadUInt32(),
                        chgdate = br.ReadUInt32(),
                        reserved2 = br.ReadUInt16()
                    }; // end - set file header info
                    #endregion SetClarionFileHeader

                    #region SetClarionFieldList
                    for (int i = 0; i < fh.numflds; i++) // begin - set fields info
                    {
                        var fld = new ClarionFileFields
                        {
                            fldtype = (ClarionFileData.ClarionDataTypes)br.ReadByte(),
                            fldname = Mazovia.GetString(br.ReadBytes(16)).Trim(),
                            foffset = br.ReadUInt16(),
                            length = br.ReadUInt16(),
                            decsig = br.ReadByte(),
                            decdec = br.ReadByte(),
                            arrnum = br.ReadUInt16(),
                            picnum = br.ReadUInt16(),
                        };
                        fldsList.Add(fld);
                    } // end - set fields info
                    #endregion SetClarionFieldList

                    #region SetClarionTableKeys
                    for (int i = 0; i < fh.numkeys; i++) // begin - set table keys
                    {
                        var keysect = new ClarionFileKeySect
                        {
                            numcomps = br.ReadByte(),
                            keyname = Mazovia.GetString(br.ReadBytes(16)),
                            comptype = br.ReadByte(),
                            complen = br.ReadByte(),
                            keyparts = new List<ClarionFileKeyPart>()
                        };
                        for (int j = 0; j < keysect.numcomps; j++)
                        {
                            var keypart = new ClarionFileKeyPart
                            {
                                fldtype = br.ReadByte(),
                                fldnum = br.ReadUInt16(),
                                elmoff = br.ReadUInt16(),
                                elmlen = br.ReadByte()
                            };
                            keysect.keyparts.Add(keypart);
                        }
                        keys.Add(keysect);
                    } // end - set table keys
                    #endregion SetClarionTableKeys

                    #region SetDataTableColumns
                    //begin - set in-memory table columns
                    foreach (var fld in fldsList)
                    {
                        try
                        {
                            switch (fld.fldtype)
                            {
                                case ClarionFileData.ClarionDataTypes.LONG: // type is 'LONG' uses 4 bytes 
                                                                            // i'm guessing "long" in clarion means date/time, but because i'm planning on using timespan for time
                                                                            // save as string?
                                                                            //dataTable.Columns.Add(fld.fldname, typeof(DateTime));
                                    dataTable.Columns.Add(fld.fldname, typeof(String));
                                    break;
                                case ClarionFileData.ClarionDataTypes.REAL: // type is 'REAL'
                                    dataTable.Columns.Add(fld.fldname, typeof(String));
                                    break;
                                case ClarionFileData.ClarionDataTypes.STRING: // type is 'STRING'
                                    dataTable.Columns.Add(fld.fldname, typeof(String));
                                    break;
                                case ClarionFileData.ClarionDataTypes.STRING_WITH_PICTURE_TOKEN: // type is 'STRING With PICTURE TOKEN'
                                    dataTable.Columns.Add(fld.fldname, typeof(String));
                                    break;
                                case ClarionFileData.ClarionDataTypes.BYTE: // type is 'BYTE'
                                    dataTable.Columns.Add(fld.fldname, typeof(Byte));
                                    break;
                                case ClarionFileData.ClarionDataTypes.SHORT: // type is 'SHORT'
                                                                             //dataTable.Columns.Add(fld.fldname, typeof(ushort));
                                    dataTable.Columns.Add(fld.fldname, typeof(UInt16));
                                    break;
                                case ClarionFileData.ClarionDataTypes.GROUP: // type is 'GROUP'
                                    dataTable.Columns.Add(fld.fldname, typeof(String));
                                    break;
                                case ClarionFileData.ClarionDataTypes.DECIMAL: // type is 'DECIMAL'
                                                                               // decimals are packed BCD-like format with 2 digits per byte
                                    dataTable.Columns.Add(fld.fldname, typeof(Decimal));
                                    break;
                                default: // No Ideal What this is. Throw error
                                    throw new ArgumentException("Field Type is unknown");
                                    break;
                            }
                        }
                        catch (System.Data.DuplicateNameException ex)
                        {
                            dupa++;
                            dataTable.Columns.Add(fld.fldname+ dupa.ToString(), typeof(String));
                        }
                    }
                    #endregion SetDataTableColumns

                    #region SetTableRows
                    // begin Set table rows
                    // Read the Whole file and add it to the Table
                    byte rec_status;
                    uint rec_ptr;
                    //for (uint rec_no = 0; rec_no < fh.numrecs; rec_no++)
                    for (uint rec_no = 0; rec_no < 100; rec_no++)
                    {
                        br.BaseStream.Seek(fh.offset + (fh.reclen * rec_no), SeekOrigin.Begin);
                        //Read a head by 5 bytes, as Clarion adds 5 bytes to the header of each record
                        rec_status = br.ReadByte();
                        rec_ptr = br.ReadUInt32();
                        DataRow newRow = dataTable.NewRow();
                        foreach (var fld in fldsList)
                        {
                            switch (fld.fldtype)
                            {
                                case ClarionFileData.ClarionDataTypes.LONG: // type is 'LONG' uses 4 bytes -- i'm guessing "long" in clarion means date/time
                                    uint fld_long = br.ReadUInt32();

                                    string long_type = String.Empty;
                                    // date and time information is from
                                    // Clarion TB 117
                                    if (fld_long > 109211) // The Max value for Date is 109211, if it's greater assume it's a time
                                    {                      // though techinally a time can be below 109211 -- but that is in range of 00:00:00 (midnight) to 00:18:12.11
                                        long_type = "time";// if you assume you multiply by 10 --- so an 18 minute range to assume it's a date is good by me
                                        var abstime = fld_long - 1;
                                        //Console.WriteLine(TimeSpan.FromMilliseconds(abstime * 10)); // it is saved as centiseconds, multiply by 10 to get milleseconds
                                        DateTime theTime = new DateTime(1700, 1, 1); // if date is 1700, 1, 1 then it's a time
                                        theTime += TimeSpan.FromMilliseconds(abstime * 10);
                                        //newRow[fld.fldname] = TimeSpan.FromMilliseconds(abstime * 10); //theTime;
                                        //var k = theTime.ToLongTimeString();
                                        newRow[fld.fldname] = theTime.ToLongTimeString();
                                    }
                                    else if (fld_long > 3) // dates can't be below 3
                                    {
                                        long_type = "date";
                                        //uint absday = fld_long > 36527 ? fld_long - 3 : fld_long - 4;
                                        //var year = (1801 + 4 * (absday / 1461));
                                        //absday = absday % 1461;
                                        //year += absday != 1460 ? (absday / 365) : 3;
                                        //var day = absday != 1460 ? (absday % 365) : 365;
                                        //DateTime theDate = new DateTime((int)year, 1, 1).AddDays(day); // if date is not 1700 then it it's a date
                                        DateTime theDate = new DateTime(1800, 12, 28).AddDays(fld_long); // fld_long is number of days since 28-Dec-1800
                                        //Console.WriteLine(theDate.ToString("M/d/yyyy"));
                                        newRow[fld.fldname] = theDate.ToString("M/d/yyyy");
                                    }
                                    else
                                    {
                                        long_type = "Bad Date/Time?";
                                        //Console.WriteLine();
                                        //newRow[fld.fldname] = new DateTime(1, 1, 1); // 1/1/1 will represent an invalid date
                                        newRow[fld.fldname] = String.Empty;
                                    }
                                    break;
                                case ClarionFileData.ClarionDataTypes.REAL: // type is 'REAL'
                                    //br.BaseStream.Seek(br.BaseStream.Position + fld.length, SeekOrigin.Begin);
                                    throw new NotImplementedException(String.Format("Type {0} is not implemented yet", fld.fldtype));
                                    break;
                                case ClarionFileData.ClarionDataTypes.STRING: // type is 'STRING'
                                    // strings are padded with spaces to their maximum length
                                    byte[] fld_bytes = br.ReadBytes(fld.length);
                                    newRow[fld.fldname] = Mazovia.GetString(fld_bytes);
                                    break;
                                case ClarionFileData.ClarionDataTypes.STRING_WITH_PICTURE_TOKEN: // type is 'STRING With PICTURE TOKEN'
                                    //br.BaseStream.Seek(br.BaseStream.Position + fld.length, SeekOrigin.Begin);
                                    throw new NotImplementedException(String.Format("Type {0} is not implemented yet", fld.fldtype));
                                    break;
                                case ClarionFileData.ClarionDataTypes.BYTE: // type is 'BYTE'
                                    byte fld_byte = br.ReadByte();
                                    newRow[fld.fldname] = fld_byte;
                                    break;
                                case ClarionFileData.ClarionDataTypes.SHORT: // type is 'SHORT'
                                    //var count = 0;
                                    //if (fld.arrnum != 0)
                                    //{
                                    //    for (int j = 0; j < (fld.length / sizeof(ushort)) - 1; j++)
                                    //    {
                                    //        //Console.Write("{0},", br.ReadUInt16());
                                    //        br.ReadUInt16();
                                    //        count++;
                                    //    }
                                    //}
                                    ushort fld_short = br.ReadUInt16();
                                    newRow[fld.fldname] = fld_short;
                                    break;
                                case ClarionFileData.ClarionDataTypes.GROUP: // type is 'GROUP'
                                    //br.BaseStream.Seek(br.BaseStream.Position + fld.length, SeekOrigin.Begin);
                                    throw new NotImplementedException(String.Format("Type {0} is not implemented yet", fld.fldtype));
                                    break;
                                case ClarionFileData.ClarionDataTypes.DECIMAL: // type is 'DECIMAL'
                                    // decimals are packed BCD-like format with 2 digits per byte
                                    var bcdByteArr = br.ReadBytes(fld.length);
                                    decimal clairionDecimalVal = 0;
                                    Stack<int> bcdByteStack = new Stack<int>();
                                    foreach (byte b in bcdByteArr) // unpack each byte in byte arr
                                    {
                                        bcdByteStack.Push((b & 0xf0) >> 4);  // get the first number
                                        bcdByteStack.Push(b & 0x0f); // get the second digit
                                    }
                                    for (int i = -1 * fld.decdec; i < fld.decsig; i++)
                                    {
                                        clairionDecimalVal += (decimal)(Math.Pow(10, i) * bcdByteStack.Pop());
                                    }
                                    int bcdByteStackLeft = bcdByteStack.Pop();
                                    if (bcdByteStackLeft > 0) // this appears to be what makes the value negative
                                    {
                                        clairionDecimalVal *= -1;
                                    }
                                    //if (clairionDecimalVal != 0)
                                    //    Console.WriteLine("{0}\t:{1}", fld.fldname, clairionDecimalVal);
                                    newRow[fld.fldname] = clairionDecimalVal;
                                    break;
                                default: // No Ideal What this is. Throw error
                                    throw new ArgumentException("Field Type is unknown");
                                    break;
                            }
                        }
                        dataTable.Rows.Add(newRow);
                    }
                    // end Set table rows
                    #endregion SetTableRows
                }); // end read the file for the header
        } // end method

        private void ReadDataFile(Action<BinaryReader> fileAction) // begin - open the file and perform action
        {
            Encoding Mazovia = new MazoviaEncoding();
            #region OpenFileStream
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (BinaryReader br = new BinaryReader(fs, Mazovia)) // begin -- open file stream
            {
                fileAction(br);
            } // end using of file stream
            #endregion OpenFileStream
        } // end - open the file with action
         

        // public methods
        public IEnumerable<DataRow> GetData()
        {
            setupDatatable();
            return dataTable.AsEnumerable();
        }
    }
    class MazoviaEncoding : Encoding
    {
        //https://stackoverflow.com/questions/13655614/unicode-to-mazovia-encoding-redundant-char
        private static int[] codePoints =  {
            0x0000,0x0001,0x0002,0x0003,0x0004,0x0005,0x0006,0x0007,0x0008,0x0009,0x000A,0x000B,0x000C,0x000D,0x000E,0x000F
            ,0x0010,0x0011,0x0012,0x0013,0x0014,0x0015,0x0016,0x0017,0x0018,0x0019,0x001A,0x001B,0x001C,0x001D,0x001E,0x001F
            ,0x0020,0x0021,0x0022,0x0023,0x0024,0x0025,0x0026,0x0027,0x0028,0x0029,0x002A,0x002B,0x002C,0x002D,0x002E,0x002F
            ,0x0030,0x0031,0x0032,0x0033,0x0034,0x0035,0x0036,0x0037,0x0038,0x0039,0x003A,0x003B,0x003C,0x003D,0x003E,0x003F
            ,0x0040,0x0041,0x0042,0x0043,0x0044,0x0045,0x0046,0x0047,0x0048,0x0049,0x004A,0x004B,0x004C,0x004D,0x004E,0x004F
            ,0x0050,0x0051,0x0052,0x0053,0x0054,0x0055,0x0056,0x0057,0x0058,0x0059,0x005A,0x005B,0x005C,0x005D,0x005E,0x005F
            ,0x0060,0x0061,0x0062,0x0063,0x0064,0x0065,0x0066,0x0067,0x0068,0x0069,0x006A,0x006B,0x006C,0x006D,0x006E,0x006F
            ,0x0070,0x0071,0x0072,0x0073,0x0074,0x0075,0x0076,0x0077,0x0078,0x0079,0x007A,0x007B,0x007C,0x007D,0x007E,0x007F
            ,0x00C7,0x00FC,0x00E9,0x00E2,0x00E4,0x00E0,0x0105,0x00E7,0x00EA,0x00EB,0x00E8,0x00EF,0x00EE,0x0107,0x00C4,0x0104
            ,0x0118,0x0119,0x0142,0x00F4,0x00F6,0x0106,0x00FB,0x00F9,0x015A,0x00D6,0x00DC,0x00A2,0x0141,0x00A5,0x015B,0x0192
            ,0x0179,0x017B,0x00F3,0x00D3,0x0144,0x0143,0x017A,0x017C,0x00BF,0x2310,0x00AC,0x00BD,0x00BC,0x00A1,0x00AB,0x00BB
            ,0x2591,0x2592,0x2593,0x2502,0x2524,0x2561,0x2562,0x2556,0x2555,0x2563,0x2551,0x2557,0x255D,0x255C,0x255B,0x2510
            ,0x2514,0x2534,0x252C,0x251C,0x2500,0x253C,0x255E,0x255F,0x255A,0x2554,0x2569,0x2566,0x2560,0x2550,0x256C,0x2567
            ,0x2568,0x2564,0x2565,0x2559,0x2558,0x2552,0x2553,0x256B,0x256A,0x2518,0x250C,0x2588,0x2584,0x258C,0x2590,0x2580
            ,0x03B1,0x00DF,0x0393,0x03C0,0x03A3,0x03C3,0x00B5,0x03C4,0x03A6,0x0398,0x03A9,0x03B4,0x221E,0x03C6,0x03B5,0x2229
            ,0x2261,0x00B1,0x2265,0x2264,0x2320,0x2321,0x00F7,0x2248,0x00B0,0x2219,0x00B7,0x221A,0x207F,0x00B2,0x25A0,0x00A0
        };

        private static Dictionary<char, byte> unicodeToByte;


        static MazoviaEncoding()
        {
            unicodeToByte = new Dictionary<char, byte>();

            for (int i = 0; i < codePoints.Length; ++i)
            {
                unicodeToByte.Add((char)codePoints[i], (byte)i);
            }

        }



        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            return byteCount;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
            {
                throw new ArgumentNullException();
            }
            if (charIndex + charCount > chars.Length ||
                charIndex < 0 ||
                byteIndex < 0 ||
                byteIndex + charCount > bytes.Length
                )
            {
                throw new ArgumentOutOfRangeException();
            }

            int total = 0;
            int j = 0;
            for (int i = charIndex; i < charIndex + charCount; ++i)
            {
                char cur = chars[i];
                byte asMazovia;
                if (!unicodeToByte.TryGetValue(cur, out asMazovia))
                {

                    asMazovia = (byte)0x003F; // "?"
                }
                total++;
                bytes[j + byteIndex] = asMazovia;
                j++;
            }
            return total;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (chars == null || bytes == null)
            {
                throw new ArgumentNullException();
            }
            if (byteIndex + byteCount > bytes.Length ||
                charIndex < 0 ||
                byteIndex < 0 ||
                charIndex + byteCount > chars.Length
                )
            {
                throw new ArgumentOutOfRangeException();
            }

            int total = 0;
            int j = 0;
            for (int i = byteIndex; i < byteIndex + byteCount; ++i)
            {
                byte cur = bytes[i];
                char decoded = (char)codePoints[cur];
                total++;
                chars[charIndex + j] = decoded;
                j++;

            }
            return total;
        }

        public override int GetByteCount(char[] charArray, int index, int count)
        {
            if (charArray == null)
            {
                throw new ArgumentNullException();
            }

            if (index + count <= charArray.Length && index >= 0 && count >= 0)
            {
                return count;
            }
            else
            {

                throw new ArgumentOutOfRangeException();
            }
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException();
            }

            if (index < 0 || count < 0 || index + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            return count;
        }



    }

}
