using FoodStreet.Models;
using Microsoft.Maui.Media;
namespace FoodStreet.Services;

public class NarrationService
{
    HashSet<int> played = new();
    DateTime lastPlayedTime = DateTime.MinValue;

    bool isSpeaking = false;

    public async Task SpeakAsync(Poi poi)
    {
        // Nếu đang nói → bỏ qua
        if (isSpeaking)
            return;

        // Nếu đã phát trong 6s → bỏ qua
        if (played.Contains(poi.Id) &&
            (DateTime.Now - lastPlayedTime).TotalSeconds < 6)
            return;

        try
        {
            isSpeaking = true;

            await TextToSpeech.SpeakAsync(poi.TtsScript);

            played.Add(poi.Id);
            lastPlayedTime = DateTime.Now;
        }
        finally
        {
            isSpeaking = false;
        }


    }
}

