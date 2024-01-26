using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DummyHost
{
    public class RollConsole : IDisposable
    {
        private readonly List<Item> _items = new List<Item>();
        private readonly BufferBlock<Item> _pendingBlock = new BufferBlock<Item>();
        private int _top = -1;
        private readonly int _count;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public static RollConsole Instance { get; }

        static RollConsole()
        {
            Instance = new RollConsole(5);
        }

        private RollConsole(int count)
        {
            _count = count;
            Start();
        }

        public void WriteLine(string content, ConsoleColor color = ConsoleColor.Gray)
        {
            _pendingBlock.Post(new Item($"[{DateTime.Now:HH:mm:ss}] {content}", color));
        }

        async void Start()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await DoWrite();
            }
        }

        async Task DoWrite()
        {

            var preItem = await _pendingBlock.ReceiveAsync(_cancellationTokenSource.Token);

            if (_top == -1) _top = Console.CursorTop;
            while (_items.Count >= _count)
            {
                _items.RemoveAt(0);
            }

            _items.Add(preItem);

            for (var i = 0; i < _count; i++)
            {
                Console.CursorTop = _top + i;
                Console.CursorLeft = 0;
                if (i < _items.Count)
                {
                    Console.ForegroundColor = _items[i].Color;
                    Console.Write(_items[i].Content.PadLeft(Console.BufferWidth, ' '));
                }
                else
                {
                    Console.Write("".PadLeft(Console.BufferWidth, ' '));
                }
            }

            Console.CursorLeft = 0;
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }

    class Item
    {
        public Item(string content, ConsoleColor color)
        {
            Content = content.PadRight(Console.BufferWidth, ' ').Substring(0, Console.BufferWidth);
            Color = color;
        }

        public string Content { get; }
        public ConsoleColor Color { get; }
    }
}