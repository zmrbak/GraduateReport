using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraduateReport.Models;
using Spire.Doc;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;

namespace GraduateReport.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        static HttpClient httpClient;
        static MainWindowViewModel()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
            clientHandler.SslProtocols = SslProtocols.None;

            httpClient = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri("https://192.168.250.250/"),
            };
        }

        static string docFile = "ReportTemplate.docx";
        static string newDocFile = "NewReport.docx";

        [ObservableProperty]
        private Graduates? graduates;

        [ObservableProperty]
        private string? labelData;

        [RelayCommand]
        private async Task GetGraduatesAsync(string? cardNumber)
        {
            if (cardNumber == null || cardNumber == "") return;
            LabelData = "";
            Graduates = null;

            using HttpResponseMessage response = await httpClient.GetAsync("/GdlisNet/Graduates?cardNumber=" + cardNumber);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (jsonResponse == "")
            {
                MessageBox.Show("该一卡通号未查到数据！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Graduates = JsonSerializer.Deserialize<Graduates>(jsonResponse);
            if (Graduates == null) return;

            LabelData = JsonSerializer.Serialize<Graduates>(Graduates, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true
            });
        }
        [RelayCommand]
        private void CreateReport()
        {
            if (Graduates == null || Graduates.userInfo == null) return;

            if (File.Exists(docFile) == false)
            {
                Assembly Asmb = Assembly.GetExecutingAssembly();
                string strName = Asmb.GetName().Name + ".report.docx";
                using Stream? ManifestStream = Asmb.GetManifestResourceStream(strName);

                byte[] StreamData = new byte[ManifestStream!.Length];
                ManifestStream.Read(StreamData, 0, (int)ManifestStream.Length);
                File.WriteAllBytes(docFile, StreamData);
            }

            Document document = new Document(docFile);

            foreach (TableRow tableRow in document.Sections[0].Tables[0].Rows)
            {
                switch (tableRow.Cells[0].Paragraphs[0].Text)
                {
                    case "一卡通号：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.userInfo.cardNumber;
                        break;
                    case "姓名：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.userInfo.userName;
                        break;
                    case "性别：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.userInfo.gender;
                        break;
                    case "学院：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.userInfo.departMent;
                        break;
                    case "到馆总次数：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.registrationCount.ToString();
                        break;
                    case "首次到馆时间：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.registrationEarliest?.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case "借书总数：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.borrowCount.ToString();
                        break;
                    case "首次借书时间：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.borrowEarliest?.borrowTime?.ToString("yyyy-MM-dd HH:mm:ss");
                        break;
                    case "首次借书书名：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.borrowEarliest?.bookName;
                        break;
                    case "首次借书条码：":
                        tableRow.Cells[1].Paragraphs[0].Text = Graduates.borrowEarliest?.bookBarCode;
                        break;
                    default:
                        break;
                }
            }
            document.SaveToFile(newDocFile, FileFormat.Docx);
            document.Close();

            Process.Start("explorer.exe", newDocFile);
        }
    }
}
