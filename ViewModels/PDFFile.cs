using GongSolutions.Wpf.DragDrop;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PDFPages.ViewModels
{
    public class PDFFile : ViewModelBase
    {
        public PDFFile(string fileName, List<PageInfo> pages)
        {
            FileName = fileName;
            Pages = new ObservableCollection<PageInfo>(pages);
        }
        public PDFFile(string fileName, List<PageInfo> pages, MemoryStream stream)
        {
            FileName = fileName;
            Pages = new ObservableCollection<PageInfo>(pages);
            GetImages(stream);
        }
        public PDFFile(string path)
        {
            if (File.Exists(path))
            {
                FullPath = path;
                FileName = Path.GetFileName(path);
                Document = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);

                foreach (var page in Document.Pages) Pages.Add(new PageInfo(page));
                Task.Delay(1).ContinueWith(_ => _ = GetImagesAsync());
            }
        }

        private async Task<bool> GetImagesAsync()
        {
            byte[] bytes;
            using (FileStream file = new FileStream(FullPath, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[file.Length];
                await file.ReadAsync(bytes, 0, (int)file.Length);
            }
            var doc = new PDFiumSharp.PdfDocument(bytes);
            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i].Page;
                var height = Convert.ToInt32(page.Height.Point);
                int yDpi = Convert.ToInt32(page.Height.Point / page.Height.Inch);
                int width = Convert.ToInt32(page.Width.Point);
                var xDpi = Convert.ToInt32(page.Width.Point / page.Width.Inch);
                var img = new PDFiumSharp.PDFiumBitmap(width, height, true);
                img.Fill(new PDFiumSharp.Types.FPDF_COLOR(255, 255, 255));
                doc.Pages[i].Render(img, PDFiumSharp.Enums.PageOrientations.Normal, PDFiumSharp.Enums.RenderingFlags.Annotations | PDFiumSharp.Enums.RenderingFlags.Printing);
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, xDpi, yDpi);
                    ms.Seek(0, SeekOrigin.Begin);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                        Pages[i].PageImage = bitmapImage;
                    });
                }
            }
            doc.Close();
            Console.WriteLine("Got Images: " + FileName);
            return true;
        }

        private void GetImages(MemoryStream stream)
        {
            var bytes = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);
            var doc = new PDFiumSharp.PdfDocument(bytes);
            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i].Page;
                var height = Convert.ToInt32(page.Height.Point);
                int yDpi = Convert.ToInt32(page.Height.Point / page.Height.Inch);
                int width = Convert.ToInt32(page.Width.Point);
                var xDpi = Convert.ToInt32(page.Width.Point / page.Width.Inch);

                var img = new PDFiumSharp.PDFiumBitmap(width, height, true);
                img.Fill(new PDFiumSharp.Types.FPDF_COLOR(255, 255, 255));
                doc.Pages[i].Render(img, PDFiumSharp.Enums.PageOrientations.Normal, PDFiumSharp.Enums.RenderingFlags.Annotations | PDFiumSharp.Enums.RenderingFlags.Printing);
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, xDpi, yDpi);
                    ms.Seek(0, SeekOrigin.Begin);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                        Pages[i].PageImage = bitmapImage;
                    });
                }
            }
            doc.Close();
            Console.WriteLine("Got Images: " + FileName);
            return;
        }

        public string FullPath { get; set; }
        public string FileName { get; set; }
        public ObservableCollection<PageInfo> Pages { get; set; } = new ObservableCollection<PageInfo>();
        public PdfDocument Document { get; set; }
    }

    public class PageInfo : ViewModelBase
    {
        public PageInfo(PdfPage page)
        {
            Page = page;
        }
        public PdfPage Page { get; set; }
        public ImageSource PageImage { get; set; }
        public int Rotation { get; set; } = 0;
    }
}
