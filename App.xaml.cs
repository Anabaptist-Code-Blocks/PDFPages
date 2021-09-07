using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PDFPages
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string[] args = Environment.GetCommandLineArgs();
            //first arg is executable path
            if (args.Length > 1)
            {
                var action = args[1].ToLower();
                switch (action)
                {
                    case "merge":
                        Merge(args);
                        App.Current.Shutdown();
                        return;
                    case "split":
                        Split(args);
                        App.Current.Shutdown();
                        return;
                }
            }
        }

        private void Split(string[] args)
        {
            for (int i = 2; i < args.Length; i++)
            {
                if (File.Exists(args[i]))
                {
                    var filePath = Path.GetFullPath(args[i]);
                    var doc = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                    int cnt = 1;
                    foreach (var page in doc.Pages)
                    {
                        var doc1 = new PdfSharp.Pdf.PdfDocument();
                        doc1.AddPage(page);
                        var newFile = filePath.Replace(".pdf", $" ({cnt}).pdf");
                        doc1.Save(newFile);
                        cnt++;
                    }
                    doc.Close();
                }
            }
        }

        private void Merge(string[] args)
        {
            var outputPath = "";
            var doc1 = new PdfSharp.Pdf.PdfDocument();
            for (int i = 2; i < args.Length; i++)
            {
                if (File.Exists(args[i]))
                {
                    var filePath = Path.GetFullPath(args[i]);
                    if (outputPath == "") outputPath = filePath.Replace(".pdf", $" (Merged).pdf");
                    var doc = PdfSharp.Pdf.IO.PdfReader.Open(filePath, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
                    foreach (var page in doc.Pages)
                    {
                        doc1.AddPage(page);
                    }
                    doc.Close();
                }
            }
            if (outputPath != "") doc1.Save(outputPath);
        }
    }
}
