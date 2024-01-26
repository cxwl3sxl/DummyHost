using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DotNetty.Codecs.Http;
using PinFun.Core.Net.Http;
using PinFun.Core.Utils.FileWatcher;

namespace DummyHost
{
    public class DummyResponseHandler : WebMiddleware, IWatcherEventHandler
    {
        private readonly Dictionary<string, ResponseConfig> _fileMap = new();
        private readonly string _responseDir;

        public DummyResponseHandler(WebMiddleware next) : base(next)
        {
            _responseDir = Path.Combine(Directory.GetCurrentDirectory(), "Response");
            ReloadMap(false);
            WatcherManager.Watch[_responseDir].IncludeSubdirectories().UseHandler(this);
        }

        public override async Task Invoke(HttpContext context)
        {
            try
            {
                var url = context.Request.File.Split('?')[0];
                RollConsole.Instance.WriteLine($"{context.Request.Method}: {url}");
                if (!_fileMap.ContainsKey(url.ToLower()))
                {
                    await context.Response.SetHttpResponseStatus(HttpResponseStatus.NotFound).End();
                    return;
                }

                var resp = _fileMap[url.ToLower()];

                var contentType = "text/html; charset=utf-8";
                foreach (var kv in resp.Header)
                {
                    if (string.Equals(kv.Key, "content-type", StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = kv.Value;
                    }
                    else
                    {
                        context.Response.SetHeader(kv.Key, kv.Value);
                    }
                }

                if (string.IsNullOrWhiteSpace(contentType)) contentType = "text/html; charset=utf-8";

                await context.Response.SetContentType(contentType).Write(resp.Content);
                await context.Response.End();
            }
            catch (Exception ex)
            {
                RollConsole.Instance.WriteLine($"错误:{ex.Message}", ConsoleColor.Red);
                try
                {
                    await context.Response.SetHttpResponseStatus(HttpResponseStatus.InternalServerError).End();
                }
                catch (Exception e2)
                {
                    RollConsole.Instance.WriteLine($"错误:{e2.Message}", ConsoleColor.Red);
                }
            }
        }

        public void HandleEvent(WatcherEventType eventType, string sourceFile)
        {
            ReloadMap(true);
        }

        void ReloadMap(bool msg)
        {
            var files = Directory.GetFiles(_responseDir, "*.txt", SearchOption.TopDirectoryOnly);

            _fileMap.Clear();
            if (msg) RollConsole.Instance.WriteLine("正在重新载入映射关系，请稍后...", ConsoleColor.DarkGreen);
            foreach (var file in files)
            {
                ParseFile(file, msg);
            }

            if (msg) RollConsole.Instance.WriteLine($"载入完成，共计{_fileMap.Count}个", ConsoleColor.DarkGreen);
        }

        void ParseFile(string file, bool msg)
        {
            var allLines = File.ReadAllLines(file);
            string url = null;
            var isHeader = true;
            var header = new Dictionary<string, string>();
            var body = new StringBuilder();
            for (var i = 0; i < allLines.Length; i++)
            {
                var line = allLines[i].Trim();
                if (i == 0)
                {
                    url = line.ToLower();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    isHeader = false;
                    continue;
                }

                if (isHeader)
                {
                    var index = line.IndexOf(':');
                    if (index <= 0) continue;
                    header[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
                }
                else
                {
                    body.AppendLine(line);
                }
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                if (msg) RollConsole.Instance.WriteLine($"{Path.GetFileName(file)}和无法读取拦截的请求地址！", ConsoleColor.DarkRed);
                return;
            }

            if (_fileMap.ContainsKey(url))
            {
                if (msg)
                    RollConsole.Instance.WriteLine($"{Path.GetFileName(file)}和{_fileMap[url].File}拦截地址相同，将被忽略！",
                        ConsoleColor.DarkRed);
                return;
            }

            _fileMap[url] = new ResponseConfig()
            {
                Content = body.ToString(),
                File = Path.GetFileName(file),
                Header = header
            };
        }

        public event Action OnDispose;
    }

    class ResponseConfig
    {
        public Dictionary<string, string> Header { get; set; }

        public string Content { get; set; }

        public string File { get; set; }
    }
}