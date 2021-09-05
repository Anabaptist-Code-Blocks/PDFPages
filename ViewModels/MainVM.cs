using GongSolutions.Wpf.DragDrop.Utilities;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PDFPages.ViewModels
{
    public class MainVM : ViewModelBase
    {
        public MainVM()
        {
            Console.WriteLine("Main Init");
            LoadFiles();
        }

        #region Fields
        private bool fileSortDesc = false;
        #endregion

        #region Properties

        public ObservableCollection<PDFFile> Files { get; set; } = new ObservableCollection<PDFFile>();
        public PDFFile SelectedFile { get; set; }
        public ObservableCollection<PDFFile> OutputFiles { get; set; } = new ObservableCollection<PDFFile>();
        public PDFFile SelectedOutputFile { get; set; }
        public string OutputPath { get; set; } = "";
        public int RowHeight { get; set; } = 150;

        #endregion

        #region Commands
        private ICommand _SplitCommand;
        public ICommand SplitCommand
        {
            get
            {
                return _SplitCommand ?? (_SplitCommand = new CommandHandler(e => { SplitPages(); }, true));
            }
        }
        private ICommand _MergeCommand;
        public ICommand MergeCommand
        {
            get
            {
                return _MergeCommand ?? (_MergeCommand = new CommandHandler(e => { MergePages(); }, true));
            }
        }
        private ICommand _SelectFolderCommand;
        public ICommand SelectFolderCommand
        {
            get
            {
                return _SelectFolderCommand ?? (_SelectFolderCommand = new CommandHandler(e => { SelectFolder(); }, true));
            }
        }
        private ICommand _SaveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return _SaveCommand ?? (_SaveCommand = new CommandHandler(e => { SaveOutputFiles(); }, true));
            }
        }


        #endregion

        #region Methods

        private void LoadFiles()
        {
            var args = Environment.GetCommandLineArgs().Where(arg => arg.ToLower().EndsWith(".pdf"));
            foreach (var arg in args)
            {
                if (File.Exists(arg)) Files.Add(new PDFFile(arg));
            }
            if (Files.Count > 0)
            {
                OutputPath = Path.GetDirectoryName(Files[0].FullPath);
            }
            if (Files.Count == 1) SplitPages();
            if (Files.Count > 1) MergePages();
        }

        public void SortFiles()
        {
            fileSortDesc = !fileSortDesc;
            List<PDFFile> list = (fileSortDesc ? Files.OrderBy(f => f.FileName) : Files.OrderByDescending(f => f.FileName)).ToList();
            Files = new ObservableCollection<PDFFile>(list);
        }

        private void SplitPages()
        {
            try
            {
                OutputFiles.Clear();
                foreach (var file in Files)
                {
                    int cnt = 1;
                    foreach (var page in file.Pages)
                    {
                        OutputFiles.Add(new PDFFile(file.FileName.Replace(".pdf", $" ({cnt}).pdf"), new List<PageInfo>() { page }));
                        cnt++;
                    }
                }
            }
            catch (Exception e)
            {
                ShowError(nameof(SplitCommand), e.Message, 4000);
            }

        }

        private void MergePages()
        {
            try
            {
                OutputFiles.Clear();
                var pages = new List<PageInfo>();
                foreach (var file in Files)
                {
                    foreach (var page in file.Pages) pages.Add(page);
                }
                OutputFiles.Add(new PDFFile("Merged.pdf", pages));
            }
            catch (Exception e)
            {
                ShowError(nameof(MergeCommand), e.Message, 4000);
            }
        }

        private void SelectFolder()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(App.Current.MainWindow).GetValueOrDefault())
            {
                OutputPath = dialog.SelectedPath;
            }
        }

        private void SaveOutputFiles()
        {
            try
            {
                foreach (var file in OutputFiles)
                {
                    var doc = new PdfDocument(Path.Combine(OutputPath, file.FileName));
                    foreach (var page in file.Pages) doc.AddPage(page.Page);
                    doc.Close();
                }
            }
            catch (Exception e)
            {
                ShowError(nameof(SaveCommand), e.Message, 4000);
            }
        }

        #endregion
    }
}
