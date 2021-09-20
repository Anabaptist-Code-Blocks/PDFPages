using GongSolutions.Wpf.DragDrop;
using PdfSharp;
using PdfSharp.Pdf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PDFPages.ViewModels
{
    public class MainVM : ViewModelBase, IDropTarget, IDragSource
    {
        public MainVM()
        {
            Console.WriteLine("Main Init");
            LoadFiles();
        }

        #region Fields
        private bool fileSortDesc = false;
        private bool outputFileSortDesc = false;
        DefaultDropHandler dropHandler = new DefaultDropHandler();
        DefaultDragHandler dragHandler = new DefaultDragHandler();
        private int _SplitNPages = 1;
        #endregion

        #region Properties

        public ObservableCollection<PDFFile> Files { get; set; } = new ObservableCollection<PDFFile>();
        public PDFFile SelectedFile { get; set; }
        public ObservableCollection<PDFFile> OutputFiles { get; set; } = new ObservableCollection<PDFFile>();
        public PDFFile SelectedOutputFile { get; set; }
        public string OutputPath { get; set; } = "";
        public int RowHeight { get; set; } = 150;
        public double PreviewWidth { get; set; } = 500;
        public double PreviewHeight { get; set; } = 800;
        public ImageSource PreviewImage { get; set; }
        public bool PreviewOpen { get; set; } = false;
        public int PreviewRotation { get; set; } = 0;
        public int SplitNPages
        {
            get => _SplitNPages;
            set
            {
                _SplitNPages = value;
                SplitPages(_SplitNPages);
            }
        }

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
        private ICommand _PreviewCommand;
        public ICommand PreviewCommand
        {
            get
            {
                return _PreviewCommand ?? (_PreviewCommand = new CommandHandler(e => { ShowPreview(e as PageInfo); }, true));
            }
        }
        private ICommand _AddFileCommand;
        public ICommand AddFileCommand
        {
            get
            {
                return _AddFileCommand ?? (_AddFileCommand = new CommandHandler(e => { AddFile(); }, true));
            }
        }
        private ICommand _DeleteItemsCommand;
        public ICommand DeleteItemsCommand
        {
            get
            {
                return _DeleteItemsCommand ?? (_DeleteItemsCommand = new CommandHandler(e => { DeleteItems(e); }, true));
            }
        }
        private ICommand _RotateCommand;
        public ICommand RotateCommand
        {
            get
            {
                return _RotateCommand ?? (_RotateCommand = new CommandHandler(e => { Rotate(e as PageInfo); }, true));
            }
        }

        #endregion

        #region Methods

        private void AddFile()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            dialog.DefaultExt = "*.pdf";
            dialog.Filter = "pdf files(*.pdf)|*.pdf|All files(*.*)|*.*";
            dialog.Multiselect = true;
            if (dialog.ShowDialog(App.Current.MainWindow).GetValueOrDefault())
            {
                foreach (var path in dialog.FileNames)
                {
                    try
                    {
                        Files.Add(new PDFFile(path));
                    }
                    catch
                    {
                        //Don't stress about invalid files.
                    }
                }
            }
        }

        private void DeleteItems(object e)
        {
            var dataGrid = e as DataGrid;
            if (dataGrid?.SelectedItems?.Count > 0)
            {
                switch (GetGridType(dataGrid.ItemsSource))
                {
                    case GridType.InputFiles:
                        foreach (var item in dataGrid.SelectedItems.Cast<PDFFile>().ToList()) Files.Remove(item as PDFFile);
                        break;
                    case GridType.InputPages:
                        foreach (var item in dataGrid.SelectedItems.Cast<PageInfo>().ToList()) SelectedFile?.Pages?.Remove(item as PageInfo);
                        break;
                    case GridType.OutputFiles:
                        foreach (var item in dataGrid.SelectedItems.Cast<PDFFile>().ToList()) OutputFiles.Remove(item as PDFFile);
                        break;
                    case GridType.OutputPages:
                        foreach (var item in dataGrid.SelectedItems.Cast<PageInfo>().ToList()) SelectedOutputFile?.Pages?.Remove(item as PageInfo);
                        break;
                }
            }
        }

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

        private void Rotate(PageInfo page)
        {
            if (page != null)
            {
                page.Page.Orientation = page.Page.Orientation == PageOrientation.Landscape ? PageOrientation.Portrait : PageOrientation.Landscape;
                page.Page.Rotate = page.Page.Rotate >= 270 ? 0 : page.Page.Rotate + 90;
                page.Rotation = page.Page.Rotate;
            }
            var i = page?.PageImage;
        }

        public void SortFiles()
        {
            fileSortDesc = !fileSortDesc;
            List<PDFFile> list = (fileSortDesc ? Files.OrderBy(f => f.FileName) : Files.OrderByDescending(f => f.FileName)).ToList();
            Files = new ObservableCollection<PDFFile>(list);
        }

        public void SortOutputFiles()
        {
            outputFileSortDesc = !outputFileSortDesc;
            List<PDFFile> list = (outputFileSortDesc ? OutputFiles.OrderBy(f => f.FileName) : OutputFiles.OrderByDescending(f => f.FileName)).ToList();
            OutputFiles = new ObservableCollection<PDFFile>(list);
        }

        private void SplitPages(int n = 1)
        {
            try
            {
                OutputFiles.Clear();
                foreach (var file in Files)
                {
                    int cnt = 1;
                    int cntN = 1;
                    foreach (var page in file.Pages)
                    {
                        if (cntN == 1)
                        {
                            OutputFiles.Add(new PDFFile(file.FileName.Replace(".pdf", $" ({cnt}).pdf"), new List<PageInfo>() { page }));
                            cnt++;
                        }
                        else
                        {
                            OutputFiles.LastOrDefault().Pages.Add(page);
                        }
                        cntN++;
                        if (cntN > n) cntN = 1;
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

        private void ShowPreview(PageInfo page)
        {
            if (page != null)
            {
                PreviewRotation = page.Rotation;
                var sideways = PreviewRotation == 90 || PreviewRotation == 270;
                PreviewHeight = sideways ? page.Page.Width.Point : page.Page.Height.Point;
                PreviewWidth = sideways ? page.Page.Height.Point : page.Page.Width.Point;
                PreviewImage = page.PageImage;
                PreviewOpen = true;
            }
        }

        #endregion

        #region Drag & Drop

        enum GridType
        {
            InputFiles,
            InputPages,
            OutputFiles,
            OutputPages,
            None
        }

        private GridType GetGridType(IEnumerable collection)
        {
            if (collection == Files) return GridType.InputFiles;
            if (collection == SelectedFile?.Pages) return GridType.InputPages;
            if (collection == OutputFiles) return GridType.OutputFiles;
            if (collection == SelectedOutputFile?.Pages) return GridType.OutputPages;
            return GridType.None;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            //throw new NotImplementedException();
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
            GridType DragGrid = GetGridType(dropInfo?.DragInfo?.SourceCollection);
            GridType DropGrid = GetGridType(dropInfo?.TargetCollection);

            switch (DropGrid)
            {
                case GridType.InputFiles:
                    if (DragGrid != GridType.InputFiles)
                    {
                        var data = dropInfo.Data as IDataObject;
                        if (data != null && (data.GetDataPresent("FileContents") || data.GetDataPresent(DataFormats.FileDrop)))
                        {
                            dropInfo.Effects = DragDropEffects.Copy;
                            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                        }
                        else dropInfo.Effects = DragDropEffects.None;
                    }
                    return;
                case GridType.InputPages:
                    if (DragGrid != GridType.InputPages)
                    {
                        dropInfo.DropTargetAdorner = null;
                        dropInfo.Effects = DragDropEffects.None;
                    }
                    return;
                case GridType.OutputFiles:
                    if (DragGrid != GridType.OutputFiles)
                    {
                        dropInfo.Effects = DragDropEffects.Copy;
                    }
                    if ((int)dropInfo.InsertPosition >= 4)
                    {
                        dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    }
                    return;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            try
            {
                GridType DragGrid = GetGridType(dropInfo?.DragInfo?.SourceCollection);
                GridType DropGrid = GetGridType(dropInfo?.TargetCollection);
                switch (DropGrid)
                {
                    case GridType.InputFiles:
                        var data = dropInfo.Data as IDataObject;
                        if (data != null && (data.GetDataPresent("FileContents") || data.GetDataPresent(DataFormats.FileDrop)))
                        {
                            var files = ExtractFiles(data);
                            for (int i = files.Count - 1; i >= 0; i--)
                            {
                                Files.Insert(dropInfo.InsertIndex, files[i]);
                            }
                            return;
                        }
                        break;
                    case GridType.OutputFiles:
                        if (DragGrid != DropGrid)
                        {
                            PDFFile file = null;
                            if (dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                            {
                                file = dropInfo.TargetItem as PDFFile;
                            }
                            else
                            {
                                file = new PDFFile("new file.pdf", new List<PageInfo>());
                                OutputFiles.Insert(dropInfo.UnfilteredInsertIndex, file);
                            }
                            switch (DragGrid)
                            {
                                case GridType.InputFiles:
                                    if (dropInfo.Data.GetType() == typeof(PDFFile))
                                    {
                                        foreach (var page in (dropInfo.Data as PDFFile).Pages)
                                        {
                                            file.Pages.Add(page);
                                        }
                                    }
                                    else
                                    {
                                        var inputFiles = (dropInfo.Data as ICollection)?.Cast<PDFFile>()?.ToList();
                                        if (inputFiles != null)
                                        {
                                            foreach (var inputFile in inputFiles)
                                            {
                                                foreach (PageInfo page in inputFile.Pages)
                                                {
                                                    file.Pages.Add(page);
                                                }
                                            }
                                        }
                                    }
                                    return;
                                case GridType.InputPages:
                                case GridType.OutputPages:
                                    var movedPages = new List<PageInfo>();
                                    if (dropInfo.Data.GetType() == typeof(PageInfo))
                                    {
                                        file.Pages.Add(dropInfo.Data as PageInfo);
                                        movedPages.Add(dropInfo.Data as PageInfo);
                                    }
                                    else
                                    {
                                        var pages = (dropInfo.Data as ICollection)?.Cast<PageInfo>()?.ToList();
                                        if (pages != null)
                                        {
                                            foreach (PageInfo page in pages)
                                            {
                                                file.Pages.Add(page);
                                                movedPages.Add(page);
                                            }
                                        }
                                    }
                                    if (DragGrid == GridType.OutputPages && SelectedOutputFile != null)
                                    {
                                        foreach (var page in movedPages)
                                        {
                                            SelectedOutputFile.Pages.Remove(page);
                                        }
                                    }
                                    return;
                            }
                        }
                        else
                        {
                            //handle dropping file(s) from outputfiles onto another file. This adds the dropped files to the target.
                            if (dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight)
                            {
                                var file = dropInfo.TargetItem as PDFFile;
                                if (dropInfo.Data.GetType() == typeof(PDFFile))
                                {
                                    foreach (var page in (dropInfo.Data as PDFFile).Pages)
                                    {
                                        file.Pages.Add(page);
                                    }
                                    OutputFiles.Remove(dropInfo.Data as PDFFile);
                                }
                                else
                                {
                                    var inputFiles = (dropInfo.Data as ICollection)?.Cast<PDFFile>()?.ToList();
                                    if (inputFiles != null)
                                    {
                                        foreach (var inputFile in inputFiles)
                                        {
                                            foreach (PageInfo page in inputFile.Pages)
                                            {
                                                file.Pages.Add(page);
                                            }
                                            OutputFiles.Remove(inputFile);
                                        }
                                    }
                                }
                                return;
                            }

                        }
                        break;
                    case GridType.OutputPages:
                        if (DragGrid != DropGrid)
                        {
                            var file = SelectedOutputFile;
                            if (file == null) return;
                            switch (DragGrid)
                            {
                                case GridType.InputFiles:
                                    if (dropInfo.Data.GetType() == typeof(PDFFile))
                                    {
                                        var pages = (dropInfo.Data as PDFFile).Pages;
                                        for (int i = pages.Count - 1; i >= 0; i--)
                                        {
                                            file.Pages.Insert(dropInfo.InsertIndex, pages[i]);
                                        }
                                    }
                                    else
                                    {
                                        var inputFiles = (dropInfo.Data as ICollection)?.Cast<PDFFile>()?.ToList();
                                        if (inputFiles != null)
                                        {
                                            var pages = new List<PageInfo>();
                                            foreach (var inputFile in inputFiles)
                                            {
                                                foreach (PageInfo page in inputFile.Pages)
                                                {
                                                    pages.Add(page);
                                                }
                                            }
                                            for (int i = pages.Count - 1; i >= 0; i--)
                                            {
                                                file.Pages.Insert(dropInfo.InsertIndex, pages[i]);
                                            }
                                        }
                                    }
                                    return;
                                case GridType.InputPages:
                                    if (dropInfo.Data.GetType() == typeof(PageInfo))
                                    {
                                        file.Pages.Insert(dropInfo.InsertIndex, dropInfo.Data as PageInfo);
                                    }
                                    else
                                    {
                                        var pages = (dropInfo.Data as ICollection)?.Cast<PageInfo>()?.ToList();
                                        if (pages != null)
                                        {
                                            for (int i = pages.Count - 1; i >= 0; i--)
                                            {
                                                file.Pages.Insert(dropInfo.InsertIndex, pages[i]);
                                            }
                                        }
                                    }
                                    return;
                                case GridType.OutputFiles:
                                    return;
                            }
                        }

                        break;
                }

                dropHandler.Drop(dropInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //So there was an error dropping the object. Don't stress about it.
            }
        }

        public void StartDrag(IDragInfo dragInfo)
        {
            dragHandler.StartDrag(dragInfo);
        }

        public bool CanStartDrag(IDragInfo dragInfo)
        {
            return dragHandler.CanStartDrag(dragInfo);
        }

        public void Dropped(IDropInfo dropInfo)
        {
        }

        public void DragDropOperationFinished(DragDropEffects operationResult, IDragInfo dragInfo)
        {
            dragHandler.DragDropOperationFinished(operationResult, dragInfo);
        }

        public void DragCancelled()
        {
            dragHandler.DragCancelled();
        }

        public bool TryCatchOccurredException(Exception exception)
        {
            return dragHandler.TryCatchOccurredException(exception);
        }

        public List<PDFFile> ExtractFiles(IDataObject data)
        {
            var files = new List<PDFFile>();
            try
            {
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] filesNames = data.GetData(DataFormats.FileDrop) as string[];
                    if (filesNames != null && filesNames.Length > 0)
                    {
                        foreach (var filePath in filesNames)
                        {
                            try
                            {
                                var file = new PDFFile(filePath);
                                files.Add(file);
                            }
                            catch
                            {
                                //in case the file is not a valid pdf it simply isn't added.
                            }
                        }
                    }
                }
                else if (data.GetDataPresent("FileContents"))
                {
                    //handle file dropped from outlook
                    //get the file names
                    List<string> fileNames = new List<string>();
                    using (var stream = data.GetData("FileGroupDescriptor") as MemoryStream)
                    {
                        var fileGroupDescriptor = new byte[stream.Length];
                        stream.Read(fileGroupDescriptor, 0, (int)stream.Length);
                        int numFiles = fileGroupDescriptor[0];
                        // used to build the filename from the FileGroupDescriptor block
                        // this trick gets the filename of the passed attached file
                        var pos = 0;
                        for (int fileNum = 0; fileNum < numFiles; fileNum++)
                        {
                            var fn = new StringBuilder("");
                            var startPos = (fileNum * 332) + 76;
                            for (int i = startPos; fileGroupDescriptor[i] != 0; i++)
                            {
                                fn.Append(Convert.ToChar(fileGroupDescriptor[i]));
                                pos = i;
                            }
                            fileNames.Add(fn.ToString());
                        }
                        stream.Close();
                    }
                    //get multiple files contents
                    var comDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)data;
                    OutlookDataObject dataObject = new OutlookDataObject(new System.Windows.Forms.DataObject(data));
                    MemoryStream[] fileStreams = (MemoryStream[])dataObject.GetData("FileContents");
                    for (int i = 0; i < fileStreams.Length; i++)
                    {
                        using (var ms = fileStreams[i])
                        {
                            //var contents = new byte[ms.Length];
                            //ms.Read(contents, 0, (int)ms.Length);
                            //ms.Close();
                            try
                            {
                                var doc = PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                                var pages = new List<PageInfo>(0);
                                foreach (var page in doc.Pages) pages.Add(new PageInfo(page));
                                var file = new PDFFile(fileNames[i], pages, ms);
                                files.Add(file);
                                ms.Close();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                //in case the file is not a valid pdf it simply isn't added.
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return files;
        }

        #endregion

    }
}
