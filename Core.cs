using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FFXIIIMovieAudioMod
{
    internal class Core
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                CmnMethods.ErrorExit("Enough arguments not specified. missing vo language code or wmp type switches");
            }


            try
            {
                // args conversion
                var arg1 = args[0];
                var arg2 = args[1];

                var voCodeSwitch = CmnMethods.VoCodes.us;
                if (Enum.TryParse(arg1, false, out CmnMethods.VoCodes convertedVoSwitch))
                {
                    voCodeSwitch = convertedVoSwitch;
                }
                else
                {
                    CmnMethods.ErrorExit("Specified voice over code switch was invalid.\nShould be 'us' or 'jp'");
                }

                var wmpTypeSwitch = WmpTypes.unmodded;
                if (Enum.TryParse(arg2, false, out WmpTypes convertedWmpSwitch))
                {
                    wmpTypeSwitch = convertedWmpSwitch;
                }
                else
                {
                    CmnMethods.ErrorExit("Specified wmp type switch was invalid.\nShould be 'unmodded' or 'moddedHD'");
                }


                // radtools check and exe
                // path selection
                var radToolsExeDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\RADVideo\\radvideo64.exe";
                if (!File.Exists(radToolsExeDir))
                {
                    DialogResult dlr = MessageBox.Show("Unable to locate Bink's RAD video tools exe file in the default directory\nWould you like to manually locate this file?", "Locate radtools", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    if (dlr == DialogResult.OK)
                    {
                        FindExe.LocateFile("radvideo64.exe", CmnMethods.FileType.RadVideo);
                        radToolsExeDir = File.ReadAllText("..\\tempRadPath.txt").Trim();
                        File.Delete("..\\tempRadPath.txt");
                    }
                }
                var radToolDir = Path.GetDirectoryName(radToolsExeDir) + "\\";


                // Path selection
                var whiteDataDir = "";
                var movieDir = "";
                if (File.Exists("..\\LocatedPath.txt"))
                {
                    whiteDataDir = File.ReadAllText("..\\LocatedPath.txt").Trim() + "white_data";
                    movieDir = whiteDataDir + "\\movie\\";
                }
                else
                {
                    CmnMethods.AppMsgBox("Select the 'FFXiiiLauncher.exe' file in the FINAL FANTASY XIII game directory", "Locate launcher", MessageBoxIcon.Information);
                    FindExe.LocateFile("FFXiiiLauncher.exe", CmnMethods.FileType.Launcher);

                    whiteDataDir = File.ReadAllText("..\\LocatedPath.txt").Trim() + "white_data";
                    movieDir = whiteDataDir + "\\movie\\";
                }

                if (!Directory.Exists(movieDir))
                {
                    File.Delete("..\\LocatedPath.txt");
                    CmnMethods.ErrorExit("movie folder does not exist");
                }


                // Check for 6gb of free space in the drive
                // where the game is installed
                Console.WriteLine("");
                Console.WriteLine("Checking for free space in the drive where the game is installed");

                DriveInfo[] drive = DriveInfo.GetDrives();
                var driveLetter = drive.Where(x => x.Name == (movieDir.Substring(0, 3)));
                var driveFreeSpace = driveLetter.First().AvailableFreeSpace;

                if (driveFreeSpace < 6442450944)
                {
                    CmnMethods.ErrorExit("Not enough space available in the location where the game is installed.\nPlease ensure atleast 6gb of free space is present in the location.");
                }

                Console.WriteLine("Determined enough free space. proceeding....");
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");


                // Use the appropriate wmp patching classes
                switch (wmpTypeSwitch)
                {
                    case WmpTypes.unmodded:
                        BaseWMPs.Patch(whiteDataDir, movieDir, voCodeSwitch, radToolDir);
                        break;

                    case WmpTypes.moddedHD:
                        HDModWMPs.Patch(whiteDataDir, movieDir, voCodeSwitch, radToolDir);
                        break;

                    case WmpTypes.unpackedNova:
                        UnpkdFMVs.NovaFMVs(movieDir, voCodeSwitch, radToolDir);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (File.Exists("..\\CrashLog.txt"))
                {
                    File.Delete("..\\CrashLog.txt");
                }

                using (var sw = new StreamWriter("..\\CrashLog.txt", append: true))
                {
                    sw.WriteLine(ex);
                }

                Console.WriteLine("Crash details recorded in CrashLog.txt file");
                Console.WriteLine("");
                CmnMethods.ErrorExit(ex + "");
            }
        }

        enum WmpTypes
        {
            unmodded,
            moddedHD,
            unpackedNova
        }
    }
}