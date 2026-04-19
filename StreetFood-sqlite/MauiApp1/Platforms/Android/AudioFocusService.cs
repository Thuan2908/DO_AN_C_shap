using global::Android.Content;
using global::Android.Media;
using MauiApp1.Services;

namespace MauiApp1.Platforms.Android
{
    public class AudioFocusService : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly AudioManager? _audioManager;
        private AudioFocusRequestClass? _focusRequest;

        public AudioFocusService()
        {
            var context = global::Android.App.Application.Context;
            _audioManager = context.GetSystemService(Context.AudioService) as AudioManager;
        }

        public bool RequestFocus()
        {
            if (_audioManager == null)
                return false;

            var attributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.AssistanceAccessibility)
                .SetContentType(AudioContentType.Speech)
                .Build();

            _focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.GainTransient)
                 .SetAudioAttributes(attributes)
                .SetOnAudioFocusChangeListener(this)
                .SetAcceptsDelayedFocusGain(true)
                .Build();

            var result = _audioManager.RequestAudioFocus(_focusRequest);

            return result == AudioFocusRequest.Granted;
        }

        public void AbandonFocus()
        {
            if (_audioManager == null || _focusRequest == null)
                return;

            _audioManager.AbandonAudioFocusRequest(_focusRequest);
        }

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Loss:
                case AudioFocus.LossTransient:
                case AudioFocus.LossTransientCanDuck:

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AudioService.Instance.Stop();
                    });

                    break;
            }
        }
    }
}