using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace FFXIIIMovieAudioMod
{
    internal class UnpackWMP
    {
        public static void UnpkSingle(string InDbFileVar, string InWMPfileVar)
        {

            // Get WMP file path and dir, the db
            // filename and WMP filename
            var InWMPfilePath = Path.GetFullPath(InWMPfileVar);
            var InWMPfileDir = Path.GetDirectoryName(InWMPfilePath) + "\\";

            string InDbFileName = Path.GetFileName(InDbFileVar);
            string InWMPfileName = Path.GetFileName(InWMPfileVar);

            // Assign the correct movie file extension,
            // platform code, and Vo code
            string FMVFileExt = ".bik";
            string PlatformCode = ".win32";
            string Vo = "";

            if (InDbFileName.Contains("us."))
            {
                Vo = "_us";
            }


            // Get total fmv count, and the start offset of the 
            // postion where the WMP names are stored
            using (FileStream DbStream = new FileStream(InDbFileVar, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader DbReader = new BinaryReader(DbStream))
                {
                    CmnMethods.BEReader32(DbReader, 4, out byte[] GetTotalFMVs, out uint TotalFMVs);
                    TotalFMVs -= 4;

                    CmnMethods.BEReader32(DbReader, 32, out byte[] GetWMPnamesListPos, out uint WMPnamesListPos);

                    // Generate the internal fixed WMP name
                    var FixedInWMPname = Path.GetFileNameWithoutExtension(InWMPfileVar).Replace(".win32", "").Replace("_us", "");
                    var WMPoutFolder = Path.GetFileNameWithoutExtension(InWMPfileVar).Replace(".win32", "");

                    // Create a new unpacked directory if it does not 
                    // exist
                    if (!Directory.Exists(InWMPfileDir + WMPoutFolder))
                    {
                        Directory.CreateDirectory(InWMPfileDir + WMPoutFolder);
                    }


                    // From the first FMV file name start value, start 
                    // the extraction process
                    uint StartVal = 144;
                    int FileCount = 0;
                    for (int i = 0; i < TotalFMVs; i++)
                    {
                        // Get the important values
                        CmnMethods.NameBuilder(DbReader, StartVal, out StringBuilder FMVnameBuilder, out char GetFMVname);
                        var FMVName = FMVnameBuilder.ToString();

                        CmnMethods.BEReader32(DbReader, StartVal + 16, out byte[] GetFMVInfoPos, out uint FMVInfoPos);
                        CmnMethods.BEReader32(DbReader, FMVInfoPos, out byte[] GetWMPNamePos, out uint WMPNamePos);

                        CmnMethods.NameBuilder(DbReader, WMPnamesListPos + WMPNamePos, out StringBuilder WMPnameBuilder, out char GetWMPname);
                        var CurrentWMPname = WMPnameBuilder.ToString();

                        CmnMethods.BEReader32(DbReader, FMVInfoPos + 4, out byte[] GetFMVSize, out uint FMVSize);
                        BEReader64(DbReader, FMVInfoPos + 8, out byte[] GetFMVdataStart, out ulong FMVDataStart);

                        // If the WMP name for the fmv file equals the fixed
                        // internal WMP name, then start the extraction process
                        if (CurrentWMPname.Equals(FixedInWMPname))
                        {
                            Console.WriteLine("Extracting file " + FMVName + Vo + FMVFileExt + "....");

                            using (FileStream WMPfile = new FileStream(InWMPfileDir + CurrentWMPname + Vo + PlatformCode + ".wmp", FileMode.Open, FileAccess.Read))
                            {
                                if (File.Exists(InWMPfileDir + WMPoutFolder + "/" + FMVName + Vo + FMVFileExt))
                                {
                                    File.Delete(InWMPfileDir + WMPoutFolder + "/" + FMVName + Vo + FMVFileExt);
                                }

                                using (FileStream FMVoutfile = new FileStream(InWMPfileDir + WMPoutFolder + "/" + FMVName + Vo + FMVFileExt, FileMode.Create, FileAccess.Write))
                                {
                                    WMPfile.CopyTo(FMVoutfile, (long)FMVDataStart, FMVSize);
                                }
                            }

                            Console.WriteLine("Extracted file " + FMVName + Vo + FMVFileExt);
                            Console.WriteLine("");

                            FileCount++;
                        }

                        StartVal += 32;
                    }

                    if (FileCount.Equals(0))
                    {
                        Console.WriteLine("Warning: No files were extracted from " + InWMPfileName);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Extracted " + FileCount + " file(s) from " + InWMPfileName);
                        return;
                    }
                }
            }
        }

        static void BEReader64(BinaryReader ReaderName, uint ReaderPos, out byte[] GetVarName, out ulong VarName)
        {
            ReaderName.BaseStream.Position = ReaderPos;
            GetVarName = ReaderName.ReadBytes((int)ReaderName.BaseStream.Length);
            VarName = BinaryPrimitives.ReadUInt64BigEndian(GetVarName.AsSpan());
        }
    }
}