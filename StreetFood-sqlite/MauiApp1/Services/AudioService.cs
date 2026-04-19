using Microsoft.Maui.Media;
using System.Globalization;
#if ANDROID
using MauiApp1.Platforms.Android; // thêm using này để sử dụng AudioFocusService
#endif

namespace MauiApp1.Services
{
    public class AudioService
    {
        // singleton: toàn bộ app dùng chung 1 AudioService
        public static AudioService Instance { get; } = new AudioService();

        // dùng để hủy audio khi Stop()
        private CancellationTokenSource? _cts;

#if ANDROID
        private AudioFocusService? _audioFocus; //thêm  // quản lý audio focus trên Android
#endif

        // trạng thái đang phát audio hay không
        public bool IsPlaying { get; private set; }

        // user đang nghe manual (bấm nút nghe trong POI detail)
        public bool IsManualMode { get; private set; }

        // hàm đọc text bằng Text-to-Speech
        public async Task Speak(string text, bool manual = false)
        {
            // nếu đang đọc rồi thì không đọc thêm
            if (IsPlaying)
                return;

#if ANDROID
            // tạo AudioFocusService nếu chưa có
            _audioFocus ??= new AudioFocusService();

            // xin quyền audio focus (tạm dừng nhạc app khác)
            if (!_audioFocus.RequestFocus())
                return;
#endif

            // tạo token để có thể cancel khi Stop()
            _cts = new CancellationTokenSource();

            // đánh dấu đang phát audio
            IsPlaying = true;

            // nếu user bấm nghe manual
            if (manual)
                IsManualMode = true;

            try
            {
                // lấy danh sách voice của thiết bị
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                // lấy culture hiện tại của app
                var culture = CultureInfo.CurrentUICulture.Name;

                // lấy 2 ký tự đầu của ngôn ngữ
                var lang = culture.Substring(0, 2);

                // chọn voice phù hợp
                var voice =
                    locales.FirstOrDefault(l => l.Language == culture)
                    ?? locales.FirstOrDefault(l => l.Language.StartsWith(lang));

                // đọc text bằng Text-to-Speech
                await TextToSpeech.Default.SpeakAsync(
                    text,
                    new SpeechOptions
                    {
                        // voice
                        Locale = voice,

                        // volume lấy từ settings
                        Volume = SettingsService.Volume / 100.0f,

                        // tốc độ đọc
                        Rate = (float)SettingsService.SpeechSpeed
                    },
                    _cts.Token
                );
            }
            catch
            {
                // bỏ qua lỗi
            }
            finally
            {
                // kết thúc audio
                IsPlaying = false;

#if ANDROID
                _audioFocus?.AbandonFocus(); //thêm  // trả lại audio focus
#endif

                // nếu manual mode thì reset lại
                if (manual)
                    IsManualMode = false;
            }
        }

        public void Stop()
        {
            // nếu chưa có audio đang chạy thì thoát
            if (_cts == null)//thêm
                return;

            // hủy audio
            _cts?.Cancel();
            _cts = null;

#if ANDROID
            _audioFocus?.AbandonFocus(); //thêm  // trả lại audio focus
#endif

            // reset trạng thái
            IsPlaying = false;
            IsManualMode = false;
        }
    }
}