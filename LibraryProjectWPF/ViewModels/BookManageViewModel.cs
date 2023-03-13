﻿using LibraryProjectWPF.Utilities;
using LPLibrary.DataAccess.Models;
using LPLibrary.Respository.Classes;
using LPLibrary.Respository.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace LibraryProjectWPF.ViewModels
{
    internal class BookManageViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged Implementation

        #region Commands

        public ICommand SetBookSearchModeCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand AddTitleCommand { get; set; }
        public ICommand EditTitleCommand { get; set; }
        public ICommand DeleteTitleCommand { get; set; }
        public ICommand AddBookCommand { get; set; }
        public ICommand DeleteBookCommand { get; set; }

        #endregion Commands

        #region Fields

        private IBookRespository _bookRespository = new BookRespository();
        private IBookInfoRespository _bookInfoRespository = new BookInfoRespository();
        private IPublisherRespository _publisherRespository = new PublisherRespository();
        private IAuthorRespository _authorRespository = new AuthorRespository();
        private IAuthorBookRespository _authorBookRespository = new AuthorBookRespository();

        private ObservableCollection<BookManagementModel> _books;
        private BookManagementModel _selectedBook;
        private string _bookSearchString = "";

        public string BookSearchMode { get; set; }

        public string BookSearchString
        {
            get => _bookSearchString;
            set
            {
                if (_bookSearchString != value)
                {
                    _bookSearchString = value;
                    OnPropertyChanged(nameof(BookSearchString));
                }
            }
        }

        public ObservableCollection<BookManagementModel> Books
        {
            get => _books;
            set
            {
                if (_books != value)
                {
                    _books = value;
                    OnPropertyChanged(nameof(Books));
                }
            }
        }

        public BookManagementModel SelectedBook
        {
            get => _selectedBook;
            set
            {
                if (_selectedBook != value)
                {
                    _selectedBook = value;
                    OnSelectedBookChange();
                }
            }
        }

        public int SelectedBookTitleId { get; set; }
        public string SelectedBookBookId { get; set; }

        #endregion Fields

        public BookManageViewModel()
        {
            SelectedBook = new();
            InitializeBooks();
            SetBookSearchMode("Title");

            SetBookSearchModeCommand = new RelayCommand<string>(
                (mode) => true,
                (mode) => SetBookSearchMode(mode));

            SearchCommand = new RelayCommand<object>(
                (_) => true,
                (_) => SearchBook());
        }

        private void InitializeBooks()
        {
            List<Book> tempBooks = _bookRespository.GetBooks();
            List<BookInfo> tempInfo = _bookInfoRespository.GetBookInfos();
            List<Publisher> tempPublishers = _publisherRespository.GetPublishers();
            List<Author> tempAuthors = _authorRespository.GetAuthors();
            List<AuthorBook> tempAuthorBook = _authorBookRespository.GetAuthorBooks();

            List<BookManagementModel> temp = (from bookInfo in tempInfo
                                              join
                                                   publisher in tempPublishers on
                                                   bookInfo.PublisherId equals publisher.PublisherId
                                              join
                                                   authorBook in tempAuthorBook on
                                                   bookInfo.TitleId equals authorBook.TitleId
                                              join
                                                   author in tempAuthors on
                                                   authorBook.AuthorId equals author.AuthorId
                                              join
                                                   book in tempBooks on
                                                   bookInfo.TitleId equals book.TitleId
                                                   into groupedData
                                              from data in groupedData.DefaultIfEmpty()
                                              let condition = data?.Condition switch
                                              {
                                                  1 => "Good",
                                                  2 => "Damaged",
                                                  3 => "Lent",
                                                  _ => "No Data"
                                              }
                                              select new BookManagementModel()
                                              {
                                                  TitleId = bookInfo.TitleId,
                                                  BookId = data?.BookId == null ? "No book in stock" : data.BookId.ToString(),
                                                  Title = bookInfo.Title,
                                                  Author = author.AuthorName,
                                                  Publisher = publisher.PublisherName,
                                                  InStock = bookInfo.InStock,
                                                  NumberOfPages = bookInfo.NumberOfPages,
                                                  Condition = condition,
                                              }).ToList();

            Application.Current.Dispatcher.Invoke(() =>
                Books = new ObservableCollection<BookManagementModel>(temp)
            );
        }

        private void OnSelectedBookChange()
        {
            if (SelectedBook != null && SelectedBook.TitleId != 0 && !SelectedBook.BookId.IsNullOrEmpty())
            {
                SelectedBookTitleId = SelectedBook.TitleId;
                SelectedBookBookId = SelectedBook.BookId;
            }
            else
            {
                SelectedBookTitleId = 0;
                SelectedBookBookId = "";
            }
        }

        private void SetBookSearchMode(string parameter) => BookSearchMode = parameter;

        private void SearchBook()
        {
            InitializeBooks();
            if (!BookSearchString.IsNullOrEmpty())
            {
                switch (BookSearchMode)
                {
                    case "Title":
                        Application.Current.Dispatcher.Invoke(() =>
                            Books = new ObservableCollection<BookManagementModel>(Books.Where(x => x.Title!.Contains(BookSearchString, StringComparison.OrdinalIgnoreCase)))
                        );
                        break;

                    case "Author":
                        Application.Current.Dispatcher.Invoke(() =>
                            Books = new ObservableCollection<BookManagementModel>(Books.Where(x => x.Author!.Contains(BookSearchString, StringComparison.OrdinalIgnoreCase)))
                        );
                        break;
                }
            }
        }
    }
}