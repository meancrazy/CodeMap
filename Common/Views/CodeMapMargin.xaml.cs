using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Common.ViewModels;
using Microsoft.VisualStudio.Text.Editor;
using ReactiveUI;

namespace Common.Views
{
    /// <summary> Interaction logic for CodeMapMargin.xaml </summary>
    public partial class CodeMapMargin : IWpfTextViewMargin, IViewFor<CodeMapMarginViewModel>
    {
        /// <summary> A value indicating whether the object is disposed. </summary>
        private bool _isDisposed;

        public CodeMapMargin()
        {
            InitializeComponent();
            this.WhenActivated(Initialize);
        }

        private IEnumerable<IDisposable> Initialize()
        {
            yield return this.WhenAnyValue(x => x.ViewModel)
                             .SelectMany(x => x.ParseSyntaxtTree.Execute())
                             .Subscribe();

            yield return this.OneWayBind(ViewModel, x => x.Items, x => x.Items.ItemsSource);
            yield return this.Bind(ViewModel, x => x.SelectedItem, x => x.Items.SelectedItem);
            yield return this.Bind(ViewModel, x => x.SearchString, x => x.FilterTextBox.Text);
        }

        #region IViewFor

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(CodeMapMarginViewModel), typeof(CodeMapMargin));

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (CodeMapMarginViewModel)value; }
        }

        public CodeMapMarginViewModel ViewModel
        {
            get { return (CodeMapMarginViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        #endregion

        #region IWpfTextViewMargin

        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> that implements the visual representation of the margin.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The margin is disposed.</exception>
        public FrameworkElement VisualElement
        {
            // Since this margin implements Canvas, this is the object which renders
            // the margin.
            get
            {
                ThrowIfDisposed();
                return this;
            }
        }

        #endregion

        #region ITextViewMargin

        public double MarginSize
        {
            get
            {
                ThrowIfDisposed();
                return ActualWidth;
            }
        }
        
        public bool Enabled
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return string.Equals(marginName, CodeMapMarginViewModel.MarginName, StringComparison.OrdinalIgnoreCase) ? this : null;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            GC.SuppressFinalize(this);
            _isDisposed = true;
        }

        #endregion

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(CodeMapMarginViewModel.MarginName);
            }
        }

        private void ThumbOnDragDelta(object sender, DragDeltaEventArgs e)
        {
            var width = Width;
            width -= e.HorizontalChange;

            if (width < 6)
                width = 6;

            Width = width;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel.GotoLine.Execute();
        }
    }
}
