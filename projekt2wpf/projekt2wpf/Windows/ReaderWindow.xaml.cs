using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace projekt2wpf
{
    public partial class ReaderWindow : Window
    {
        private ReaderViewModel? _vm;

        public ReaderWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
            _vm = DataContext as ReaderViewModel;
            if (_vm == null) return;
            _vm.PropertyChanged += OnVmChanged;
            UpdateDocument();
        }

        private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReaderViewModel.ChapterContent))
                UpdateDocument();
        }

        private void UpdateDocument()
        {
            if (_vm == null) return;
            ContentViewer.Document = BuildDocument(_vm.ChapterTitle, _vm.ChapterContent);
        }

        private static FlowDocument BuildDocument(string title, string content)
        {
            var doc = new FlowDocument
            {
                FontFamily  = new FontFamily("Segoe UI"),
                FontSize    = 14,
                LineHeight  = 22,
                PagePadding = new Thickness(20, 16, 20, 16)
            };

            if (!string.IsNullOrWhiteSpace(title))
                doc.Blocks.Add(new Paragraph(new Run(title))
                {
                    FontSize   = 22,
                    FontWeight = FontWeights.Bold,
                    Margin     = new Thickness(0, 0, 0, 16)
                });

            foreach (var raw in content.Split('\n'))
            {
                var line = raw.TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 4, 0, 0) });
                }
                else if (line.StartsWith("### "))
                {
                    doc.Blocks.Add(new Paragraph(new Run(line[4..]))
                        { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 4) });
                }
                else if (line.StartsWith("## "))
                {
                    doc.Blocks.Add(new Paragraph(new Run(line[3..]))
                        { FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 4) });
                }
                else if (line.StartsWith("# "))
                {
                    doc.Blocks.Add(new Paragraph(new Run(line[2..]))
                        { FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 12, 0, 4) });
                }
                else
                {
                    doc.Blocks.Add(new Paragraph(new Run(line)));
                }
            }

            return doc;
        }
    }
}
