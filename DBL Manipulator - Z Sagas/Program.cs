using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DBL_Manipulator___Z_Sagas
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Dragon Ball Z Sagas DBL Manipulator\n" +
                   "Bit.Raiden 2021\n" +
                   "Version 1.0\n" +
                   "Texture structure help: MummRa(STR-BR) and Tuliopiloto\n\n" +
                   "Choose a operation:\n\n" +
                   "1 - Extract DBL's Textures\n" +
                   "2 - Repack DBL's Textures\n" +
                   "3 - Exit\n\nType the operation code: ");
            switch (Console.ReadLine())
            {
                case "1":
                    ExtractDBL();
                    break;

                case "2":
                    RepackDBL();
                    break;

                case "3":
                    Environment.Exit(0);
                    break;

                default:
                    #region Operação inválida+timer
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Invalid operation code!\nVerify the code and try again.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        BackMenu();
                    }
                    #endregion
                    break;
            }
        }
        static ProgressBar progress;
        static void BackMenu()
        {
            Console.Clear();
            Main(new string[0]);
        }
        static void Atualizar()
        {
            Console.WriteLine("Operation concluded!\n" +
                "Want to go back to main menu?\n" +
                "\n(Y)Yes/(N)No: ");
            switch (Console.ReadLine().ToLower())
            {
                case "y":
                    BackMenu();
                    break;
                case "n":
                    Environment.Exit(0);
                    break;
                default:
                    Console.Clear();
                    var time = TimeSpan.FromSeconds((double)10);
                    long tick = time.Ticks;
                    Console.WriteLine("Invalid operation code!\nVerify the code and try again.");
                    while (tick > 0)
                    {
                        tick--;
                    }
                    if (tick == 0)
                    {
                        Atualizar();
                    }
                    break;
            }
        }
        static void ExtractDBL()
        {
            Console.Clear();
            Console.WriteLine("Open one or multiple DBL containers extracted from QuickBms script(not DBLMERGE)...");
            var op = new OpenFileDialog();
            op.Title = "Open one or multiple DBL containers extracted from QuickBms script(not DBLMERGE)...";
            op.Multiselect = true;
            if (op.ShowDialog() == DialogResult.OK)
            {
                var folder = new FolderBrowserDialog();
                folder.Description = "Select the folder to extract.(It will create a folder named 'ExtractedDBL')";
                if (folder.ShowDialog() == DialogResult.OK)
                {
                    string pathX = folder.SelectedPath + @"\ExtractedDBL";
                    if (!Directory.Exists(pathX))
                        Directory.CreateDirectory(pathX);
                    pathX += @"\";

                    Console.Clear();
                    Console.WriteLine("Full Progress: ");
                    progress = new ProgressBar();
                    int pro = 0;
                    foreach (var file in op.FileNames)
                    {
                        string pathdb = pathX + Path.GetFileNameWithoutExtension(file);
                        
                        try
                        {
                            DBL dBL = new DBL(File.ReadAllBytes(file));

                            

                            if (!Directory.Exists(pathdb))
                                Directory.CreateDirectory(pathdb);

                            pathdb += @"\";

                            File.WriteAllBytes(pathX + Path.GetFileName(file), dBL.DBLB);

                            dBL.ExtractContainer(pathdb);
                        }
                        catch (Exception) { }
                        #region ProgressBar
                        progress.Report((double)pro / op.FileNames.Length);
                        Thread.Sleep(20);
                        #endregion
                        pro++;
                    }
                    progress.Dispose();
                    Console.Clear();
                    Console.WriteLine("Extracted sucessfully!\n" +
                            "See path: " + pathX);
                    Atualizar();
                }
                else
                    BackMenu();
            }
            else
                BackMenu();
        }
        static void RepackDBL()
        {
            Console.Clear();
            Console.WriteLine("Open the containing DBL containers folder to repack them...");
            var folder = new FolderBrowserDialog();
            folder.Description = "Open the containing DBL containers folder to repack them...";
            if (folder.ShowDialog() == DialogResult.OK)
            {
                string openpack = folder.SelectedPath;
                folder.Description = "Select a folder to save the repacked DBLs.(It will create a folder named 'Repacked')";
                if (folder.ShowDialog() == DialogResult.OK)
                {
                    string savepath = folder.SelectedPath + @"\Repacked";
                    if (!Directory.Exists(savepath))
                        Directory.CreateDirectory(savepath);

                    progress = new ProgressBar();
                    Console.Clear();
                    Console.WriteLine("Full Progress: ");

                    savepath += @"\";
                    DBL.RepackContainer(openpack, savepath, progress);

                    progress.Dispose();
                    Console.Clear();
                    Console.WriteLine("Repacked sucessfully!\n" +
                                                "See path: " + savepath);
                    Atualizar();
                }
                else
                    BackMenu();
            }
            else
                BackMenu();
        }
    }
}
