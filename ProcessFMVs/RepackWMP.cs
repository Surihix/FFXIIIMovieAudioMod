using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace FFXIIIMovieAudioMod
{
    internal class RepackWMP
    {
        public static void RpkWMP(string InDbFileVar, string UnPackedWMPdirVar)
        {

            // Get the unpacked WMP folder path, 
            // dir, and folder name
            var UnPackedWMPfolderPath = Path.GetFullPath(UnPackedWMPdirVar);
            var UnpackedWMPfolderDir = Path.GetDirectoryName(UnPackedWMPfolderPath) + "\\";
            var UnPackedWMPfolderName = new DirectoryInfo(UnPackedWMPdirVar).Name;

            var InDbFileName = Path.GetFileName(InDbFileVar);

            // Assign the movie file extension,
            // platform code, Vo code, and a bool for
            // checking if the unpacked folder exists or not
            var FolderExists = false;

            var PlatformCode = ".win32";
            var FMVFileExt = ".bik";
            var Vo = "";

            if (InDbFileName.Contains(".win32."))
            {
                PlatformCode = ".win32";
                FMVFileExt = ".bik";
            }

            if (InDbFileName.Contains("us."))
            {
                Vo = "_us";
            }

            if (File.Exists(UnpackedWMPfolderDir + UnPackedWMPfolderName + PlatformCode + ".wmp"))
            {
                File.Delete(UnpackedWMPfolderDir + UnPackedWMPfolderName + PlatformCode + ".wmp");
            }

            // Get total fmv count, and the start offset of the 
            // postion where the WMP names are stored
            using (FileStream DbStream = new FileStream(InDbFileVar, FileMode.Open, FileAccess.ReadWrite))
            {
                using (BinaryReader DbReader = new BinaryReader(DbStream))
                {
                    using (BinaryWriter DbWriter = new BinaryWriter(DbStream))
                    {
                        CmnMethods.BEReader32(DbReader, 4, out byte[] GetTotalFMVs, out uint TotalFMVs);
                        TotalFMVs -= 4;

                        CmnMethods.BEReader32(DbReader, 32, out byte[] GetWMPnamesListPos, out uint WMPnamesListPos);


                        // From the first FMV file name start value, start 
                        // the repacking process
                        uint StartVal = 144;
                        for (int i = 0; i < TotalFMVs; i++)
                        {
                            // Get the important values
                            CmnMethods.NameBuilder(DbReader, StartVal, out StringBuilder FMVnameBuilder, out char GetFMVname);
                            var FMVname = FMVnameBuilder.ToString();

                            CmnMethods.BEReader32(DbReader, StartVal + 16, out byte[] GetFMVInfoPos, out uint FMVInfoPos);
                            CmnMethods.BEReader32(DbReader, FMVInfoPos, out byte[] GetWMPNamePos, out uint WMPNamePos);

                            CmnMethods.NameBuilder(DbReader, WMPnamesListPos + WMPNamePos, out StringBuilder WMPnameBuilder, out char GetWMPname);
                            var CurrentWMPname = WMPnameBuilder.ToString();

                            // Get the internal FMV name from the specified
                            // movie file and adjust it to be same as the 
                            // internal FMV name
                            var CurrentFMVfile = FMVname + Vo + FMVFileExt;
                            var CurrentWMPfile = UnpackedWMPfolderDir + "\\" + CurrentWMPname + Vo + PlatformCode + ".wmp";
                            var AdjustedWMPfolderName = UnPackedWMPfolderName.Replace("_us", "");

                            // Set the folder exists bool to true
                            if (CurrentWMPname.Equals(AdjustedWMPfolderName))
                            {
                                FolderExists = true;
                            }

                            // According to the bool and if the movie file exists,
                            // start the repacking process
                            switch (FolderExists)
                            {
                                case true:
                                    if (File.Exists(UnPackedWMPdirVar + "\\" + CurrentFMVfile))
                                    {
                                        // If the wmp file does not exist, then
                                        // create a new one
                                        if (!File.Exists(CurrentWMPfile))
                                        {
                                            using (FileStream NewWMPfile = new FileStream(CurrentWMPfile, FileMode.OpenOrCreate, FileAccess.Write))
                                            {
                                                using (BinaryWriter HeaderWriter = new BinaryWriter(NewWMPfile))
                                                {
                                                    HeaderWriter.BaseStream.Position = 0;
                                                    byte[] wmp_header = { 87, 77, 80, 00, 86, 101, 114, 58, 48, 46, 48, 49, 00, 00, 00, 00 };
                                                    HeaderWriter.Write(wmp_header);
                                                }
                                            }
                                        }

                                        Console.WriteLine("Copying " + FMVname + Vo + FMVFileExt + " data to .wmp file....");

                                        // Get FMV file size and the WMP file size
                                        var FMVfileInfo = new FileInfo(UnPackedWMPdirVar + "\\" + CurrentFMVfile);
                                        var NewFMVdataSize = (uint)FMVfileInfo.Length;

                                        var WMPfileInfo = new FileInfo(CurrentWMPfile);
                                        var NewFMVdataStart = (ulong)WMPfileInfo.Length;

                                        // Start repacking the movie file to the WMP and 
                                        // update the db file
                                        using (FileStream WMPfile = new FileStream(CurrentWMPfile, FileMode.Append, FileAccess.Write))
                                        {
                                            using (FileStream MovieFile = new FileStream(UnPackedWMPdirVar + "\\" + CurrentFMVfile, FileMode.Open, FileAccess.Read))
                                            {
                                                WMPfile.Seek((long)NewFMVdataStart, SeekOrigin.Begin);
                                                MovieFile.CopyTo(WMPfile);

                                                Console.WriteLine("Copied data to .wmp file");
                                                Console.WriteLine("");

                                                BEWriter32(out byte[] AdjustFMVDatasize, NewFMVdataSize, DbWriter, FMVInfoPos + 4);
                                                BEWriter64(out byte[] AdjustFMVdataStart, NewFMVdataStart, DbWriter, FMVInfoPos + 8);
                                            }
                                        }
                                    }
                                    break;

                                case false:
                                    break;
                            }

                            StartVal += 32;
                        }

                        if (FolderExists.Equals(true))
                        {
                            Console.WriteLine("Offsets updated in " + InDbFileName);
                        }
                    }
                }
            }
        }

        static void BEWriter32(out byte[] AdjustByteVar, uint NewByteVar, BinaryWriter WriterName, uint WriterPos)
        {
            AdjustByteVar = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(AdjustByteVar, NewByteVar);
            WriterName.BaseStream.Position = WriterPos;
            WriterName.Write(AdjustByteVar);
        }

        static void BEWriter64(out byte[] AdjustByteVar, ulong NewByteVar, BinaryWriter WriterName, uint WriterPos)
        {
            AdjustByteVar = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(AdjustByteVar, NewByteVar);
            WriterName.BaseStream.Position = WriterPos;
            WriterName.Write(AdjustByteVar);
        }
    }
}