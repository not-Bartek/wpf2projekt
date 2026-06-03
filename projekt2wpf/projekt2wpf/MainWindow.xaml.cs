using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace projekt2wpf
{
    public class Book
    {
        public string  Title      { get; set; } = "";
        public string  Author     { get; set; } = "";
        public byte[]? CoverImage { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter)     => _execute(parameter);
        public void RaiseCanExecuteChanged()       => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class ByteToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                var bmp = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bmp.BeginInit();
                bmp.StreamSource  = ms;
                bmp.CacheOption   = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private Book?  _selectedBook;
        private string _statusMessage = "Ready";

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? AddBookRequested;

        public ObservableCollection<Book> Books { get; } = new();
        public int BookCount => Books.Count;

        public Book? SelectedBook
        {
            get => _selectedBook;
            set
            {
                _selectedBook = value;
                OnPropertyChanged();
                DeleteBookCommand.RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand AddBookCommand    { get; }
        public RelayCommand DeleteBookCommand { get; }

        public MainViewModel()
        {
            AddBookCommand = new RelayCommand(
                _ => AddBookRequested?.Invoke());

            DeleteBookCommand = new RelayCommand(
                _ => DeleteSelectedBook(),
                _ => SelectedBook != null);

            Books.CollectionChanged += (_, _) => OnPropertyChanged(nameof(BookCount));
        }

        public void AddBook(Book book)
        {
            Books.Add(book);
            StatusMessage = "Book added";
        }

        private void DeleteSelectedBook()
        {
            if (SelectedBook == null) return;
            Books.Remove(SelectedBook);
            SelectedBook  = null;
            StatusMessage = "Book removed";
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal class AddBookWindow : Window
    {
        private readonly TextBox    _titleBox;
        private readonly TextBox    _authorBox;
        private readonly Image      _coverImg;
        private readonly TextBlock  _noCoverText;
        private readonly Button     _saveButton;
        private          byte[]?    _coverBytes;

        public Book? Result { get; private set; }

        public AddBookWindow()
        {
            Title                   = "Add Book";
            Width                   = 340;
            Height                  = 480;
            ResizeMode              = ResizeMode.NoResize;
            WindowStartupLocation   = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(20) };

            for (int i = 0; i < 9; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions[7].Height = new GridLength(1, GridUnitType.Star);

            var titleLabel = new TextBlock
            {
                Text       = "Title",
                FontWeight = FontWeights.SemiBold,
                Margin     = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(titleLabel, 0);

            _titleBox = new TextBox
            {
                Padding = new Thickness(6, 4, 6, 4),
                Margin  = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(_titleBox, 1);

            var authorLabel = new TextBlock
            {
                Text       = "Author",
                FontWeight = FontWeights.SemiBold,
                Margin     = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(authorLabel, 2);

            _authorBox = new TextBox
            {
                Padding = new Thickness(6, 4, 6, 4),
                Margin  = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(_authorBox, 3);

            var coverLabel = new TextBlock
            {
                Text       = "Cover",
                FontWeight = FontWeights.SemiBold,
                Margin     = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(coverLabel, 4);

            var coverGrid = new Grid();
            _noCoverText = new TextBlock
            {
                Text                = "No Cover",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center,
                Foreground          = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)),
                FontSize            = 12
            };
            _coverImg = new Image
            {
                Stretch    = Stretch.UniformToFill,
                Visibility = Visibility.Collapsed
            };
            coverGrid.Children.Add(_noCoverText);
            coverGrid.Children.Add(_coverImg);

            var coverBorder = new Border
            {
                Width               = 120,
                Height              = 160,
                Background          = new SolidColorBrush(Color.FromRgb(0xE4, 0xE4, 0xE4)),
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius        = new CornerRadius(4),
                ClipToBounds        = true,
                Margin              = new Thickness(0, 0, 0, 10),
                Child               = coverGrid
            };
            Grid.SetRow(coverBorder, 5);

            var browseButton = new Button
            {
                Content             = "Browse Image...",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding             = new Thickness(10, 5, 10, 5)
            };
            browseButton.Click += OnBrowseClick;
            Grid.SetRow(browseButton, 6);

            var accentBrush = Application.Current.Resources["AccentBrush"] as SolidColorBrush;

            _saveButton = new Button
            {
                Content     = "Save",
                Width       = 80,
                Padding     = new Thickness(0, 5, 0, 5),
                Margin      = new Thickness(0, 0, 10, 0),
                IsEnabled   = false,
                Background  = accentBrush,
                Foreground  = Brushes.White,
                BorderBrush = accentBrush
            };
            _saveButton.Click += OnSaveClick;

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width   = 80,
                Padding = new Thickness(0, 5, 0, 5)
            };
            cancelButton.Click += OnCancelClick;

            var buttonPanel = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin              = new Thickness(0, 12, 0, 0)
            };
            buttonPanel.Children.Add(_saveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 8);

            _titleBox.TextChanged += (_, _) =>
                _saveButton.IsEnabled = !string.IsNullOrWhiteSpace(_titleBox.Text);

            grid.Children.Add(titleLabel);
            grid.Children.Add(_titleBox);
            grid.Children.Add(authorLabel);
            grid.Children.Add(_authorBox);
            grid.Children.Add(coverLabel);
            grid.Children.Add(coverBorder);
            grid.Children.Add(browseButton);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg" };
            if (dlg.ShowDialog(this) != true) return;

            _coverBytes = File.ReadAllBytes(dlg.FileName);

            var bmp = new BitmapImage();
            using var ms = new MemoryStream(_coverBytes);
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption  = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            _coverImg.Source     = bmp;
            _coverImg.Visibility = Visibility.Visible;
            _noCoverText.Visibility = Visibility.Collapsed;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            Result = new Book
            {
                Title      = _titleBox.Text.Trim(),
                Author     = _authorBox.Text.Trim(),
                CoverImage = _coverBytes
            };
            DialogResult = true;
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm            = new MainViewModel();
            DataContext    = _vm;
            _vm.AddBookRequested += OpenAddBookDialog;
        }

        private void OpenAddBookDialog()
        {
            var dialog = new AddBookWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result != null)
                _vm.AddBook(dialog.Result);
        }
    }
}
