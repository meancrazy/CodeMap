using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Common.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace Common.ViewModels
{
    public class CodeMapMarginViewModel : ReactiveObject, IDisposable
    {
        #region private

        public const string MarginName = "CodeMapMargin";

        private readonly IWpfTextView _textView;
        private readonly IOutliningManager _outliningManager;
        private readonly CompositeDisposable _cleanup;

        private readonly ReactiveList<Item> _items = new ReactiveList<Item>();

        #endregion

        public CodeMapMarginViewModel(IWpfTextView textView, IOutliningManager outliningManager)
        {
            _textView = textView;
            _outliningManager = outliningManager;

            var filter = this.WhenAnyValue(x => x.SearchString)
                             .DistinctUntilChanged()
                             .Throttle(TimeSpan.FromSeconds(0.3))
                             .Select(x => new Func<Item, bool>(i => string.IsNullOrEmpty(x)|| i.Contains(x)));

            var d0 = _items.Filter(Items, filter, DispatcherScheduler.Current);
            
            ParseSyntaxtTree = ReactiveCommand.CreateFromObservable(ParseSyntaxtTreeImpl);
            ParseSyntaxtTree.ThrownExceptions.Subscribe(x => this.Log().ErrorException("ParseSyntaxtTree", x)); // TODO:
            ParseSyntaxtTree.SubscribeOnDispatcher()
                            .Subscribe(x =>
                            {
                                using (_items.SuppressChangeNotifications())
                                {
                                    _items.Clear();
                                    _items.AddRange(x);
                                }
                            });
            
            GotoLine = ReactiveCommand.CreateFromObservable(GotoLineImpl);
            GotoLine.ThrownExceptions.Subscribe(x => this.Log().ErrorException("GotoLine", x)); // TODO:

            var d1 = Observable.FromEventPattern<TextContentChangedEventArgs>(e => _textView.TextBuffer.ChangedLowPriority += e,
                                                                              e => _textView.TextBuffer.ChangedLowPriority -= e)
                               .Select(x => Unit.Default)
                               .Throttle(TimeSpan.FromSeconds(1))
                               .InvokeCommand(this, x => x.ParseSyntaxtTree);
            
            _cleanup = new CompositeDisposable(d0, d1);
        }

        #region logic

        private IObservable<Unit> GotoLineImpl()
        {
            return Observable.Start(() =>
            {
                var item = SelectedItem;

                if (item == null)
                    return Unit.Default;

                if (!_textView.HasAggregateFocus)
                    _textView.VisualElement.Focus();

                var snapshot = _textView.TextBuffer.CurrentSnapshot;

                var span = new SnapshotSpan(snapshot, item.Span.Start, item.Span.Length);

                foreach (var region in _outliningManager.GetCollapsedRegions(span))
                    _outliningManager.Expand(region);

                _textView.ViewScroller.EnsureSpanVisible(span, EnsureSpanVisibleOptions.ShowStart);
                _textView.Caret.MoveTo(span.Start);

                return Unit.Default;
            }, DispatcherScheduler.Current);
        }

        private IObservable<List<Item>> ParseSyntaxtTreeImpl()
        {
            return Observable.Start(() =>
            {
                var text = _textView.TextSnapshot.GetText();
                var tree = CSharpSyntaxTree.ParseText(text);

                var root = tree.GetRoot();

                var list = new List<Item>();

                var regionDirectives = root.DescendantNodes(null, true).OfType<RegionDirectiveTriviaSyntax>().Reverse().ToList();
                var endRegionDirectives = root.DescendantNodes(null, true).OfType<EndRegionDirectiveTriviaSyntax>().ToList();

                foreach (var startNode in regionDirectives)
                {
                    var endNode = endRegionDirectives.First(r => r.SpanStart > startNode.SpanStart);
                    endRegionDirectives.Remove(endNode);

                    list.Add(new Item(startNode.ToString(), ItemType.Region,  new TextSpan(startNode.Span.Start, endNode.Span.End - startNode.Span.Start)));
                }

                var namespaces = root.DescendantNodes()
                                     .OfType<NamespaceDeclarationSyntax>()
                                     .Select(x => new Item(x.Name.ToString(), ItemType.Namespace, x.Span))
                                     .ToList();

                if (namespaces.Count > 1)
                    list.AddRange(namespaces);

                var classes = root.DescendantNodes()
                                  .OfType<ClassDeclarationSyntax>()
                                  .Select(x => new Item(x.Identifier.ValueText, ItemType.Class, x.Span))
                                  .ToList();

                list.AddRange(classes);

                var interfaces = root.DescendantNodes()
                                     .OfType<InterfaceDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.Interface, x.Span))
                                     .ToList();

                list.AddRange(interfaces);

                var enums = root.DescendantNodes()
                                     .OfType<EnumDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.Enum, x.Span))
                                     .ToList();

                list.AddRange(enums);

                var enumMembers = root.DescendantNodes()
                                     .OfType<EnumMemberDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.EnumMember, x.Span))
                                     .ToList();

                list.AddRange(enumMembers);

                var structs = root.DescendantNodes()
                                     .OfType<StructDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.Struct, x.Span))
                                     .ToList();

                list.AddRange(structs);

                var constructors = root.DescendantNodes()
                                       .OfType<ConstructorDeclarationSyntax>()
                                       .Select(x => new Item(x.Identifier.ValueText, ItemType.Constructor, x.Span))
                                       .ToList();

                list.AddRange(constructors);

                var methods = root.DescendantNodes()
                                  .OfType<MethodDeclarationSyntax>()
                                  .Select(x => new Item(x.Identifier.ValueText, ItemType.Method, x.Span))
                                  .ToList();

                list.AddRange(methods);

                var fields = root.DescendantNodes()
                                 .OfType<FieldDeclarationSyntax>()
                                 .SelectMany(x => x.Declaration.Variables, 
                                             (f, v) => f.Modifiers.Any(SyntaxKind.ConstKeyword) ? new Item(v.Identifier.ValueText, ItemType.Const, v.Span) 
                                                                                                : new Item(v.Identifier.ValueText, ItemType.Field, v.Span))
                                 .ToList();

                list.AddRange(fields);

                var properties = root.DescendantNodes()
                                     .OfType<PropertyDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.Property, x.Span))
                                     .ToList();

                list.AddRange(properties);

                var eventFields = root.DescendantNodes()
                                      .OfType<EventFieldDeclarationSyntax>()
                                      .SelectMany(x => x.Declaration.Variables, (f, v) => new Item(v.Identifier.ValueText, ItemType.Event, v.Span))
                                      .ToList();

                list.AddRange(eventFields);

                var eventProperties = root.DescendantNodes()
                                          .OfType<EventDeclarationSyntax>()
                                          .Select(x => new Item(x.Identifier.ValueText, ItemType.Event, x.Span))
                                          .ToList();

                list.AddRange(eventProperties);

                var delegates = root.DescendantNodes()
                                     .OfType<DelegateDeclarationSyntax>()
                                     .Select(x => new Item(x.Identifier.ValueText, ItemType.Delegate, x.Span))
                                     .ToList();

                list.AddRange(delegates);

                return list.BuildTree();
            });
        }

        #endregion

        #region public

        public ReactiveList<Item> Items { get; } = new ReactiveList<Item>();

        [Reactive]
        public Item SelectedItem { get; set; }

        public ReactiveCommand<Unit, List<Item>> ParseSyntaxtTree { get; }

        public ReactiveCommand<Unit, Unit> GotoLine { get; }

        [Reactive]
        public string SearchString { get; set; }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cleanup.Dispose();
            _items.Clear();
        }

        #endregion
    }
}
 