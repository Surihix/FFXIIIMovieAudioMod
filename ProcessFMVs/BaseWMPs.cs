using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FFXIIIMovieAudioMod
{
    internal class BaseWMPs
    {
        public static void Patch(string whiteDataDirVar, string movieDirVar, CmnMethods.VoCodes voCodeSwitchVar, string radToolsDirVar)
        {
            string[] wmpMovieDir = Directory.GetFiles(movieDirVar, "*.wmp", SearchOption.TopDirectoryOnly);
            var wmpList = new List<string>();

            string[] audioDirToCheck = { };
            var trackCheckList = new List<string>();


            var archiveVoCode = "";
            var fileVoCode = "";
            var audioTrackdDirVo = "";
            foreach (var wmpItem in wmpMovieDir)
            {
                var currentWmpItemName = new FileInfo(wmpItem).Name;

                switch (voCodeSwitchVar)
                {
                    case CmnMethods.VoCodes.us:
                        archiveVoCode = "u";
                        fileVoCode = "_us";
                        audioTrackdDirVo = "us";

                        CmnMethods.DirectoryExistsCheck(Directory.GetCurrentDirectory() + "\\audio_data\\us\\", "Audio folder for the selected voiceover is not present");

                        audioDirToCheck = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\audio_data\\us\\", "*.wav", SearchOption.AllDirectories);
                        CmnMethods.CheckAudioTracks(audioDirToCheck, trackCheckList, TracksList.tracks_us);

                        if (WMPsList.WMPs_us.Contains(currentWmpItemName))
                        {
                            wmpList.Add(currentWmpItemName);
                        }
                        break;

                    case CmnMethods.VoCodes.jp:
                        archiveVoCode = "c";
                        audioTrackdDirVo = "jp";

                        CmnMethods.DirectoryExistsCheck(Directory.GetCurrentDirectory() + "\\audio_data\\jp\\", "Audio folder for the selected voiceover is not present");

                        audioDirToCheck = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\audio_data\\jp\\", "*.wav", SearchOption.AllDirectories);
                        CmnMethods.CheckAudioTracks(audioDirToCheck, trackCheckList, TracksList.tracks_jp);

                        if (WMPsList.WMPs_jp.Contains(currentWmpItemName))
                        {
                            wmpList.Add(currentWmpItemName);
                        }
                        break;
                }
            }


            var dbFileWhitePath = "db\\resident\\movie_items" + fileVoCode + ".win32.wdb";
            var filelistFile = whiteDataDirVar + "\\sys\\filelist" + archiveVoCode + ".win32.bin";
            var whiteImgBinFile = whiteDataDirVar + "\\sys\\white_img" + archiveVoCode + ".win32.bin";

            CmnMethods.FileExistsCheck(filelistFile);
            CmnMethods.FileExistsCheck(whiteImgBinFile);

            WhiteBin.UnpkAFile(filelistFile, whiteImgBinFile, dbFileWhitePath);


            var dbFile = whiteDataDirVar + "\\sys\\white_img" + archiveVoCode + "_win32\\" + "db\\resident\\movie_items" + fileVoCode + ".win32.wdb";
            for (int w = 0; w < wmpList.Count; w++)
            {
                var currentWmpFileInList = movieDirVar + wmpList[w];
                UnpackWMP.UnpkSingle(dbFile, currentWmpFileInList);
                Console.WriteLine("");
                Console.WriteLine("");

                var currentWmpExtactedDir = Path.GetFileNameWithoutExtension(currentWmpFileInList).Replace(".win32", "") + "\\";
                var currentBinkAudioTracksDir = Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\" + Path.GetFileNameWithoutExtension(currentWmpFileInList).Replace(".win32", "");
                string[] binkFilesInFolder = Directory.GetFiles(movieDirVar + currentWmpExtactedDir, "*.bik", SearchOption.AllDirectories);

                foreach (var bink in binkFilesInFolder)
                {
                    var currentBinkFileName = new FileInfo(bink).Name;
                    var currentBinkFile = Path.GetFullPath(bink);


                    var trackNo = 1;
                    var currentBinkAudioTrackFile = "";
                    var binkTrackNo = 0;
                    Console.WriteLine("Patching audio tracks to " + currentBinkFileName + "....");
                    for (int a = 1; a < 5; a++)
                    {
                        currentBinkAudioTrackFile = currentBinkAudioTracksDir + "\\" + currentBinkFileName.Replace(".bik", "") + "_track-" + trackNo + ".wav";

                        var channelCount = 1;
                        if (binkTrackNo.Equals(0) || binkTrackNo.Equals(3))
                        {
                            channelCount = 2;
                        }

                        CmnMethods.BinkPatch(radToolsDirVar, currentBinkFile, currentBinkAudioTrackFile, binkTrackNo, channelCount);

                        binkTrackNo++;
                        trackNo++;
                    }

                    Console.WriteLine("Patched audio tracks to " + currentBinkFileName);
                    Console.WriteLine("");
                }

                Console.WriteLine("");

                var unpackedWmpDir = movieDirVar + "\\" + Path.GetFileNameWithoutExtension(currentWmpFileInList).Replace(".win32", "");
                RepackWMP.RpkWMP(dbFile, unpackedWmpDir);
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");

                Directory.Delete(unpackedWmpDir, true);
            }

            Console.WriteLine("Finished patching audio to all movie files");
            Console.WriteLine("");

            WhiteBin.RpkAFile(filelistFile, whiteImgBinFile, dbFileWhitePath);
            Directory.Delete(whiteDataDirVar + "\\sys\\white_img" + archiveVoCode + "_win32", true);

            Console.WriteLine("Repacked db file to archive");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Patching complete");
            CmnMethods.AppMsgBox("Patched in higher quality audio into the cutscenes", "Success", MessageBoxIcon.Information);
        }
    }
}