using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FFXIIIMovieAudioMod
{
    internal class FindExe
    {
        public static void LocateFile(string fileToFindVar, CmnMethods.FileType fileTypeVar)
        {
            var t = new Thread(() =>
            {
                var pathSelect = new OpenFileDialog
                {
                    FileName = fileToFindVar,
                    Filter = fileToFindVar + $"|{fileToFindVar}",
                    RestoreDirectory = true
                };

                if (pathSelect.ShowDialog() == DialogResult.OK)
                {
                    string filePath = pathSelect.FileName;
                    string recordFolder = Path.GetDirectoryName(filePath);

                    switch (fileTypeVar)
                    {
                        case CmnMethods.FileType.Launcher:
                            PathWriter("..\\LocatedPath.txt", recordFolder);
                            break;

                        case CmnMethods.FileType.RadVideo:
                            PathWriter("..\\tempRadPath.txt", recordFolder);
                            break;
                    }
                }
                else
                {
                    CmnMethods.ErrorExit("Path selection was cancelled");
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }

        static void PathWriter(string txtFileNameVar, string recordFolderVar)
        {
            using (var dirRecord = new StreamWriter(txtFileNameVar))
            {
                dirRecord.Write(recordFolderVar);
                dirRecord.WriteLine("\\");
            }
        }
    }
}