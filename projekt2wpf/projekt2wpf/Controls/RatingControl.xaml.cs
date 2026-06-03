using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace projekt2wpf
{
    public partial class RatingControl : UserControl
    {
        private readonly TextBlock[] _stars = new TextBlock[5];

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(RatingControl),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(RatingControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty StarSizeProperty =
            DependencyProperty.Register(nameof(StarSize), typeof(double), typeof(RatingControl),
                new PropertyMetadata(18.0, OnStarSizeChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public double StarSize
        {
            get => (double)GetValue(StarSizeProperty);
            set => SetValue(StarSizeProperty, value);
        }

        public RatingControl()
        {
            InitializeComponent();
            var gold = new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x23));
            for (var i = 0; i < 5; i++)
            {
                var idx  = i;
                var star = new TextBlock
                {
                    Text       = "☆",
                    FontSize   = 18,
                    Foreground = gold,
                    Cursor     = Cursors.Hand,
                    Margin     = new Thickness(1, 0, 1, 0)
                };
                star.MouseLeftButtonDown += (_, _) => { if (!IsReadOnly) Value = idx + 1; };
                star.MouseEnter          += (_, _) => { if (!IsReadOnly) Highlight(idx + 1); };
                star.MouseLeave          += (_, _) => { if (!IsReadOnly) DrawStars(Value); };
                _stars[i] = star;
                StarPanel.Children.Add(star);
            }
            DrawStars(0);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((RatingControl)d).DrawStars((double)e.NewValue);

        private static void OnStarSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (RatingControl)d;
            foreach (var s in ctrl._stars) s.FontSize = (double)e.NewValue;
        }

        private void DrawStars(double val)
        {
            for (var i = 0; i < 5; i++)
                _stars[i].Text = i < val ? "★" : "☆";
        }

        private void Highlight(int upTo)
        {
            for (var i = 0; i < 5; i++)
                _stars[i].Text = i < upTo ? "★" : "☆";
        }
    }
}
