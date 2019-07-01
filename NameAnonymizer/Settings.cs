using System.ComponentModel;
using System.Runtime.CompilerServices;
using NameAnonymizer.Annotations;

namespace NameAnonymizer
{
    public class Settings : INotifyPropertyChanged
    {
        private int _leadingZero;
        private string _regEx;

        public Settings()
        {
            RegEx = "^(from|to)?[^:]*";
            LeadingZero = 0;
        }

        public int LeadingZero
        {
            get => _leadingZero;
            set
            {
                if (value == _leadingZero) return;
                _leadingZero = value;
                OnPropertyChanged();
            }
        }

        public string RegEx
        {
            get => _regEx;
            set
            {
                if (value == _regEx) return;
                _regEx = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}