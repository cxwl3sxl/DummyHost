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
        private readonly Dictionary<string, string> _fileMap = new Dictionary<string, string>();
        private readonly string _responseDir;
        private readonly RollConsole _rollConsole = new RollConsole(5);

        public DummyResponseHandler(WebMiddleware next) : base(next)
        {
            _responseDir = Path.Combine(Directory.GetCurrentDirectory(), "Response");
            ReloadMap(false);
            WatcherManager.Watch[_responseDir].IncludeSubdirectories().UseHandler(this);
        }

        public override async Task Invoke(HttpContext context)
        {
            var url = context.Request.File.Split('?')[0];
            _rollConsole.WriteLine($"{context.Request.Method}: {url}");
            if (TryGetResponse(url.Replace("/", "~"), out var header, out var content))
            {
                var contentType = "text/html; charset=utf-8";
                if (header != null)
                {
                    foreach (var kv in header)
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
                }

                if (string.IsNullOrWhiteSpace(contentType)) contentType = "text/html; charset=utf-8";

                await context.Response.SetContentType(contentType).Write(content).End();
            }
            else
            {
                await context.Response.SetHttpResponseStatus(HttpResponseStatus.NotFound).End();
            }
        }

        bool TryGetResponse(string uri, out Dictionary<string, string> header, out string content)
        {
            header = null;
            content = null;
            if (!_fileMap.ContainsKey(uri)) return false;
            var file = _fileMap[uri];
            if (!File.Exists(file)) return false;
            var lines = File.ReadAllLines(file);
            header = new Dictionary<string, string>();
            var body = new StringBuilder();
            var isBodyNow = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) isBodyNow = true;

                if (isBodyNow)
                {
                    body.AppendLine(line);
                }
                else
                {
                    var index = line.IndexOf(':');
                    if (index <= 0) continue;
                    header[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
                }
            }

            content = body.ToString();
            return true;
        }

        public void HandleEvent(WatcherEventType eventType, string sourceFile)
        {
            if (eventType == WatcherEventType.Changed) return;
            ReloadMap(true);
        }

        void ReloadMap(bool msg)
        {
            var files = Directory.GetFiles(_responseDir, "*.*", SearchOption.AllDirectories);
            _fileMap.Clear();
            if (msg) _rollConsole.WriteLine("正在重新载入映射关系，请稍后...", ConsoleColor.DarkGreen);
            foreach (var file in files)
            {
                var name = file.Replace(_responseDir, "").Replace('/', '~').Replace('\\', '~');
                if (_fileMap.ContainsKey(name))
                {
                    _rollConsole.WriteLine($"{file}和{_fileMap[name]}解析后同名，将被忽略！", ConsoleColor.DarkRed);
                    continue;
                }

                _fileMap[name] = file;
            }

            if (msg) _rollConsole.WriteLine($"载入完成，共计{_fileMap.Count}个", ConsoleColor.DarkGreen);
        }

        public event Action OnDispose;
    }
}