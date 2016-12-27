using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Common.Domain
{
    // taken from https://github.com/github/VisualStudio
    public class FilterTextBox : TextBox
    {
        public static readonly DependencyProperty PromptTextProperty = DependencyProperty.Register("PromptText", typeof(string), typeof(FilterTextBox), new UIPropertyMetadata("Filter"));

        [Localizability(LocalizationCategory.Text)]
        [DefaultValue("Filter")]
        public string PromptText
        {
            get { return (string)GetValue(PromptTextProperty); }
            set { SetValue(PromptTextProperty, value); }
        }

        public FilterTextBox()
        {
            // http://stackoverflow.com/a/661224/2114
            AddHandler(PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton), true);
            AddHandler(GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText), true);
            AddHandler(MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText), true);
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ClearButtonClick), true);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !String.IsNullOrEmpty(Text))
            {
                Clear();
                e.Handled = true;
            }

            OnPreviewKeyDown(e);
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            Clear();
            e.Handled = true;
        }

        // http://stackoverflow.com/a/661224/2114
        private static void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            var textBox = FindTextBoxInAncestors(e.OriginalSource as UIElement);

            if (textBox == null || textBox.IsKeyboardFocusWithin) return;

            // If the text box is not yet focussed, give it the focus and
            // stop further processing of this click event.
            textBox.Focus();
            e.Handled = true;
        }

        private static TextBox FindTextBoxInAncestors(DependencyObject current)
        {
            while (current != null)
            {
                var tb = current as TextBox;
                if (tb != null)
                    return tb;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;

            textBox?.SelectAll();
        }
    }
}