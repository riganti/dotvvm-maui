using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DotVVM.Maui.App
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private string routeName;
        public string RouteName
        {
            get { return routeName; }
            set 
            {
                routeName = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName!));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}