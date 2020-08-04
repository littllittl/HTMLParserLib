using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HTMLparserLib
{
    public class Script : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _filename;
        public string Filename
        {
            get { return _filename; }
            set { SetProperty(ref _filename, value); }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        private string _progress;
        public string Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName]string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public Script(string Filename, string Status, string Progress)
        {
            _filename = Filename;
            _status = Status;
            _progress = Progress;
        }
    }

    public class SimpleParser
    {
        readonly string _address;
        readonly IBrowsingContext _context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        IHtmlCollection<IElement> _cells;
        IDocument _document;

        public SimpleParser(string n)
        {
            _address = n;
        }

        async Task<string> GetScriptsTitle()
        {
            _document = await _context.OpenAsync(_address);

            return _document.Title.Split(' ').Last();
        }

        int GetScriptsCount()
        {
            _cells = _document.QuerySelectorAll("script");
            return _cells.Length;
        }

        async Task<IEnumerable<string>> GetScriptsTexts()
        {
            var scripts = _cells.Select(s => s.TextContent).Where(s => s.Length > 0).ToList();
            var scriptsFromUrl = _cells.Select(s => s.GetAttribute("src")).Where(s => s != null);
            foreach (var scr in scriptsFromUrl)
            {
                var script = scr.StartsWith("https://") || scr.StartsWith("http://") ? await _context.OpenAsync(scr) : await _context.OpenAsync($"https://{_document.Domain}{scr}");
                scripts.Add(script.DocumentElement.TextContent);
            }
            return scripts;
        }

        public async Task<ObservableCollection<Script>> GetScriptsNames()
        {
            var scripts = new ObservableCollection<Script>();
            var title = await GetScriptsTitle();
            var count = GetScriptsCount();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                    scripts.Add(new Script(title + i, "Найден", "Ожидание записи"));
                return scripts;
            }
            else
                throw new ArgumentNullException("По данному адресу скрипты не найдены");
        }


        public async void SaveScripts(ObservableCollection<Script> scripts)
        {
            var scriptTexts = await GetScriptsTexts();
            string writePath = @"C:\Users\littl\Desktop";
            Parallel.For(0, scripts.Count, (i, state) =>
            {       
                using (StreamWriter sw = new StreamWriter($"{writePath}\\{scripts[i].Filename}.js", false, System.Text.Encoding.Default))
                {
                    scripts[i].Progress = "Запись";
                    System.Threading.Thread.Sleep(300);
                    sw.WriteLine(scriptTexts.ElementAt(i));
                    scripts[i].Progress = "Сохранено";
                    scripts[i].Status = "Внесен";
                }
            });
        }
    }
}
