using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Common.Domain
{
    public class Item : IDisposable
    {
        public string Name { get; }

        public ItemType Type { get; }

        public TextSpan Span { get; }

        public Item Parent { get; set; }

        public List<Item> ChildList { get; } = new List<Item>();

        public Item(string name, ItemType type, TextSpan span)
        {
            Name = name;
            Type = type;
            Span = span;
        }

        public override string ToString()
        {
            return $"{Type} {Name}";
        }
        
        public Item Clone()
        {
            return new Item(Name, Type, Span);
        }

        public void Dispose()
        {
            ChildList.Clear();
        }
    }
}