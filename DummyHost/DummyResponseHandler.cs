using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var files = Directory.GetFiles(_responseDir, "*.txt", SearchOption.AllDirectories);

            _fileMap.Clear();
            if (msg) RollConsole.Instance.WriteLine("正在重新载入映射关系，请稍后...", ConsoleColor.DarkGreen);
            foreach (var file in files)
            {
                ParseFile(file.Replace(_responseDir, ""), msg, File.ReadAllLines(file));
            }

            if (msg) RollConsole.Instance.WriteLine($"载入完成，共计{_fileMap.Count}个", ConsoleColor.DarkGreen);
        }

        void ParseFile(string file, bool msg, string[] allSourceLines)
        {
            var firstNotEmptyStringIndex = 0;
            for (var i = 0; i < allSourceLines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(allSourceLines[i])) continue;
                firstNotEmptyStringIndex = i;
                break;
            }

            var allLines = allSourceLines.Skip(firstNotEmptyStringIndex).ToArray();

            var responseInfo = new[] { new List<string>(), new List<string>(), new List<string>() };
            var currentIndex = 0;
            foreach (var t in allLines)
            {
                var line = t.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    currentIndex++;
                    continue;
                }

                responseInfo[currentIndex].Add(line);
            }

            ParseResponse(file, responseInfo[0], responseInfo[1], responseInfo[2], msg);
        }

        void ParseResponse(string file, List<string> urlBuilder, List<string> headerBuilder, List<string> bodyBuilder,
            bool msg)
        {
            if (urlBuilder.Count == 0)
            {
                RollConsole.Instance.WriteLine($"文件{file}非法，无法读取请求地址");
                return;
            }

            var url = urlBuilder[0].ToLower();

            if (_fileMap.ContainsKey(url))
            {
                if (msg)
                    RollConsole.Instance.WriteLine($"{file}和{_fileMap[url].File}拦截地址相同，将被忽略！",
                        ConsoleColor.DarkRed);
                return;
            }

            var header = new Dictionary<string, string>();
            foreach (var headerItem in headerBuilder)
            {
                var index = headerItem.IndexOf(':');
                if (index <= 0) continue;
                header[headerItem.Substring(0, index).Trim()] = headerItem.Substring(index + 1).Trim();
            }

            _fileMap[url] = new ResponseConfig()
            {
                Content = string.Join(Environment.NewLine, bodyBuilder),
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