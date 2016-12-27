using System.ComponentModel.Composition;
using Common.ViewModels;
using Common.Views;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;

namespace CodeMap
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name(CodeMapMarginViewModel.MarginName)]
    [Order(After = PredefinedMarginNames.VerticalScrollBarContainer)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class CodeMapMarginFactory : IWpfTextViewMarginProvider
    {
        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        #region IWpfTextViewMarginProvider

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            // TODO: currently supporting only CSharp
            var isCSharp = wpfTextViewHost.TextView.TextBuffer.ContentType.IsOfType("CSharp");

            if (!isCSharp) return null;

            var outliningManager = OutliningManagerService.GetOutliningManager(wpfTextViewHost.TextView);

            var viewModel = new CodeMapMarginViewModel(wpfTextViewHost.TextView, outliningManager);
            return new CodeMapMargin { ViewModel = viewModel };
        }

        #endregion
    }
}
