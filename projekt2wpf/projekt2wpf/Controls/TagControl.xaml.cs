using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace projekt2wpf
{
    public partial class TagControl : UserControl
    {
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register(nameof(Tags), typeof(IEnumerable<string>), typeof(TagControl),
                new PropertyMetadata(null, OnTagsChanged));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(TagControl),
                new PropertyMetadata(false, OnRebuildTrigger));

        public static readonly DependencyProperty RemoveTagCommandProperty =
            DependencyProperty.Register(nameof(RemoveTagCommand), typeof(ICommand), typeof(TagControl),
                new PropertyMetadata(null, OnRebuildTrigger));

        public IEnumerable<string>? Tags
        {
            get => (IEnumerable<string>?)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        public ICommand? RemoveTagCommand
        {
            get => (ICommand?)GetValue(RemoveTagCommandProperty);
            set => SetValue(RemoveTagCommandProperty, value);
        }

        public TagControl() => InitializeComponent();

        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (TagControl)d;
            if (e.OldValue is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= ctrl.OnCollectionChanged;
            if (e.NewValue is INotifyCollectionChanged newColl)
                newColl.CollectionChanged += ctrl.OnCollectionChanged;
            ctrl.RebuildChips();
        }

        private static void OnRebuildTrigger(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((TagControl)d).RebuildChips();

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => RebuildChips();

        private void RebuildChips()
        {
            TagPanel.Children.Clear();
            if (Tags == null) return;

            var bg = Application.Current?.Resources["PrimaryBrush"] as Brush
                     ?? new SolidColorBrush(Color.FromRgb(0x2D, 0x35, 0x61));

            foreach (var tag in Tags)
            {
                var panel = new StackPanel
                {
                    Orientation       = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(new TextBlock
                {
                    Text              = tag,
                    Foreground        = Brushes.White,
                    FontSize          = 11,
                    VerticalAlignment = VerticalAlignment.Center
                });

                if (IsEditable && RemoveTagCommand != null)
                {
                    panel.Children.Add(new Button
                    {
                        Content           = "×",
                        FontSize          = 13,
                        Width             = 18,
                        Height            = 18,
                        Padding           = new Thickness(0),
                        Margin            = new Thickness(6, 0, 0, 0),
                        Background        = Brushes.Transparent,
                        BorderThickness   = new Thickness(0),
                        Foreground        = Brushes.White,
                        Cursor            = Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                        Command           = RemoveTagCommand,
                        CommandParameter  = tag
                    });
                }

                TagPanel.Children.Add(new Border
                {
                    Background   = bg,
                    CornerRadius = new CornerRadius(12),
                    Margin       = new Thickness(3, 3, 0, 0),
                    Padding      = new Thickness(9, 4, 9, 4),
                    Child        = panel
                });
            }
        }
    }
}
