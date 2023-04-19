using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace FFXIIIMovieAudioMod
{
    internal class WhiteBin
    {
        public static void UnpkAFile(string filelistFileVar, string whiteImgBinFileVar, string whiteFilePathVar)
        {
            // Set extract directory
            var InBinFilePath = Path.GetFullPath(whiteImgBinFileVar);
            var InBinFileDir = Path.GetDirectoryName(InBinFilePath);
            var Extract_dir_Name = Path.GetFileNameWithoutExtension(whiteImgBinFileVar).Replace(".win32", "_win32");
            var Extract_dir = InBinFileDir + "\\" + Extract_dir_Name;
            var DefaultChunksExtDir = Extract_dir + "\\_chunks";
            var ChunkFile = DefaultChunksExtDir + "\\chunk_";


            // Check and delete extracted directory if they exist in the
            // folder where they are supposed to be extracted
            if (Directory.Exists(Extract_dir))
            {
                Directory.Delete(Extract_dir, true);
                Console.Clear();
            }

            Directory.CreateDirectory(Extract_dir);
            Directory.CreateDirectory(DefaultChunksExtDir);


            // Process File chunks section
            // Intialize the variables required for extraction
            var ChunkFNameCount = (uint)0;
            var TotalChunks = (uint)0;
            var TotalFiles = (uint)0;

            using (FileStream Filelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader FilelistReader = new BinaryReader(Filelist))
                {
                    FilelistReader.BaseStream.Position = 0;
                    var chunksInfoStartPos = FilelistReader.ReadUInt32();
                    var chunksStartPos = FilelistReader.ReadUInt32();
                    TotalFiles = FilelistReader.ReadUInt32();

                    var ChunkInfo_size = chunksStartPos - chunksInfoStartPos;
                    TotalChunks = ChunkInfo_size / 12;

                    // Make a memorystream for holding all Chunks info
                    using (MemoryStream ChunkInfoStream = new MemoryStream())
                    {
                        Filelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                        byte[] ChunkInfoBuffer = new byte[ChunkInfo_size];
                        var ChunkBytesRead = Filelist.Read(ChunkInfoBuffer, 0, ChunkInfoBuffer.Length);
                        ChunkInfoStream.Write(ChunkInfoBuffer, 0, ChunkBytesRead);

                        // Make memorystream for all Chunks compressed data
                        using (MemoryStream ChunkStream = new MemoryStream())
                        {
                            Filelist.Seek(chunksStartPos, SeekOrigin.Begin);
                            Filelist.CopyTo(ChunkStream);

                            // Open a binary reader and read each chunk's info and
                            // dump them as separate files
                            using (BinaryReader ChunkInfoReader = new BinaryReader(ChunkInfoStream))
                            {
                                var ChunkInfoReadVal = (uint)0;
                                for (int c = 0; c < TotalChunks; c++)
                                {
                                    ChunkInfoReader.BaseStream.Position = ChunkInfoReadVal + 4;
                                    var ChunkCmpSize = ChunkInfoReader.ReadUInt32();
                                    var ChunkDataStart = ChunkInfoReader.ReadUInt32();

                                    ChunkStream.Seek(ChunkDataStart, SeekOrigin.Begin);
                                    using (MemoryStream ChunkToDcmp = new MemoryStream())
                                    {
                                        byte[] ChunkBuffer = new byte[ChunkCmpSize];
                                        var ReadCmpBytes = ChunkStream.Read(ChunkBuffer, 0, ChunkBuffer.Length);
                                        ChunkToDcmp.Write(ChunkBuffer, 0, ReadCmpBytes);

                                        using (FileStream ChunksOutStream = new FileStream(ChunkFile + ChunkFNameCount,
                                            FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                        {
                                            ChunkToDcmp.Seek(0, SeekOrigin.Begin);
                                            ZlibLibrary.ZlibDecompress(ChunkToDcmp, ChunksOutStream);
                                        }
                                    }

                                    ChunkInfoReadVal += 12;
                                    ChunkFNameCount++;
                                }
                            }
                        }
                    }
                }
            }


            // Extracting files section 
            ChunkFNameCount = 0;
            var CountDuplicate = 1;
            for (int ch = 0; ch < TotalChunks; ch++)
            {
                // Get the total number of files in a chunk file by counting the number of times
                // an null character occurs in the chunk file
                var FilesInChunkCount = (uint)0;
                using (StreamReader FileCountReader = new StreamReader(DefaultChunksExtDir + "/chunk_" + ChunkFNameCount))
                {
                    while (!FileCountReader.EndOfStream)
                    {
                        var CurrentNullChar = FileCountReader.Read();
                        if (CurrentNullChar == 0)
                        {
                            FilesInChunkCount++;
                        }
                    }
                }

                // Open a chunk file for reading
                using (FileStream CurrentChunk = new FileStream(ChunkFile + ChunkFNameCount, FileMode.Open, FileAccess.Read))
                {
                    using (BinaryReader ChunkStringReader = new BinaryReader(CurrentChunk))
                    {
                        var ChunkStringReaderPos = (uint)0;
                        for (int f = 0; f < FilesInChunkCount; f++)
                        {
                            ChunkStringReader.BaseStream.Position = ChunkStringReaderPos;
                            var ParsedString = new StringBuilder();
                            char GetParsedString;
                            while ((GetParsedString = ChunkStringReader.ReadChar()) != default)
                            {
                                ParsedString.Append(GetParsedString);
                            }
                            var Parsed = ParsedString.ToString();

                            if (Parsed.StartsWith("end"))
                            {
                                break;
                            }

                            string[] data = Parsed.Split(':');
                            var Pos = Convert.ToUInt32(data[0], 16) * 2048;
                            var UncmpSize = Convert.ToUInt32(data[1], 16);
                            var CmpSize = Convert.ToUInt32(data[2], 16);
                            var MainPath = data[3].Replace("/", "\\");

                            var DirectoryPath = Path.GetDirectoryName(MainPath);
                            var FileName = Path.GetFileName(MainPath);
                            var FullFilePath = Extract_dir + "\\" + DirectoryPath + "\\" + FileName;
                            var CompressedState = false;

                            if (!UncmpSize.Equals(CmpSize))
                            {
                                CompressedState = true;
                            }
                            else
                            {
                                CompressedState = false;
                            }

                            // Extract a specific file
                            if (MainPath.Equals(whiteFilePathVar))
                            {
                                using (FileStream Bin = new FileStream(whiteImgBinFileVar, FileMode.Open, FileAccess.Read))
                                {
                                    if (!Directory.Exists(Extract_dir + "\\" + DirectoryPath))
                                    {
                                        Directory.CreateDirectory(Extract_dir + "\\" + DirectoryPath);
                                    }
                                    if (File.Exists(FullFilePath))
                                    {
                                        File.Delete(FullFilePath);
                                        CountDuplicate++;
                                    }

                                    switch (CompressedState)
                                    {
                                        case true:
                                            using (MemoryStream CmpData = new MemoryStream())
                                            {
                                                Bin.CopyTo(CmpData, Pos, CmpSize);

                                                using (FileStream OutFile = new FileStream(FullFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                                {
                                                    CmpData.Seek(0, SeekOrigin.Begin);
                                                    ZlibLibrary.ZlibDecompress(CmpData, OutFile);
                                                }
                                            }
                                            break;

                                        case false:
                                            using (FileStream OutFile = new FileStream(FullFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                                            {
                                                OutFile.Seek(0, SeekOrigin.Begin);
                                                Bin.CopyTo(OutFile, Pos, UncmpSize);
                                            }
                                            break;
                                    }
                                }
                            }

                            ChunkStringReaderPos = (uint)ChunkStringReader.BaseStream.Position;
                        }
                    }
                }

                ChunkFNameCount++;
            }

            Directory.Delete(DefaultChunksExtDir, true);
        }


        public static void RpkAFile(string filelistFileVar, string whiteImgBinFileVar, string whiteFilePathVar)
        {
            // Replace the slashes to ones that are similar to what is used for
            // the file path strings in the chunks
            whiteFilePathVar = whiteFilePathVar.Replace("\\", "/");

            // Set the filelist name
            var FilelistName = Path.GetFileName(filelistFileVar);

            // Set directories and file paths for the filelist files,
            // the extracted white bin folder, and other temp files
            var InFilelistFilePath = Path.GetFullPath(filelistFileVar);
            var InFilelistFileDir = Path.GetDirectoryName(InFilelistFilePath);

            var Extracted_DirName = Path.GetFileName(whiteImgBinFileVar).Replace(".win32.bin", "_win32");
            var WhiteBinFolderName = Path.GetFileName(Extracted_DirName);
            var Extracted_Dir = InFilelistFileDir + "\\" + Extracted_DirName;

            var TmpCmpDataFile = Extracted_Dir + "\\CmpData";
            var TmpCmpChunkFile = Extracted_Dir + "\\CmpChunk";
            var DefaultChunksExtDir = Extracted_Dir + "\\_default_chunks";
            var NewChunksExtDir = Extracted_Dir + "\\_new_chunks";

            var DefChunkFile = DefaultChunksExtDir + "\\chunk_";
            var NewChunkFile = NewChunksExtDir + "\\chunk_";
            var NewFileListFile = InFilelistFileDir + "\\" + FilelistName + ".new";


            // Check and delete extracted chunk directory if they exist in the
            // folder where they are supposed to be extracted
            if (Directory.Exists(DefaultChunksExtDir))
            {
                Directory.Delete(DefaultChunksExtDir, true);
            }
            Directory.CreateDirectory(DefaultChunksExtDir);

            if (Directory.Exists(NewChunksExtDir))
            {
                Directory.Delete(NewChunksExtDir, true);
            }
            Directory.CreateDirectory(NewChunksExtDir);


            // Initialise variables to commonly use
            var chunksInfoStartPos = (uint)0;
            var chunksStartPos = (uint)0;
            var ChunkFNameCount = (uint)0;
            var TotalChunks = (uint)0;
            var LastChunkFileNumber = (uint)1000;

            // Set the values to the initialised variables
            using (FileStream BaseFilelist = new FileStream(filelistFileVar, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader BaseFilelistReader = new BinaryReader(BaseFilelist))
                {
                    BaseFilelistReader.BaseStream.Position = 0;
                    chunksInfoStartPos = BaseFilelistReader.ReadUInt32();
                    chunksStartPos = BaseFilelistReader.ReadUInt32();
                    var TotalFiles = BaseFilelistReader.ReadUInt32();

                    var ChunkInfo_size = chunksStartPos - chunksInfoStartPos;
                    TotalChunks = ChunkInfo_size / 12;

                    // Make a memorystream for holding all Chunks info
                    using (MemoryStream ChunkInfoStream = new MemoryStream())
                    {
                        BaseFilelist.Seek(chunksInfoStartPos, SeekOrigin.Begin);
                        byte[] ChunkInfoBuffer = new byte[ChunkInfo_size];
                        var ChunkBytesRead = BaseFilelist.Read(ChunkInfoBuffer, 0, ChunkInfoBuffer.Length);
                        ChunkInfoStream.Write(ChunkInfoBuffer, 0, ChunkBytesRead);

                        // Make memorystream for all Chunks compressed data
                        using (MemoryStream ChunkStream = new MemoryStream())
                        {
                            BaseFilelist.Seek(chunksStartPos, SeekOrigin.Begin);
                            BaseFilelist.CopyTo(ChunkStream);

                            // Open a binary reader and read each chunk's info and
                            // dump them as separate files
                            using (BinaryReader ChunkInfoReader = new BinaryReader(ChunkInfoStream))
                            {
                                var ChunkInfoReadVal = (uint)0;
                                for (int c = 0; c < TotalChunks; c++)
                                {
                                    ChunkInfoReader.BaseStream.Position = ChunkInfoReadVal + 4;
                                    var ChunkCmpSize = ChunkInfoReader.ReadUInt32();
                                    var ChunkDataStart = ChunkInfoReader.ReadUInt32();

                                    ChunkStream.Seek(ChunkDataStart, SeekOrigin.Begin);
                                    using (MemoryStream ChunkToDcmp = new MemoryStream())
                                    {
                                        byte[] ChunkBuffer = new byte[ChunkCmpSize];
                                        var ReadCmpBytes = ChunkStream.Read(ChunkBuffer, 0, ChunkBuffer.Length);
                                        ChunkToDcmp.Write(ChunkBuffer, 0, ReadCmpBytes);

                                        using (FileStream ChunksOutStream = new FileStream(DefChunkFile + ChunkFNameCount,
                                            FileMode.OpenOrCreate, FileAccess.ReadWrite))
                                        {
                                            ChunkToDcmp.Seek(0, SeekOrigin.Begin);
                                            ZlibLibrary.ZlibDecompress(ChunkToDcmp, ChunksOutStream);
                                        }
                                    }

                                    ChunkInfoReadVal += 12;
                                    ChunkFNameCount++;
                                }
                            }
                        }
                    }


                    // Compress each file into the white image archive section
                    // Open a chunk file and start the repacking process
                    ChunkFNameCount = 0;
                    for (int ch = 0; ch < TotalChunks; ch++)
                    {
                        // Get the total number of files in a chunk file by counting the number of times
                        // an null character occurs in the chunk file
                        var FilesInChunkCount = (uint)0;
                        using (StreamReader FileCountReader = new StreamReader(DefChunkFile + ChunkFNameCount))
                        {
                            while (!FileCountReader.EndOfStream)
                            {
                                var CurrentNullChar = FileCountReader.Read();
                                if (CurrentNullChar == 0)
                                {
                                    FilesInChunkCount++;
                                }
                            }
                        }

                        // Open a chunk file for reading
                        using (FileStream CurrentChunk = new FileStream(DefChunkFile + ChunkFNameCount, FileMode.Open, FileAccess.Read))
                        {
                            using (BinaryReader ChunkStringReader = new BinaryReader(CurrentChunk))
                            {
                                // Create a new chunk file with append mode for writing updated values back to the 
                                // filelist file
                                using (FileStream UpdChunk = new FileStream(NewChunkFile + ChunkFNameCount, FileMode.Append, FileAccess.Write))
                                {
                                    using (StreamWriter UpdChunkStrings = new StreamWriter(UpdChunk))
                                    {

                                        // Compress files in a chunk into the archive 
                                        var ChunkStringReaderPos = (uint)0;
                                        for (int f = 0; f < FilesInChunkCount; f++)
                                        {
                                            ChunkStringReader.BaseStream.Position = ChunkStringReaderPos;
                                            var ParsedString = new StringBuilder();
                                            char GetParsedString;
                                            while ((GetParsedString = ChunkStringReader.ReadChar()) != default)
                                            {
                                                ParsedString.Append(GetParsedString);
                                            }
                                            var Parsed = ParsedString.ToString();

                                            if (Parsed.StartsWith("end"))
                                            {
                                                UpdChunkStrings.Write("end\0");
                                                LastChunkFileNumber = ChunkFNameCount;
                                                break;
                                            }

                                            string[] data = Parsed.Split(':');
                                            var OgFilePos = Convert.ToUInt32(data[0], 16) * 2048;
                                            var OgUSize = Convert.ToUInt32(data[1], 16);
                                            var OgCSize = Convert.ToUInt32(data[2], 16);
                                            var MainPath = data[3];
                                            var DirectoryPath = Path.GetDirectoryName(MainPath);
                                            var FileName = Path.GetFileName(MainPath);
                                            var FullFilePath = Extracted_Dir + "\\" + DirectoryPath + "\\" + FileName;

                                            // Assign values to the variables to ensure that 
                                            // they get modified only when the file to repack
                                            // is found
                                            uint NewFilePos = OgFilePos;
                                            uint NewUcmpSize = OgUSize;
                                            uint NewCmpSize = OgCSize;
                                            var AsciCmpSize = "";
                                            var AsciUcmpSize = "";
                                            var AsciFilePos = "";
                                            var PackedState = "";
                                            var PackedAs = "";
                                            var CompressedState = false;

                                            if (!OgUSize.Equals(OgCSize))
                                            {
                                                CompressedState = true;
                                                PackedState = "Compressed";
                                            }
                                            else
                                            {
                                                CompressedState = false;
                                                PackedState = "Copied";
                                            }

                                            // Repack a specific file
                                            if (MainPath.Equals(whiteFilePathVar))
                                            {
                                                using (FileStream CleanBin = new FileStream(whiteImgBinFileVar, FileMode.Open, FileAccess.Write))
                                                {
                                                    CleanBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                    for (int pad = 0; pad < OgCSize; pad++)
                                                    {
                                                        CleanBin.WriteByte(0);
                                                    }
                                                }

                                                // According to the compressed state, compress or
                                                // copy the file
                                                switch (CompressedState)
                                                {
                                                    case true:
                                                        // Compress the file and get its uncompressed
                                                        // and compressed size
                                                        var CreateFile = File.Create(TmpCmpDataFile);
                                                        CreateFile.Close();

                                                        ZlibLibrary.ZlibCompress(FullFilePath, TmpCmpDataFile, Ionic.Zlib.CompressionLevel.Level9);

                                                        FileInfo UcmpDataInfo = new FileInfo(FullFilePath);
                                                        NewUcmpSize = (uint)UcmpDataInfo.Length;

                                                        FileInfo CmpDataInfo = new FileInfo(TmpCmpDataFile);
                                                        NewCmpSize = (uint)CmpDataInfo.Length;

                                                        // Open the compressed file in a stream and
                                                        // decide whether to inject or append the
                                                        // compressed file
                                                        using (FileStream CmpDataStream = new FileStream(TmpCmpDataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                                                        {
                                                            // If file is smaller or same as original, then inject
                                                            // the file at the original position
                                                            if (NewCmpSize < OgCSize || NewCmpSize.Equals(OgCSize))
                                                            {
                                                                PackedAs = " (Injected)";
                                                                NewFilePos = OgFilePos;

                                                                using (FileStream InjectWhiteBin = new FileStream(whiteImgBinFileVar, FileMode.Open, FileAccess.Write))
                                                                {
                                                                    InjectWhiteBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                                    CmpDataStream.CopyTo(InjectWhiteBin);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // If file is larger, then append the
                                                                // file at the end
                                                                using (FileStream AppendWhiteBin = new FileStream(whiteImgBinFileVar, FileMode.Append, FileAccess.Write))
                                                                {
                                                                    PackedAs = " (Appended)";
                                                                    NewFilePos = (uint)AppendWhiteBin.Length;

                                                                    // Check if file position is divisible by 2048
                                                                    // and if its not divisible, add in null bytes
                                                                    // till next closest divisible number
                                                                    if (NewFilePos % 2048 != 0)
                                                                    {
                                                                        var Remainder = NewFilePos % 2048;
                                                                        var IncreaseBytes = 2048 - Remainder;
                                                                        var NewPos = NewFilePos + IncreaseBytes;
                                                                        var PadNulls = NewPos - NewFilePos;

                                                                        AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                        for (int pad = 0; pad < PadNulls; pad++)
                                                                        {
                                                                            AppendWhiteBin.WriteByte(0);
                                                                        }
                                                                        NewFilePos = (uint)AppendWhiteBin.Length;
                                                                    }

                                                                    AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                    CmpDataStream.CopyTo(AppendWhiteBin);
                                                                }
                                                            }
                                                        }
                                                        File.Delete(TmpCmpDataFile);
                                                        break;

                                                    case false:
                                                        // Get the file size and copy the file
                                                        FileInfo CopyTypeFileInfo = new FileInfo(FullFilePath);
                                                        NewUcmpSize = (uint)CopyTypeFileInfo.Length;
                                                        NewCmpSize = NewUcmpSize;

                                                        // Open the file in a stream and decide whether
                                                        // to inject or append the compressed file
                                                        using (FileStream CopyTypeFileStream = new FileStream(FullFilePath, FileMode.Open, FileAccess.Read))
                                                        {
                                                            // If file is smaller or same as original, then inject
                                                            // the file at the original position
                                                            if (NewUcmpSize < OgUSize || NewUcmpSize == OgUSize)
                                                            {
                                                                PackedAs = " (Injected)";
                                                                NewFilePos = OgFilePos;

                                                                using (FileStream InjectWhiteBin = new FileStream(whiteImgBinFileVar, FileMode.Open, FileAccess.Write))
                                                                {
                                                                    InjectWhiteBin.Seek(OgFilePos, SeekOrigin.Begin);
                                                                    CopyTypeFileStream.CopyTo(InjectWhiteBin);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // If file is larger, then append
                                                                // the file at the end
                                                                using (FileStream AppendWhiteBin = new FileStream(whiteImgBinFileVar, FileMode.Append, FileAccess.Write))
                                                                {
                                                                    PackedAs = " (Appended)";
                                                                    NewFilePos = (uint)AppendWhiteBin.Length;

                                                                    // Check if file position is divisible by 2048
                                                                    // and if its not divisible, add in null bytes
                                                                    // till next closest divisible number
                                                                    if (NewFilePos % 2048 != 0)
                                                                    {
                                                                        var Remainder = NewFilePos % 2048;
                                                                        var IncreaseBytes = 2048 - Remainder;
                                                                        var NewPos = NewFilePos + IncreaseBytes;
                                                                        var PadNulls = NewPos - NewFilePos;

                                                                        AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                        for (int pad = 0; pad < PadNulls; pad++)
                                                                        {
                                                                            AppendWhiteBin.WriteByte(0);
                                                                        }
                                                                        NewFilePos = (uint)AppendWhiteBin.Length;
                                                                    }

                                                                    AppendWhiteBin.Seek(NewFilePos, SeekOrigin.Begin);
                                                                    CopyTypeFileStream.CopyTo(AppendWhiteBin);
                                                                }
                                                            }
                                                        }
                                                        break;
                                                }

                                                Console.WriteLine(PackedState + " " + WhiteBinFolderName + "/" + MainPath + PackedAs);
                                            }

                                            NewFilePos /= 2048;
                                            DecToHex(NewFilePos, ref AsciFilePos);
                                            DecToHex(NewUcmpSize, ref AsciUcmpSize);
                                            DecToHex(NewCmpSize, ref AsciCmpSize);

                                            var NewUpdatedPath = AsciFilePos + ":" + AsciUcmpSize + ":" + AsciCmpSize + ":" + MainPath + "\0";
                                            UpdChunkStrings.Write(NewUpdatedPath);

                                            ChunkStringReaderPos = (uint)ChunkStringReader.BaseStream.Position;
                                        }
                                    }
                                }
                            }
                        }

                        ChunkFNameCount++;
                    }


                    // Fileinfo updating and chunk compression section
                    // Copy the base filelist file's data into the new filelist file till the chunk data begins
                    var AppendAt = (uint)0;
                    using (FileStream NewFilelist = new FileStream(NewFileListFile, FileMode.Append, FileAccess.Write))
                    {
                        using (BinaryWriter NewFilelistWriter = new BinaryWriter(NewFilelist))
                        {
                            BaseFilelist.Seek(0, SeekOrigin.Begin);
                            byte[] NewFilelistBuffer = new byte[chunksStartPos];
                            var NewFilelistBytesRead = BaseFilelist.Read(NewFilelistBuffer, 0, NewFilelistBuffer.Length);
                            NewFilelist.Write(NewFilelistBuffer, 0, NewFilelistBytesRead);

                            // Compress and append multiple chunks to the new filelist file
                            ChunkFNameCount = 0;
                            var ChunkInfoWriterPos = chunksInfoStartPos;
                            var ChunkCmpSize = (uint)0;
                            var ChunkUncmpSize = (uint)0;
                            var ChunkStartVal = (uint)0;
                            var FileInfoWriterPos = 18;
                            for (int Ac = 0; Ac < TotalChunks; Ac++)
                            {
                                // Get total number of files in the chunk and decrease the filecount by 1 if the 
                                // the lastchunk number matches with the current chunk number running in this for loop
                                var FilesInChunkCount = (uint)0;
                                using (StreamReader FileCountReader = new StreamReader(NewChunkFile + ChunkFNameCount))
                                {
                                    while (!FileCountReader.EndOfStream)
                                    {
                                        var CurrentNullChar = FileCountReader.Read();
                                        if (CurrentNullChar == 0)
                                        {
                                            FilesInChunkCount++;
                                        }
                                    }
                                }

                                if (LastChunkFileNumber.Equals(ChunkFNameCount))
                                {
                                    FilesInChunkCount--;
                                }

                                // Get each file strings start position in a chunk and update the position
                                // value in the info section of the new filelist file
                                using (FileStream FileStrings = new FileStream(NewChunkFile + ChunkFNameCount, FileMode.Open, FileAccess.Read))
                                {
                                    using (BinaryReader FileStringsReader = new BinaryReader(FileStrings))
                                    {
                                        var FilePosInChunk = (UInt16)0;
                                        var FilePosInChunkToWrite = (UInt16)0;
                                        for (int Fic = 0; Fic < FilesInChunkCount; Fic++)
                                        {
                                            AdjustBytesUInt16(NewFilelistWriter, FileInfoWriterPos, out byte[] AdjustFilePosInChunk, FilePosInChunkToWrite);

                                            FileStringsReader.BaseStream.Position = FilePosInChunk;
                                            var ParsedVal = new StringBuilder();
                                            char GetParsedVal;
                                            while ((GetParsedVal = FileStringsReader.ReadChar()) != default)
                                            {
                                                ParsedVal.Append(GetParsedVal);
                                            }

                                            FilePosInChunk = (UInt16)FileStringsReader.BaseStream.Position;
                                            FilePosInChunkToWrite = (UInt16)FileStringsReader.BaseStream.Position;
                                            FileInfoWriterPos += 8;
                                        }
                                    }
                                }


                                // Compress and package a chunk back into the new filelist file and update the 
                                // offsets in the chunk info section of the filelist file
                                AppendAt = (uint)NewFilelist.Length;
                                NewFilelist.Seek(AppendAt, SeekOrigin.Begin);

                                FileInfo ChunkDataInfo = new FileInfo(NewChunkFile + ChunkFNameCount);
                                ChunkUncmpSize = (uint)ChunkDataInfo.Length;

                                var CreateChunkFile = File.Create(TmpCmpChunkFile);
                                CreateChunkFile.Close();

                                ZlibLibrary.ZlibCompress(NewChunkFile + ChunkFNameCount, TmpCmpChunkFile, Ionic.Zlib.CompressionLevel.Level9);

                                using (FileStream CmpChunkDataStream = new FileStream(TmpCmpChunkFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    CmpChunkDataStream.Seek(0, SeekOrigin.Begin);
                                    CmpChunkDataStream.CopyTo(NewFilelist);

                                    FileInfo CmpChunkDataInfo = new FileInfo(TmpCmpChunkFile);
                                    ChunkCmpSize = (uint)CmpChunkDataInfo.Length;
                                }
                                File.Delete(TmpCmpChunkFile);

                                AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos, out byte[] AdjustChunkUnCmpSize, ChunkUncmpSize, "le");
                                AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos + 4, out byte[] AdjustChunkCmpSize, ChunkCmpSize, "le");
                                AdjustBytesUInt32(NewFilelistWriter, ChunkInfoWriterPos + 8, out byte[] AdjustChunkStart, ChunkStartVal, "le");

                                var NewChunkStartVal = ChunkStartVal + ChunkCmpSize;
                                ChunkStartVal = NewChunkStartVal;

                                ChunkInfoWriterPos += 12;
                                ChunkFNameCount++;
                            }
                        }
                    }
                }
            }

            Directory.Delete(DefaultChunksExtDir, true);
            Directory.Delete(NewChunksExtDir, true);


            // Delete the old filelist file and rename the new filelist file
            // to the old filelist file name
            File.Delete(filelistFileVar);
            File.Move(NewFileListFile, filelistFileVar);
        }

        static void DecToHex(uint DecValue, ref string HexValue)
        {
            HexValue = DecValue.ToString("x");
        }

        static void AdjustBytesUInt16(BinaryWriter WriterName, int WriterPos, out byte[] AdjustByteVar,
            ushort NewAdjustVar)
        {
            WriterName.BaseStream.Position = WriterPos;
            AdjustByteVar = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(AdjustByteVar, NewAdjustVar);
            WriterName.Write(AdjustByteVar);
        }

        static void AdjustBytesUInt32(BinaryWriter WriterName, uint WriterPos, out byte[] AdjustByteVar,
            uint NewAdjustVar, string EndianType)
        {
            WriterName.BaseStream.Position = WriterPos;
            AdjustByteVar = new byte[4];
            switch (EndianType)
            {
                case "le":
                    BinaryPrimitives.WriteUInt32LittleEndian(AdjustByteVar, NewAdjustVar);
                    break;
                case "be":
                    BinaryPrimitives.WriteUInt32BigEndian(AdjustByteVar, NewAdjustVar);
                    break;
            }
            WriterName.Write(AdjustByteVar);
        }
    }
}