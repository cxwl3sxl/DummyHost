using System;
using System.Collections.Generic;

namespace DummyHost
{
    public class RollConsole
    {
        private readonly object _lock = new object();
        private readonly List<Item> _items = new List<Item>();
        private readonly int _count;
        private int _top = -1;

        public RollConsole(int count)
        {
            _count = count;
        }

        public void WriteLine(string content, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (_lock)
            {
                if (_top == -1) _top = Console.CursorTop;
                if (_items.Count > _count)
                {
                    _items.RemoveAt(0);
                }

                _items.Add(new Item(content, color));
                Console.CursorTop = _top;
                foreach (var item in _items)
                {
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = item.Color;
                    Console.Write(item.Content);
                }
            }
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