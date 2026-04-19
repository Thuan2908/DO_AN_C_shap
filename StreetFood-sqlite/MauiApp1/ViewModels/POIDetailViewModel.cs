using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiApp1.Models;
using MauiApp1.Resources.Languages;
using MauiApp1.Services;

namespace MauiApp1.ViewModels
{
    // ViewModel cho trang chi tiet POI
    public class POIDetailViewModel : INotifyPropertyChanged
    {
        // POI dang duoc hien thi
        private POI? _selectedPOI;

        // trang thai dang doc audio hay khong
        private bool _isSpeaking;

        // dung AudioService chung (singleton)
        private readonly AudioService _audioService = AudioService.Instance;

        // POI duoc truyen tu trang truoc (Map/List)
        public POI? SelectedPOI
        {
            get => _selectedPOI;
            set
            {
                _selectedPOI = value;
                OnPropertyChanged(); // cap nhat UI
            }
        }

        // trang thai dang doc audio
        public bool IsSpeaking
        {
            get => _isSpeaking;
            set
            {
                _isSpeaking = value;
                OnPropertyChanged(); // cap nhat UI
                OnPropertyChanged(nameof(SpeakButtonText)); // cap nhat text cua nut
            }
        }

        // text cua nut nghe audio
        public string SpeakButtonText =>
            IsSpeaking ? AppResources.Stop : AppResources.ListenNow;

        // command khi bam nut nghe audio
        public ICommand SpeakCommand { get; }

        // command khi bam nut dong trang
        public ICommand CloseCommand { get; }

        // constructor
        public POIDetailViewModel()
        {
            // gan command
            SpeakCommand = new Command(async () => await OnSpeak());
            CloseCommand = new Command(async () => await OnClose());
        }

        // xu ly khi bam nut nghe audio
        private async Task OnSpeak()
        {
            if (SelectedPOI == null)
                return;

            // neu dang phat audio thi dung
            if (_audioService.IsPlaying)
            {
                Stop();
                return;
            }

            // bat dau doc
            IsSpeaking = true;

            // doc noi dung chi tiet POI
            await _audioService.Speak(SelectedPOI.Detail, true);

            // doc xong
            IsSpeaking = false;
        }

        // dung audio
        public void Stop()
        {
            _audioService.Stop();
            IsSpeaking = false;
        }

        // dong trang chi tiet
        private async Task OnClose()
        {
            Stop(); // dung audio truoc khi dong
            await Shell.Current.GoToAsync(".."); // quay lai trang truoc
        }

        // su kien thay doi property (de update UI)
        public event PropertyChangedEventHandler? PropertyChanged;

        // ham thong bao property thay doi
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}