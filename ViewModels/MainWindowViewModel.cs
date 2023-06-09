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
        static string docFile = "Report\\Template.docx";
        string newDocFile = "";
        static MainWindowViewModel()
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };
            clientHandler.SslProtocols = SslProtocols.None;

            httpClient = new HttpClient(clientHandler)
            {
                BaseAddress = new Uri("https://192.168.250.250/"),
            };

            if (Directory.Exists("Report") == false)
            {
                Directory.CreateDirectory("Report");
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CreateReportCommand))]
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
            newDocFile = "Report\\" + cardNumber + ".docx";
        }


        [RelayCommand(CanExecute = nameof(CanCreateReport))]
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
            document.Replace("[一卡通号]", Graduates.userInfo.cardNumber, true,true);
            document.Replace("[姓名]", Graduates.userInfo.userName, true,true);
            document.Replace("[性别]", Graduates.userInfo.gender, true,true);
            document.Replace("[学院]", Graduates.userInfo.departMent, true,true);
            document.Replace("[到馆总次数]", Graduates.registrationCount.ToString(), true,true);
            document.Replace("[首次到馆时间]", Graduates.registrationEarliest?.ToString("yyyy-MM-dd HH:mm:ss"), true,true);
            document.Replace("[借书总数]", Graduates.borrowCount.ToString(), true,true);
            document.Replace("[首次借书时间]", Graduates.borrowEarliest?.borrowTime?.ToString("yyyy-MM-dd HH:mm:ss"), true,true);
            document.Replace("[首次借书书名]", Graduates.borrowEarliest?.bookName, true,true);
            document.Replace("[首次借书条码]", Graduates.borrowEarliest?.bookBarCode, true,true);
           
            try
            {
                document.SaveToFile(newDocFile, FileFormat.Docx);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            finally
            {
                document.Close();
            }


            try
            {
                Process.Start("explorer.exe", newDocFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Process.Start("explorer.exe", "Report");
            }
        }

        private bool CanCreateReport() => Graduates != null;
    }
}
