using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace projekt2wpf
{
    public enum ReadingStatus { Unread, Reading, Read }

    public class Book : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void N<T>(ref T field, T val, [CallerMemberName] string? p = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, val)) return;
            field = val;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        private string        _title       = "";
        private string        _author      = "";
        private string        _description = "";
        private string?       _genre;
        private double?       _rating;
        private ReadingStatus _status      = ReadingStatus.Unread;
        private int?          _totalPages;
        private int           _currentPage;
        private string?       _bookFilePath;
        private byte[]?       _coverImage;

        public string        Title        { get => _title;       set => N(ref _title, value); }
        public string        Author       { get => _author;      set => N(ref _author, value); }
        public string        Description  { get => _description; set => N(ref _description, value); }
        public string?       Genre        { get => _genre;       set => N(ref _genre, value); }
        public double?       Rating       { get => _rating;      set => N(ref _rating, value); }
        public ReadingStatus Status       { get => _status;      set => N(ref _status, value); }
        public int?          TotalPages   { get => _totalPages;  set => N(ref _totalPages, value); }
        public int           CurrentPage  { get => _currentPage; set => N(ref _currentPage, value); }
        public string?       BookFilePath { get => _bookFilePath; set => N(ref _bookFilePath, value); }
        public byte[]?       CoverImage   { get => _coverImage;  set => N(ref _coverImage, value); }
        public List<string>  Tags         { get; set; } = new();
    }

    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action       _execute;
        private readonly Func<bool>?  _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter)     => _execute();
        public void Raise()                        => CommandManager.InvalidateRequerySuggested();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?>       _execute;
        private readonly Func<T?, bool>?  _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            var t = parameter is T v ? v : default;
            return _canExecute?.Invoke(t) ?? true;
        }

        public void Execute(object? parameter)
        {
            var t = parameter is T v ? v : default;
            _execute(t);
        }
    }

    public class ByteToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not byte[] bytes || bytes.Length == 0) return null;
            var bmp = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption  = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class LibraryRepository
    {
        private string? _filePath;
        private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

        public ObservableCollection<Book> Books    { get; } = new();
        public bool                       IsOpen   => _filePath != null;

        public void Create(string filePath)
        {
            _filePath = filePath;
            Books.Clear();
            Save();
        }

        public void Open(string filePath)
        {
            _filePath = filePath;
            var json  = File.ReadAllText(filePath, Encoding.UTF8);
            var list  = JsonSerializer.Deserialize<List<Book>>(json, _opts) ?? new();
            Books.Clear();
            foreach (var b in list) Books.Add(b);
        }

        public void Save()
        {
            if (_filePath == null) return;
            var json = JsonSerializer.Serialize(Books.ToList(), _opts);
            File.WriteAllText(_filePath, json, Encoding.UTF8);
        }
    }

    public class WelcomeViewModel : BaseViewModel
    {
        public RelayCommand CreateRepositoryCommand { get; }
        public RelayCommand OpenRepositoryCommand   { get; }

        public WelcomeViewModel(RelayCommand createCmd, RelayCommand openCmd)
        {
            CreateRepositoryCommand = createCmd;
            OpenRepositoryCommand   = openCmd;
        }
    }

    public class BookListViewModel : BaseViewModel
    {
        private readonly ObservableCollection<Book> _all;
        private readonly Action         _requestAdd;
        private readonly Action<Book>   _requestEdit;
        private readonly Action<Book>   _requestDelete;
        private readonly Action<Book>   _openDetail;

        private Book?  _selectedBook;
        private string _search       = "";
        private string _statusFilter = "All";
        private string _ratingFilter = "Any";
        private string _sortBy       = "Title";

        public ObservableCollection<Book> FilteredBooks { get; } = new();

        public IReadOnlyList<string> StatusOptions { get; } = new[] { "All", "Unread", "Reading", "Read" };
        public IReadOnlyList<string> RatingOptions { get; } = new[] { "Any", "1+", "2+", "3+", "4+", "5" };
        public IReadOnlyList<string> SortOptions   { get; } = new[] { "Title", "Author", "Rating" };

        public Book? SelectedBook
        {
            get => _selectedBook;
            set => SetProperty(ref _selectedBook, value);
        }

        public string Search
        {
            get => _search;
            set { SetProperty(ref _search, value); ApplyFilters(); OnPropertyChanged(nameof(HasActiveFilters)); }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { SetProperty(ref _statusFilter, value); ApplyFilters(); OnPropertyChanged(nameof(HasActiveFilters)); }
        }

        public string RatingFilter
        {
            get => _ratingFilter;
            set { SetProperty(ref _ratingFilter, value); ApplyFilters(); OnPropertyChanged(nameof(HasActiveFilters)); }
        }

        public string SortBy
        {
            get => _sortBy;
            set { SetProperty(ref _sortBy, value); ApplyFilters(); OnPropertyChanged(nameof(HasActiveFilters)); }
        }

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Search) ||
            StatusFilter != "All" ||
            RatingFilter != "Any" ||
            SortBy != "Title";

        public RelayCommand       AddBookCommand    { get; }
        public RelayCommand       EditBookCommand   { get; }
        public RelayCommand       DeleteBookCommand { get; }
        public RelayCommand       ClearFiltersCommand { get; }
        public RelayCommand<Book> OpenDetailCommand { get; }

        public BookListViewModel(
            ObservableCollection<Book> all,
            Action requestAdd, Action<Book> requestEdit,
            Action<Book> requestDelete, Action<Book> openDetail)
        {
            _all           = all;
            _requestAdd    = requestAdd;
            _requestEdit   = requestEdit;
            _requestDelete = requestDelete;
            _openDetail    = openDetail;

            AddBookCommand    = new RelayCommand(() => _requestAdd());
            EditBookCommand   = new RelayCommand(() => { if (SelectedBook != null) _requestEdit(SelectedBook); }, () => SelectedBook != null);
            DeleteBookCommand = new RelayCommand(() => { if (SelectedBook != null) _requestDelete(SelectedBook); }, () => SelectedBook != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters, () => HasActiveFilters);
            OpenDetailCommand   = new RelayCommand<Book>(b => { if (b != null) _openDetail(b); });

            _all.CollectionChanged += (_, _) => ApplyFilters();
            ApplyFilters();
        }

        private void ClearFilters()
        {
            _search       = "";
            _statusFilter = "All";
            _ratingFilter = "Any";
            _sortBy       = "Title";
            OnPropertyChanged(nameof(Search));
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(RatingFilter));
            OnPropertyChanged(nameof(SortBy));
            OnPropertyChanged(nameof(HasActiveFilters));
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var q = _all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(Search))
                q = q.Where(b =>
                    b.Title.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(Search, StringComparison.OrdinalIgnoreCase));

            if (StatusFilter != "All")
            {
                var s = StatusFilter switch
                {
                    "Reading" => ReadingStatus.Reading,
                    "Read"    => ReadingStatus.Read,
                    _         => ReadingStatus.Unread
                };
                q = q.Where(b => b.Status == s);
            }

            if (RatingFilter != "Any" && int.TryParse(RatingFilter[0].ToString(), out var min))
                q = q.Where(b => b.Rating >= min);

            q = SortBy switch
            {
                "Author" => q.OrderBy(b => b.Author),
                "Rating" => q.OrderByDescending(b => b.Rating ?? 0),
                _        => q.OrderBy(b => b.Title)
            };

            FilteredBooks.Clear();
            foreach (var b in q) FilteredBooks.Add(b);
        }
    }

    public class BookDetailViewModel : BaseViewModel
    {
        private readonly Action           _goBack;
        private readonly Action<Book>     _requestEdit;
        private readonly Action<Book,bool> _openReader;

        public Book Book { get; }

        public bool   HasDescription => !string.IsNullOrWhiteSpace(Book.Description);
        public bool   HasTags        => Book.Tags.Count > 0;
        public bool   HasPages       => Book.TotalPages.HasValue;
        public bool   HasFile        => !string.IsNullOrEmpty(Book.BookFilePath) && File.Exists(Book.BookFilePath);
        public bool   CanContinue    => HasFile && Book.CurrentPage > 0;
        public int    TotalPagesVal  => Book.TotalPages ?? 1;
        public int    ProgressPct    => TotalPagesVal > 0 ? (int)(Book.CurrentPage * 100.0 / TotalPagesVal) : 0;
        public string ProgressText   => Book.TotalPages.HasValue ? $"{Book.CurrentPage} / {Book.TotalPages} pages" : "";
        public string StatusText     => Book.Status.ToString();

        public RelayCommand BackCommand            { get; }
        public RelayCommand EditCommand            { get; }
        public RelayCommand ReadFromStartCommand   { get; }
        public RelayCommand ContinueReadingCommand { get; }

        public BookDetailViewModel(Book book, Action goBack, Action<Book> requestEdit, Action<Book, bool> openReader)
        {
            Book         = book;
            _goBack      = goBack;
            _requestEdit = requestEdit;
            _openReader  = openReader;

            BackCommand            = new RelayCommand(() => _goBack());
            EditCommand            = new RelayCommand(() => _requestEdit(Book));
            ReadFromStartCommand   = new RelayCommand(() => _openReader(Book, true),  () => HasFile);
            ContinueReadingCommand = new RelayCommand(() => _openReader(Book, false), () => CanContinue);

            book.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(Book.CurrentPage) or nameof(Book.BookFilePath))
                {
                    OnPropertyChanged(nameof(CanContinue));
                    OnPropertyChanged(nameof(HasFile));
                    OnPropertyChanged(nameof(ProgressPct));
                    OnPropertyChanged(nameof(ProgressText));
                }
            };
        }
    }

    public class BookEditViewModel : BaseViewModel
    {
        private readonly Book? _existing;

        private string        _title        = "";
        private string        _author       = "";
        private string        _description  = "";
        private string        _genre        = "";
        private string        _newTag       = "";
        private double        _rating       = 0;
        private string        _statusStr    = "Unread";
        private string        _totalPagesText = "";
        private byte[]?       _coverImage;
        private string?       _bookFilePath;

        public event Action<Book>? SaveCompleted;
        public event Action?       CloseRequested;

        public ObservableCollection<string> Tags { get; } = new();

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Author       { get => _author;      set => SetProperty(ref _author, value); }
        public string Description  { get => _description; set => SetProperty(ref _description, value); }
        public string Genre        { get => _genre;       set => SetProperty(ref _genre, value); }
        public string NewTag       { get => _newTag;      set => SetProperty(ref _newTag, value); }
        public double Rating       { get => _rating;      set => SetProperty(ref _rating, value); }

        public string StatusStr
        {
            get => _statusStr;
            set => SetProperty(ref _statusStr, value);
        }

        public string TotalPagesText
        {
            get => _totalPagesText;
            set => SetProperty(ref _totalPagesText, value);
        }

        public byte[]? CoverImage
        {
            get => _coverImage;
            set => SetProperty(ref _coverImage, value);
        }

        public string? BookFilePath
        {
            get => _bookFilePath;
            set { SetProperty(ref _bookFilePath, value); OnPropertyChanged(nameof(BookFileDisplay)); }
        }

        public string BookFileDisplay =>
            string.IsNullOrEmpty(BookFilePath) ? "No file selected" : Path.GetFileName(BookFilePath);

        public IReadOnlyList<string> StatusOptions { get; } = new[] { "Unread", "Reading", "Read" };

        public RelayCommand       BrowseCoverCommand  { get; }
        public RelayCommand       BrowseBookCommand   { get; }
        public RelayCommand       AddTagCommand       { get; }
        public RelayCommand<string> RemoveTagCommand  { get; }
        public RelayCommand       SaveCommand         { get; }
        public RelayCommand       CancelCommand       { get; }

        public BookEditViewModel(Book? existing = null)
        {
            _existing = existing;

            if (existing != null)
            {
                _title          = existing.Title;
                _author         = existing.Author;
                _description    = existing.Description;
                _genre          = existing.Genre ?? "";
                _rating         = existing.Rating ?? 0;
                _statusStr      = existing.Status.ToString();
                _totalPagesText = existing.TotalPages?.ToString() ?? "";
                _coverImage     = existing.CoverImage;
                _bookFilePath   = existing.BookFilePath;
                foreach (var t in existing.Tags) Tags.Add(t);
            }

            BrowseCoverCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg" };
                if (dlg.ShowDialog() == true)
                    CoverImage = File.ReadAllBytes(dlg.FileName);
            });

            BrowseBookCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog { Filter = "Markdown|*.md|All Files|*.*" };
                if (dlg.ShowDialog() == true)
                    BookFilePath = dlg.FileName;
            });

            AddTagCommand = new RelayCommand(
                () =>
                {
                    var t = NewTag.Trim();
                    if (!string.IsNullOrEmpty(t) && !Tags.Contains(t)) Tags.Add(t);
                    NewTag = "";
                },
                () => !string.IsNullOrWhiteSpace(NewTag));

            RemoveTagCommand = new RelayCommand<string>(tag => { if (tag != null) Tags.Remove(tag); });

            SaveCommand = new RelayCommand(
                () =>
                {
                    var status = _statusStr switch
                    {
                        "Reading" => ReadingStatus.Reading,
                        "Read"    => ReadingStatus.Read,
                        _         => ReadingStatus.Unread
                    };
                    int? pages = int.TryParse(_totalPagesText, out var n) && n > 0 ? n : null;

                    if (existing != null)
                    {
                        existing.Title       = Title.Trim();
                        existing.Author      = Author.Trim();
                        existing.Description = Description.Trim();
                        existing.Genre       = string.IsNullOrWhiteSpace(Genre) ? null : Genre.Trim();
                        existing.Rating      = _rating > 0 ? _rating : null;
                        existing.Status      = status;
                        existing.TotalPages  = pages;
                        existing.BookFilePath = BookFilePath;
                        existing.CoverImage  = CoverImage;
                        existing.Tags        = Tags.ToList();
                        SaveCompleted?.Invoke(existing);
                    }
                    else
                    {
                        var book = new Book
                        {
                            Title       = Title.Trim(),
                            Author      = Author.Trim(),
                            Description = Description.Trim(),
                            Genre       = string.IsNullOrWhiteSpace(Genre) ? null : Genre.Trim(),
                            Rating      = _rating > 0 ? _rating : null,
                            Status      = status,
                            TotalPages  = pages,
                            BookFilePath = BookFilePath,
                            CoverImage  = CoverImage,
                            Tags        = Tags.ToList()
                        };
                        SaveCompleted?.Invoke(book);
                    }
                    CloseRequested?.Invoke();
                },
                () => !string.IsNullOrWhiteSpace(Title));

            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
        }
    }

    public class ReaderViewModel : BaseViewModel
    {
        private readonly Book   _book;
        private readonly Action _saveRepo;
        private readonly List<(string Title, string Content)> _chapters;
        private int _index;

        public string ChapterTitle   => _chapters.Count > 0 ? _chapters[_index].Title : "";
        public string ChapterContent => _chapters.Count > 0 ? _chapters[_index].Content : "";
        public string Position       => $"Chapter {_index + 1} of {_chapters.Count}";
        public bool   CanGoPrevious  => _index > 0;
        public bool   CanGoNext      => _index < _chapters.Count - 1;

        public RelayCommand PreviousCommand { get; }
        public RelayCommand NextCommand     { get; }

        public ReaderViewModel(Book book, Action saveRepo)
        {
            _book     = book;
            _saveRepo = saveRepo;
            _chapters = ParseMarkdown(File.ReadAllText(book.BookFilePath!));
            _index    = Math.Clamp(book.CurrentPage, 0, Math.Max(0, _chapters.Count - 1));

            PreviousCommand = new RelayCommand(GoPrevious, () => CanGoPrevious);
            NextCommand     = new RelayCommand(GoNext,     () => CanGoNext);
        }

        private void GoPrevious()
        {
            if (!CanGoPrevious) return;
            _index--;
            Persist();
        }

        private void GoNext()
        {
            if (!CanGoNext) return;
            _index++;
            Persist();
        }

        private void Persist()
        {
            _book.CurrentPage = _index;
            _saveRepo();
            OnPropertyChanged(nameof(ChapterTitle));
            OnPropertyChanged(nameof(ChapterContent));
            OnPropertyChanged(nameof(Position));
            OnPropertyChanged(nameof(CanGoPrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }

        private static List<(string, string)> ParseMarkdown(string text)
        {
            var result  = new List<(string, string)>();
            var sb      = new StringBuilder();
            var title   = "Introduction";

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.StartsWith("## ") || line.StartsWith("# "))
                {
                    var body = sb.ToString().Trim();
                    if (body.Length > 0) result.Add((title, body));
                    title = line.TrimStart('#').Trim();
                    sb.Clear();
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            var last = sb.ToString().Trim();
            if (last.Length > 0) result.Add((title, last));

            return result.Count > 0
                ? result
                : new List<(string, string)> { ("Content", text) };
        }
    }

    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel      _currentPage;
        private string             _statusMessage = "Welcome to Libraria";
        private BookListViewModel? _bookListVm;

        private readonly LibraryRepository _repo = new();

        public event Action<BookEditViewModel>? OpenEditWindowRequested;
        public event Action<ReaderViewModel>?   OpenReaderWindowRequested;

        public BaseViewModel CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public int    BookCount    => _repo.Books.Count;
        public int    ReadCount    => _repo.Books.Count(b => b.Status == ReadingStatus.Read);
        public int    ReadingCount => _repo.Books.Count(b => b.Status == ReadingStatus.Reading);

        public string AvgRating
        {
            get
            {
                var rated = _repo.Books.Where(b => b.Rating.HasValue).ToList();
                return rated.Count > 0 ? $"{rated.Average(b => b.Rating!.Value):F1} ★" : "—";
            }
        }

        public string TopGenre
        {
            get
            {
                var top = _repo.Books
                    .Where(b => !string.IsNullOrEmpty(b.Genre))
                    .GroupBy(b => b.Genre)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                return top?.Key ?? "—";
            }
        }

        public RelayCommand RepositoryCreateCommand { get; }
        public RelayCommand RepositoryOpenCommand   { get; }
        public RelayCommand BookAddCommand          { get; }
        public RelayCommand BookEditCommand         { get; }
        public RelayCommand BookDeleteCommand       { get; }

        public MainViewModel()
        {
            RepositoryCreateCommand = new RelayCommand(() =>
            {
                var dlg = new SaveFileDialog
                {
                    Title      = "Create Repository",
                    Filter     = "Library Repository|*.librepo",
                    DefaultExt = ".librepo"
                };
                if (dlg.ShowDialog() == true) CreateRepository(dlg.FileName);
            });

            RepositoryOpenCommand = new RelayCommand(() =>
            {
                var dlg = new OpenFileDialog
                {
                    Title  = "Open Repository",
                    Filter = "Library Repository|*.librepo"
                };
                if (dlg.ShowDialog() == true) OpenRepository(dlg.FileName);
            });

            BookAddCommand    = new RelayCommand(() => RequestAddBook(),
                                                () => _repo.IsOpen);
            BookEditCommand   = new RelayCommand(() => { if (_bookListVm?.SelectedBook != null) RequestEditBook(_bookListVm.SelectedBook); },
                                                () => _bookListVm?.SelectedBook != null);
            BookDeleteCommand = new RelayCommand(() => { if (_bookListVm?.SelectedBook != null) RequestDeleteBook(_bookListVm.SelectedBook); },
                                                () => _bookListVm?.SelectedBook != null);

            _currentPage = new WelcomeViewModel(RepositoryCreateCommand, RepositoryOpenCommand);
            _repo.Books.CollectionChanged += (_, _) => RefreshStats();
        }

        private void CreateRepository(string path)
        {
            _repo.Create(path);
            GoToBookList();
            StatusMessage = "Repository created";
        }

        private void OpenRepository(string path)
        {
            _repo.Open(path);
            GoToBookList();
            StatusMessage = "Repository opened";
        }

        private void GoToBookList()
        {
            _bookListVm = new BookListViewModel(
                _repo.Books,
                RequestAddBook,
                RequestEditBook,
                RequestDeleteBook,
                NavigateToDetail);
            CurrentPage = _bookListVm;
            RefreshStats();
        }

        private void NavigateToDetail(Book book)
        {
            CurrentPage = new BookDetailViewModel(
                book,
                GoToBookList,
                RequestEditBook,
                (b, fromStart) =>
                {
                    if (fromStart) b.CurrentPage = 0;
                    OpenReaderWindowRequested?.Invoke(
                        new ReaderViewModel(b, () => { _repo.Save(); StatusMessage = "Reading position saved"; }));
                });
        }

        private void RequestAddBook()
        {
            var editVm = new BookEditViewModel();
            editVm.SaveCompleted += book =>
            {
                _repo.Books.Add(book);
                _repo.Save();
                StatusMessage = "Book added";
                RefreshStats();
            };
            OpenEditWindowRequested?.Invoke(editVm);
        }

        private void RequestEditBook(Book book)
        {
            var editVm = new BookEditViewModel(book);
            editVm.SaveCompleted += _ =>
            {
                _repo.Save();
                StatusMessage = "Book updated";
                RefreshStats();
            };
            OpenEditWindowRequested?.Invoke(editVm);
        }

        private void RequestDeleteBook(Book book)
        {
            var r = MessageBox.Show($"Delete \"{book.Title}\"?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _repo.Books.Remove(book);
            if (CurrentPage is BookDetailViewModel d && d.Book == book)
                GoToBookList();
            _repo.Save();
            StatusMessage = "Book removed";
            RefreshStats();
        }

        private void RefreshStats()
        {
            OnPropertyChanged(nameof(BookCount));
            OnPropertyChanged(nameof(ReadCount));
            OnPropertyChanged(nameof(ReadingCount));
            OnPropertyChanged(nameof(AvgRating));
            OnPropertyChanged(nameof(TopGenre));
        }
    }

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

    public partial class TagControl : UserControl
    {
        public static readonly DependencyProperty TagsProperty =
            DependencyProperty.Register(nameof(Tags), typeof(IEnumerable<string>), typeof(TagControl),
                new PropertyMetadata(null));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(TagControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty RemoveTagCommandProperty =
            DependencyProperty.Register(nameof(RemoveTagCommand), typeof(ICommand), typeof(TagControl),
                new PropertyMetadata(null));

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

        public TagControl()
        {
            InitializeComponent();
        }
    }

    public partial class StatisticsBarControl : UserControl
    {
        public StatisticsBarControl() => InitializeComponent();
    }

    public partial class WelcomeView : UserControl
    {
        public WelcomeView() => InitializeComponent();
    }

    public partial class BookListView : UserControl
    {
        public BookListView() => InitializeComponent();
    }

    public partial class BookDetailView : UserControl
    {
        public BookDetailView() => InitializeComponent();
    }

    public partial class BookEditWindow : Window
    {
        public BookEditWindow() => InitializeComponent();
    }

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
                FontFamily = new FontFamily("Segoe UI"),
                FontSize   = 14,
                LineHeight = 22,
                Padding    = new Thickness(20, 16, 20, 16)
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

    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm         = new MainViewModel();
            DataContext = _vm;

            _vm.OpenEditWindowRequested += editVm =>
            {
                var win = new BookEditWindow { Owner = this };
                win.DataContext = editVm;
                editVm.CloseRequested += () => win.Close();
                win.ShowDialog();
            };

            _vm.OpenReaderWindowRequested += readerVm =>
            {
                var win = new ReaderWindow { Owner = this };
                win.DataContext = readerVm;
                win.ShowDialog();
            };
        }
    }
}
