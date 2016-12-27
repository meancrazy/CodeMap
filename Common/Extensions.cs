using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Common.Domain;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;

namespace Common
{
    public static class Extensions
    {
        public static int GetLineNumber(this ConstructorDeclarationSyntax node)
        {
            return node.SyntaxTree.GetLineSpan(node.Identifier.Span).StartLinePosition.Line + 1;
        }

        public static int GetLineNumber(this MethodDeclarationSyntax node)
        {
            return node.SyntaxTree.GetLineSpan(node.Identifier.Span).StartLinePosition.Line + 1;
        }

        public static int GetLineNumber(this PropertyDeclarationSyntax node)
        {
            return node.SyntaxTree.GetLineSpan(node.Identifier.Span).StartLinePosition.Line + 1;
        }

        public static int GetLineNumber(this VariableDeclaratorSyntax node)
        {
            return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        }

        public static int GetLineNumber(this DirectiveTriviaSyntax node)
        {
            return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        }

        public static bool Contains(this Item item, string filter)
        {
            return Contains(item.Name, filter);//|| item.ChildList.Any(x => x.Contains(filter));
        }

        /// <summary> Ignore case String.Ccntains </summary>
        /// <param name="source"></param>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        public static bool Contains(this string source, string toCheck)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private static Tuple<Item, Item> FindParent(Item item1, Item item2)
        {
            if (item1 == item2)
                return null;

            var intersection = item1.Span.Intersection(item2.Span);

            if (intersection == null)
                return null;

            return intersection == item2.Span
                ? new Tuple<Item, Item>(item1, item2)
                : new Tuple<Item, Item>(item2, item1);
        }

        public static void SetParent(this Item item1, Item item2)
        {
            var tuple = FindParent(item1, item2);

            if (tuple == null)
                return;

            var parent = tuple.Item1;
            var child = tuple.Item2;

            if (child.Parent == null)
            {
                child.Parent = parent;
            }
            else
            {
                var oldParent = child.Parent;

                if (oldParent == parent)
                    return;

                tuple = FindParent(parent, oldParent);
                if (tuple == null)
                    throw new Exception("holy fucking shit");

                child.Parent = tuple.Item2;
            }
        }

        public static List<Item> BuildTree(this List<Item> items)
        {
            items.ForEach(i => items.ForEach(i.SetParent));
            items.ForEach(i => i.ChildList.AddRange(items.Where(ch => ch.Parent == i)));

            return items.Where(i => i.Parent == null).ToList();
        }

        public static void DisposeItemsAndClear<T>(this ReactiveList<T> list) where T : IDisposable
        {
            foreach (var t in list)
                t.Dispose();

            list.Clear();
        }

        public static IDisposable Filter(this ReactiveList<Item> source, ReactiveList<Item> result, IObservable<Func<Item, bool>> filter, IScheduler observer)
        {
            // firstly selecting default values for source list and filter observable for getting result
            var combinedList = Observable.Return(Unit.Default).Merge(source.Changed.Select(_ => Unit.Default)).Select(_ => source);
            var combinedFilter = Observable.Return(new Func<Item, bool>(t => true)).Merge(filter);

            var d0 = combinedList.CombineLatest(combinedFilter, (l, f) => l.Select(x => x.Filter(f)).Where(x => x != null))
                                 .ObserveOn(observer)
                                 .Subscribe(x =>
                                 {
                                     using (result.SuppressChangeNotifications())
                                     {
                                         result.Clear();
                                         result.AddRange(x);
                                     }
                                 });

            return new CompositeDisposable(d0);
        }

        private static Item Filter(this Item source, Func<Item, bool> predicate)
        {
            if (source.ChildList == null || source.ChildList.Count == 0)
                return predicate(source) ? source : null;

            var results = source.ChildList
                                .Select(i => i.Filter(predicate))
                                .Where(i => i != null)
                                .ToList();

            if (!results.Any())
                return predicate(source) ? source.Clone() : null;

            var result = source.Clone();
            result.ChildList.AddRange(results);
            return result;
        }
    }
}