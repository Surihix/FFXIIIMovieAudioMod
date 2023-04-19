using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FFXIIIMovieAudioMod
{
    internal class CmnMethods
    {
        public static void ErrorExit(string message)
        {
            Console.WriteLine("Error: " + message);
            AppMsgBox(message, "Error", MessageBoxIcon.Error);
            Environment.Exit(0);
        }

        public static void AppMsgBox(string msgText, string msgTitle, MessageBoxIcon msgType)
        {
            MessageBox.Show(msgText, msgTitle, MessageBoxButtons.OK, msgType);
        }

        public enum VoCodes
        {
            us,
            jp
        }

        public enum FileType
        {
            Launcher,
            RadVideo
        }

        public static void DirectoryExistsCheck(string directoryPath, string errorMsg)
        {
            if (!Directory.Exists(directoryPath))
            {
                ErrorExit(errorMsg);
            }
        }

        public static void CheckAudioTracks(string[] dirToCheckVar, List<string> checkListNameVar, string[] mainTrackListVar)
        {
            foreach (var trackItem in dirToCheckVar)
            {
                var itemName = new FileInfo(trackItem).Name;
                checkListNameVar.Add(itemName);
            }

            foreach (var mainTrackItem in mainTrackListVar)
            {
                if (!checkListNameVar.Contains(mainTrackItem))
                {
                    CmnMethods.ErrorExit("Missing one or more audio tracks for the selected voice over in the audio folder");
                }
            }
        }

        public static void FileExistsCheck(string filePath)
        {
            if (!File.Exists(filePath))
            {
                ErrorExit("Missing file " + Path.GetFileName(filePath));
            }
        }

        public static void BEReader32(BinaryReader ReaderName, uint ReaderPos, out byte[] GetVarName, out uint VarName)
        {
            ReaderName.BaseStream.Position = ReaderPos;
            GetVarName = ReaderName.ReadBytes((int)ReaderName.BaseStream.Length);
            VarName = BinaryPrimitives.ReadUInt32BigEndian(GetVarName.AsSpan());
        }

        public static void NameBuilder(BinaryReader ReaderName, uint ReaderPos, out StringBuilder builderVar, out char CharVar)
        {
            ReaderName.BaseStream.Position = ReaderPos;
            builderVar = new StringBuilder();
            while ((CharVar = ReaderName.ReadChar()) != default)
            {
                builderVar.Append(CharVar);
            }
        }

        public static void BinkPatch(string radExeDirVar, string binkFileVar, string audioFileVar, int trackNoVar, int chnlCountVar)
        {
            using (Process radTool = new Process())
            {
                radTool.StartInfo.WorkingDirectory = radExeDirVar;
                radTool.StartInfo.FileName = "radvideo64.exe";
                var endSwitch = "/o /l0 /t" + trackNoVar + " /b16 /c" + chnlCountVar + " /#";
                radTool.StartInfo.Arguments = "BinkMix " + "\"" + binkFileVar + "\"" + " " + "\"" + audioFileVar + "\"" + " " + "\"" + binkFileVar + "\"" + " " + endSwitch;
                radTool.StartInfo.UseShellExecute = true;
                radTool.Start();
                radTool.WaitForExit();
            }
        }
    }
}