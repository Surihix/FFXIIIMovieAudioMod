using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FFXIIIMovieAudioMod
{
    internal class UnpkdFMVs
    {
        public static void NovaFMVs(string movieDirVar, CmnMethods.VoCodes voCodeSwitchVar, string radToolsDirVar)
        {
            string[] binkFMVsDir = Directory.GetFiles(movieDirVar, "*.bik", SearchOption.TopDirectoryOnly);
            var binkFMVsList = new List<string>();

            string[] audioDirToCheck = { };
            var trackCheckList = new List<string>();
            var audioTrackdDirVo = "";


            foreach (var binkFmv in binkFMVsDir)
            {
                var currentBinkFMVname = new FileInfo(binkFmv).Name;

                switch (voCodeSwitchVar)
                {
                    case CmnMethods.VoCodes.us:
                        audioTrackdDirVo = "us";
                        CmnMethods.DirectoryExistsCheck(Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\", "Audio folder for the selected voiceover is not present");

                        audioDirToCheck = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\", "*.wav", SearchOption.AllDirectories);
                        CmnMethods.CheckAudioTracks(audioDirToCheck, trackCheckList, TracksList.tracks_us);

                        if (FMVsList.FMVs_us.Contains(currentBinkFMVname))
                        {
                            binkFMVsList.Add(binkFmv);
                        }
                        break;

                    case CmnMethods.VoCodes.jp:
                        audioTrackdDirVo = "jp";
                        CmnMethods.DirectoryExistsCheck(Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\", "Audio folder for the selected voiceover is not present");

                        audioDirToCheck = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\", "*.wav", SearchOption.AllDirectories);
                        CmnMethods.CheckAudioTracks(audioDirToCheck, trackCheckList, TracksList.tracks_jp);

                        if (FMVsList.FMVs_jp.Contains(currentBinkFMVname))
                        {
                            binkFMVsList.Add(binkFmv);
                        }
                        break;
                }
            }

            if (binkFMVsList.Count.Equals(0))
            {
                CmnMethods.ErrorExit("Missing movie files\nPlease check if you have correctly unpacked the game data with the Nova mod manager before running this installer.");
            }

            var appAudioDir = Directory.GetCurrentDirectory() + "\\audio_data\\" + audioTrackdDirVo + "\\";
            string[] audioTracksDir = Directory.GetFiles(appAudioDir, "*.wav", SearchOption.AllDirectories);
            var audioTracksList = new List<string>();


            foreach (var audioTrack in audioTracksDir)
            {
                audioTracksList.Add(audioTrack);
            }

            for (int b = 0; b < binkFMVsList.Count; b++)
            {
                var currentBinkFileInList = binkFMVsList[b];
                var currentBinkFileInfo = new FileInfo(currentBinkFileInList);
                var currentBinkFileName = currentBinkFileInfo.Name;
                var currentBinkFileSize = currentBinkFileInfo.Length;

                if (currentBinkFileSize > 4264973152)
                {
                    Console.WriteLine("Skipped patching " + currentBinkFileName + " due to large size");
                }
                else
                {
                    Console.WriteLine("Patching audio tracks to " + currentBinkFileName + "....");

                    var currentBinkFile = Path.GetFullPath(currentBinkFileInList);
                    var trackNo = 1;
                    var currentBinkAudioTrackFile = "";
                    var currentBinkAudioTrackName = "";
                    var binkTrackNo = 0;

                    for (int a = 1; a < 5; a++)
                    {
                        currentBinkAudioTrackName = currentBinkFileName.Replace(".bik", "") + "_track-" + trackNo + ".wav";

                        foreach (var audioTrackFile in audioTracksList)
                        {
                            var cbaName = Path.GetFileName(audioTrackFile);

                            if (currentBinkAudioTrackName.Equals(cbaName))
                            {
                                currentBinkAudioTrackFile = audioTrackFile;
                            }
                        }

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
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Patching complete");
            CmnMethods.AppMsgBox("Patched in higher quality audio into the cutscenes", "Success", MessageBoxIcon.Information);
        }
    }
}