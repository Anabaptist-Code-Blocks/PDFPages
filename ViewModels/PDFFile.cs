using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var stream = new MemoryStream(bytes);
            var pdfDocument = PdfiumViewer.PdfDocument.Load(stream);
            for (int i = 0; i < Pages.Count; i++)
            {
                var page = Pages[i].Page;
                var height = Convert.ToInt32(page.Height.Point);
                int yDpi = Convert.ToInt32(page.Height.Point / page.Height.Inch);
                int width = Convert.ToInt32(page.Width.Point);
                var xDpi = Convert.ToInt32(page.Width.Point / page.Width.Inch);
                var image = pdfDocument.Render(i, (int)width, (int)height, xDpi, yDpi, PdfiumViewer.PdfRenderFlags.Annotations);
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = ms;
                        bitmapImage.EndInit();
                        Pages[i].PageImage = bitmapImage;
                        //PageImages.Add(bitmapImage);
                    });
                }
            }
            Console.WriteLine("Got Images: " + FileName);
            return true;
        }

        public string FullPath { get; set; }
        public string FileName { get; set; }
        public ObservableCollection<PageInfo> Pages { get; set; } = new ObservableCollection<PageInfo>();
        public PdfDocument Document { get; set; }
    }

    public class PageInfo
    {
        public PageInfo(PdfPage page)
        {
            Page = page;
        }
        public PdfPage Page { get; set; }
        public ImageSource PageImage { get; set; }
    }
}
