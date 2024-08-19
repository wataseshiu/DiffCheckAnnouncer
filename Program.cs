using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Encodings;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //ブランチ名の取得
            string branchName = GetBranchName();
            ProcessStartInfo processStartInfo = new ProcessStartInfo("C:\\Program Files\\WinMerge\\WinMergeU.exe", "..\\..\\..\\..\\..\\..\\Staging ..\\..\\..\\..\\..\\..\\Release -noninteractive -minimize -r -cfg Settings/DirViewExpandSubdirs=1 -cfg Settings/ShowIdentical=0 -cfg ReportFiles/ReportType=0 -cfg ReportFiles/IncludeFileCmpReport=1 -or ..\\..\\..\\report.csv");
            Process process = Process.Start(processStartInfo);

            // プロセスが終了するまで待機
            process.WaitForExit();
            //　プロセスを終了させる
            process.Close();

            List<string> text = new();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // memo: Shift-JISを扱うためのおまじない
            using (var sr = new System.IO.StreamReader("..\\..\\..\\report.csv", System.Text.Encoding.GetEncoding("shift_jis")))
            {
                while (sr.EndOfStream == false)
                {
                    text.Add(sr.ReadLine());
                }
            }
            text.RemoveRange(0, 3);

            List<WinMergeData> datas = new();
            foreach (string txt in text)
            {
                if (txt.Split(",").ElementAt(1) != "")
                {
                    datas.Add(new(txt.Split(",").ElementAt(0),
                     txt.Split(",").ElementAt(1),
                     txt.Split(",").ElementAt(2),
                     txt.Split(",").ElementAt(3),
                     txt.Split(",").ElementAt(4),
                     txt.Split(",").ElementAt(5)));
                }
            }

            //差分ありcsvファイルのレポート行だけを抽出した
            var diffDatas = datas.Where(d => (d.LeftTimeStamp != "  ") && (d.RightTimeStamp != "  ")).Where(d => d.FileExtention == "csv").ToList();

            string diffText = "";
            foreach (var d in diffDatas)
                diffText += d.FileName + "\n";

            //差分ありcsvファイルのレポート行だけを抽出した
            var stagingOnlyDatas = datas.Where(d => (d.LeftTimeStamp != "  ") && (d.RightTimeStamp == "  ")).Where(d => d.FileExtention == "csv").ToList();
            string stagingOnlyText = "";
            foreach (var s in stagingOnlyDatas)
                stagingOnlyText += s.FileName + "\n";

            //差分ありcsvファイルのレポート行だけを抽出した
            var releaseOnlyDatas = datas.Where(d => (d.LeftTimeStamp == "  ") && (d.RightTimeStamp != "  ")).Where(d => d.FileExtention == "csv").ToList();
            string releaseOnlyText = "";
            foreach (var s in releaseOnlyDatas)
                releaseOnlyText += s.FileName + "\n";

            // Webhook URL
            var webhookUrl = "URL";

            var payload = new Payload
            {
                Text = ":kaomoji:" + branchName + "ブランチの差分チェック結果\n" + ":kaomoji:StagingとReleaseの差分があるCSVファイル一覧\n" + diffText + "\n\n"
            + ":kaomoji:Stagingにだけ存在するCSVファイル一覧\n" + stagingOnlyText + "\n\n"
            + ":kaomoji:Releaseにだけ存在するCSVファイル一覧\n" + releaseOnlyText
            };
            var json = JsonSerializer.Serialize(payload);

            var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = client.PostAsync(webhookUrl, content).Result;
            Console.WriteLine(result);
        }

        private static string GetBranchName()
        {
            var gitDirPath = string.Empty;
            gitDirPath = @"..\..\..\..\..\..\..\.git";

            if (string.IsNullOrEmpty(gitDirPath))
                return string.Empty;

            var headPath = gitDirPath + "/HEAD";
            var refs = string.Empty;
            using (var reader = new System.IO.StreamReader(headPath))
                refs = reader.ReadLine();

            // ref: refs/heads/ で始まっていない場合はハッシュチェックアウト
            if (!refs.StartsWith("ref: refs/heads/"))
                return string.Empty;

            var branch = refs.Substring("ref: refs/heads/".Length);
            return branch;
        }
    }
    public class WinMergeData
    {
        public string FileName { get; set; }
        public string FolderName { get; set; }
        public string Compare { get; set; }
        public string LeftTimeStamp { get; set; }
        public string RightTimeStamp { get; set; }
        public string FileExtention { get; set; }

        public WinMergeData(string fileName, string folderName, string compare, string leftTimeStamp, string rightTimeStamp, string fileExtention)
        {
            FileName = fileName;
            FolderName = folderName;
            Compare = compare;
            LeftTimeStamp = leftTimeStamp;
            RightTimeStamp = rightTimeStamp;
            FileExtention = fileExtention;
        }
    }
    public class Payload
    {
        [JsonPropertyName("text")] public string Text { get; set; }
        [JsonPropertyName("blocks")] public string Blocks { get; set; }
    }
}