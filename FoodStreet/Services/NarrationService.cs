using FoodStreet.Models;

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

        // Nếu đã phát trong 2 phút → bỏ qua
        if (played.Contains(poi.Id) &&
            (DateTime.Now - lastPlayedTime).TotalMinutes < 2)
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
