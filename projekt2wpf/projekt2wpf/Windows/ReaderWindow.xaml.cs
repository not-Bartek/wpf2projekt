using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
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

        private static readonly SolidColorBrush PrimaryBrush  = new(Color.FromRgb(0x2D, 0x35, 0x61));
        private static readonly SolidColorBrush AccentBrush   = new(Color.FromRgb(0xE9, 0x45, 0x60));
        private static readonly SolidColorBrush CodeBg        = new(Color.FromRgb(0xF4, 0xF4, 0xF4));
        private static readonly SolidColorBrush CodeFg        = new(Color.FromRgb(0xD6, 0x33, 0x6C));
        private static readonly SolidColorBrush QuoteFg       = new(Color.FromRgb(0x55, 0x55, 0x55));
        private static readonly SolidColorBrush SeparatorBrush = new(Color.FromRgb(0xDD, 0xDD, 0xDD));
        private static readonly SolidColorBrush QuoteBg       = new(Color.FromArgb(0x10, 0xE9, 0x45, 0x60));
        private static readonly FontFamily MonoFont           = new("Consolas, Courier New, monospace");

        private static FlowDocument BuildDocument(string title, string content)
        {
            var doc = new FlowDocument
            {
                FontFamily    = new FontFamily("Segoe UI"),
                FontSize      = 14,
                PagePadding   = new Thickness(28, 20, 28, 24),
                TextAlignment = TextAlignment.Left
            };

            if (!string.IsNullOrWhiteSpace(title))
                doc.Blocks.Add(new Paragraph(new Run(title))
                {
                    FontSize        = 24,
                    FontWeight      = FontWeights.Bold,
                    Foreground      = PrimaryBrush,
                    Margin          = new Thickness(0, 0, 0, 4),
                    BorderBrush     = SeparatorBrush,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding         = new Thickness(0, 0, 0, 14)
                });

            var lines = content.Split('\n');
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i].TrimEnd();

                if (TryParseFencedCode(lines, ref i, doc)) continue;
                if (TryParseIndentedCode(lines, ref i, doc)) continue;
                if (TryParseHorizontalRule(line, ref i, doc)) continue;
                if (TryParseHeader(line, ref i, doc)) continue;
                if (TryParseBlockquote(line, ref i, doc)) continue;
                if (TryParseUnorderedList(lines, ref i, doc)) continue;
                if (TryParseOrderedList(lines, ref i, doc)) continue;

                if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

                var para = new Paragraph { Margin = new Thickness(0, 0, 0, 8), LineHeight = 22 };
                while (i < lines.Length)
                {
                    var ll = lines[i].TrimEnd();
                    if (string.IsNullOrWhiteSpace(ll)) break;
                    if (IsBlockStart(ll)) break;
                    if (para.Inlines.Count > 0)
                        para.Inlines.Add(new Run(" "));
                    AddInlines(para.Inlines, ll);
                    i++;
                }
                if (para.Inlines.Count > 0)
                    doc.Blocks.Add(para);
            }

            return doc;
        }

        private static bool TryParseFencedCode(string[] lines, ref int i, FlowDocument doc)
        {
            var line = lines[i].TrimEnd();
            if (!line.TrimStart().StartsWith("```")) return false;

            i++;
            var sb = new StringBuilder();
            while (i < lines.Length && !lines[i].TrimEnd().TrimStart().StartsWith("```"))
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(lines[i].TrimEnd());
                i++;
            }
            i++;

            doc.Blocks.Add(new Paragraph(new Run(sb.ToString()))
            {
                Background  = CodeBg,
                Padding     = new Thickness(14, 10, 14, 10),
                Margin      = new Thickness(0, 6, 0, 10),
                FontFamily  = MonoFont,
                FontSize    = 12.5,
                LineHeight  = 19
            });
            return true;
        }

        private static bool TryParseIndentedCode(string[] lines, ref int i, FlowDocument doc)
        {
            var line = lines[i];
            if (!line.StartsWith("    ") && !line.StartsWith("\t")) return false;

            var sb = new StringBuilder();
            while (i < lines.Length && (lines[i].StartsWith("    ") || lines[i].StartsWith("\t")))
            {
                if (sb.Length > 0) sb.Append('\n');
                var stripped = lines[i].TrimEnd();
                sb.Append(stripped.StartsWith("    ") ? stripped[4..] : stripped.TrimStart('\t'));
                i++;
            }

            doc.Blocks.Add(new Paragraph(new Run(sb.ToString()))
            {
                Background  = CodeBg,
                Padding     = new Thickness(14, 10, 14, 10),
                Margin      = new Thickness(0, 6, 0, 10),
                FontFamily  = MonoFont,
                FontSize    = 12.5,
                LineHeight  = 19
            });
            return true;
        }

        private static bool TryParseHorizontalRule(string line, ref int i, FlowDocument doc)
        {
            if (!Regex.IsMatch(line, @"^(\-{3,}|\*{3,}|_{3,})\s*$")) return false;

            doc.Blocks.Add(new Paragraph
            {
                BorderBrush     = SeparatorBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin          = new Thickness(0, 10, 0, 10),
                Padding         = new Thickness(0)
            });
            i++;
            return true;
        }

        private static bool TryParseHeader(string line, ref int i, FlowDocument doc)
        {
            int level = 0;
            while (level < line.Length && line[level] == '#') level++;
            if (level == 0 || level > 6 || level >= line.Length || line[level] != ' ') return false;

            var text = line[(level + 1)..].Trim();
            var (fs, fw, mt, mb) = level switch
            {
                1 => (22.0, FontWeights.Bold,     14.0, 6.0),
                2 => (18.0, FontWeights.Bold,     12.0, 5.0),
                3 => (16.0, FontWeights.SemiBold, 10.0, 4.0),
                4 => (15.0, FontWeights.SemiBold,  8.0, 3.0),
                5 => (14.0, FontWeights.Medium,    6.0, 2.0),
                _ => (13.0, FontWeights.Medium,    4.0, 2.0)
            };

            var para = new Paragraph
            {
                FontSize   = fs,
                FontWeight = fw,
                Foreground = PrimaryBrush,
                Margin     = new Thickness(0, mt, 0, mb)
            };
            AddInlines(para.Inlines, text);
            doc.Blocks.Add(para);
            i++;
            return true;
        }

        private static bool TryParseBlockquote(string line, ref int i, FlowDocument doc)
        {
            if (!line.StartsWith(">")) return false;

            var para = new Paragraph
            {
                Padding         = new Thickness(14, 6, 8, 6),
                Margin          = new Thickness(0, 4, 0, 4),
                BorderBrush     = AccentBrush,
                BorderThickness = new Thickness(4, 0, 0, 0),
                Background      = QuoteBg,
                Foreground      = QuoteFg,
                FontStyle       = FontStyles.Italic
            };
            AddInlines(para.Inlines, line.TrimStart('>').Trim());
            doc.Blocks.Add(para);
            i++;
            return true;
        }

        private static bool TryParseUnorderedList(string[] lines, ref int i, FlowDocument doc)
        {
            var line = lines[i].TrimEnd();
            if (line.Length < 2) return false;
            if (!((line[0] == '-' || line[0] == '*' || line[0] == '+') && line[1] == ' ')) return false;

            var list = new List
            {
                MarkerStyle  = TextMarkerStyle.Disc,
                Margin       = new Thickness(0, 4, 0, 6),
                Padding      = new Thickness(22, 0, 0, 0),
                MarkerOffset = 8
            };
            while (i < lines.Length)
            {
                var ll = lines[i].TrimEnd();
                if (ll.Length < 2 || !((ll[0] == '-' || ll[0] == '*' || ll[0] == '+') && ll[1] == ' ')) break;
                var li = new ListItem { Margin = new Thickness(0, 2, 0, 2) };
                var lp = new Paragraph { Margin = new Thickness(0) };
                AddInlines(lp.Inlines, ll[2..].Trim());
                li.Blocks.Add(lp);
                list.ListItems.Add(li);
                i++;
            }
            doc.Blocks.Add(list);
            return true;
        }

        private static bool TryParseOrderedList(string[] lines, ref int i, FlowDocument doc)
        {
            var line = lines[i].TrimEnd();
            var m = Regex.Match(line, @"^(\d+)\. (.*)");
            if (!m.Success) return false;

            var list = new List
            {
                MarkerStyle  = TextMarkerStyle.Decimal,
                Margin       = new Thickness(0, 4, 0, 6),
                Padding      = new Thickness(22, 0, 0, 0),
                MarkerOffset = 8,
                StartIndex   = int.TryParse(m.Groups[1].Value, out var si) ? si : 1
            };
            while (i < lines.Length)
            {
                var ll = lines[i].TrimEnd();
                var lm = Regex.Match(ll, @"^\d+\. (.*)");
                if (!lm.Success) break;
                var li = new ListItem { Margin = new Thickness(0, 2, 0, 2) };
                var lp = new Paragraph { Margin = new Thickness(0) };
                AddInlines(lp.Inlines, lm.Groups[1].Value.Trim());
                li.Blocks.Add(lp);
                list.ListItems.Add(li);
                i++;
            }
            doc.Blocks.Add(list);
            return true;
        }

        private static bool IsBlockStart(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            if (line.TrimStart().StartsWith("```")) return true;
            if (line.StartsWith("    ") || line.StartsWith("\t")) return true;
            if (Regex.IsMatch(line, @"^(\-{3,}|\*{3,}|_{3,})\s*$")) return true;
            if (line.Length > 0 && line[0] == '#') return true;
            if (line.StartsWith(">")) return true;
            if (line.Length >= 2 && (line[0] == '-' || line[0] == '*' || line[0] == '+') && line[1] == ' ') return true;
            if (Regex.IsMatch(line, @"^\d+\. ")) return true;
            return false;
        }

        private static void AddInlines(InlineCollection inlines, string text)
        {
            int pos = 0;
            while (pos < text.Length)
            {
                char c = text[pos];

                if (c == '*' || c == '_')
                {
                    if (TryEmit(text, pos, "***", inlines, t => new Run(t) { FontWeight = FontWeights.Bold, FontStyle = FontStyles.Italic }, out int np)) { pos = np; continue; }
                    if (TryEmit(text, pos, "**",  inlines, t => new Run(t) { FontWeight = FontWeights.Bold }, out np)) { pos = np; continue; }
                    if (c == '_' && TryEmit(text, pos, "__", inlines, t => new Run(t) { FontWeight = FontWeights.Bold }, out np)) { pos = np; continue; }
                    if (c == '*' && TryEmitSingle(text, pos, '*', inlines, t => new Run(t) { FontStyle = FontStyles.Italic }, out np)) { pos = np; continue; }
                    if (c == '_' && (pos == 0 || !char.IsLetterOrDigit(text[pos - 1])) &&
                        TryEmitSingleBoundary(text, pos, '_', inlines, t => new Run(t) { FontStyle = FontStyles.Italic }, out np)) { pos = np; continue; }
                }

                if (c == '~' && pos + 1 < text.Length && text[pos + 1] == '~')
                {
                    if (TryEmit(text, pos, "~~", inlines, t => new Run(t) { TextDecorations = TextDecorations.Strikethrough }, out int np)) { pos = np; continue; }
                }

                if (c == '`')
                {
                    if (TryEmitSingle(text, pos, '`', inlines, t => new Run(t)
                    {
                        FontFamily = MonoFont,
                        Background = CodeBg,
                        Foreground = CodeFg,
                        FontSize   = 12.5
                    }, out int np)) { pos = np; continue; }
                }

                if (c == '[')
                {
                    int cb = text.IndexOf(']', pos + 1);
                    if (cb > pos && cb + 1 < text.Length && text[cb + 1] == '(')
                    {
                        int cp = text.IndexOf(')', cb + 2);
                        if (cp > cb + 2)
                        {
                            inlines.Add(new Run(text[(pos + 1)..cb])
                            {
                                Foreground      = PrimaryBrush,
                                TextDecorations = TextDecorations.Underline
                            });
                            pos = cp + 1;
                            continue;
                        }
                    }
                }

                int next = pos + 1;
                while (next < text.Length)
                {
                    char nc = text[next];
                    if (nc == '*' || nc == '_' || nc == '`' || nc == '[' || nc == '~') break;
                    next++;
                }
                inlines.Add(new Run(text[pos..next]));
                pos = next;
            }
        }

        private static bool TryEmit(string text, int pos, string marker, InlineCollection inlines,
                                    Func<string, Inline> factory, out int newPos)
        {
            newPos = pos;
            if (pos + marker.Length > text.Length) return false;
            if (text.Substring(pos, marker.Length) != marker) return false;
            int end = text.IndexOf(marker, pos + marker.Length, StringComparison.Ordinal);
            if (end <= pos + marker.Length) return false;
            inlines.Add(factory(text[(pos + marker.Length)..end]));
            newPos = end + marker.Length;
            return true;
        }

        private static bool TryEmitSingle(string text, int pos, char marker, InlineCollection inlines,
                                          Func<string, Inline> factory, out int newPos)
        {
            newPos = pos;
            int end = text.IndexOf(marker, pos + 1);
            if (end <= pos + 1) return false;
            inlines.Add(factory(text[(pos + 1)..end]));
            newPos = end + 1;
            return true;
        }

        private static bool TryEmitSingleBoundary(string text, int pos, char marker, InlineCollection inlines,
                                                   Func<string, Inline> factory, out int newPos)
        {
            newPos = pos;
            int end = text.IndexOf(marker, pos + 1);
            if (end <= pos + 1) return false;
            if (end + 1 < text.Length && char.IsLetterOrDigit(text[end + 1])) return false;
            inlines.Add(factory(text[(pos + 1)..end]));
            newPos = end + 1;
            return true;
        }
    }
}
