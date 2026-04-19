using System.Text.Json.Serialization;

namespace MauiApp1.Models
{
    /// <summary>
    /// DTO ánh xạ document Firestore → POI model.
    /// Firestore dùng camelCase, model dùng PascalCase → cần JsonPropertyName.
    /// </summary>
    public class FirebasePoiDto
    {
        // Firestore document ID (set thủ công sau khi fetch)
        [JsonIgnore]
        public string FirestoreId { get; set; } = "";

        [JsonPropertyName("name_vi")] public string Name_vi { get; set; } = "";
        [JsonPropertyName("name_en")] public string Name_en { get; set; } = "";
        [JsonPropertyName("name_ja")] public string Name_ja { get; set; } = "";

        [JsonPropertyName("address_vi")] public string Address_vi { get; set; } = "";
        [JsonPropertyName("address_en")] public string Address_en { get; set; } = "";
        [JsonPropertyName("address_ja")] public string Address_ja { get; set; } = "";

        [JsonPropertyName("description_vi")] public string Description_vi { get; set; } = "";
        [JsonPropertyName("description_en")] public string Description_en { get; set; } = "";
        [JsonPropertyName("description_ja")] public string Description_ja { get; set; } = "";

        [JsonPropertyName("detail_vi")] public string Detail_vi { get; set; } = "";
        [JsonPropertyName("detail_en")] public string Detail_en { get; set; } = "";
        [JsonPropertyName("detail_ja")] public string Detail_ja { get; set; } = "";

        [JsonPropertyName("content_vi")] public string Content_vi { get; set; } = "";
        [JsonPropertyName("content_en")] public string Content_en { get; set; } = "";
        [JsonPropertyName("content_ja")] public string Content_ja { get; set; } = "";

        [JsonPropertyName("scripts")]
        public FirebaseScripts? Scripts { get; set; }

        [JsonPropertyName("lat")] public double Latitude { get; set; }
        [JsonPropertyName("lng")] public double Longitude { get; set; }
        [JsonPropertyName("radius")] public double Radius { get; set; } = 10;
        [JsonPropertyName("nearRadius")] public double NearRadius { get; set; } = 30;
        [JsonPropertyName("priority")] public int Priority { get; set; } = 1;
        [JsonPropertyName("rating")] public double Rating { get; set; } = 0;
        [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "active";
        [JsonPropertyName("icon")] public string Icon { get; set; } = "📍";

        // ── Chuyển đổi sang POI model hiện tại ──
        public POI ToPoi(int localId)
        {
            return new POI
            {
                Id = localId,
                Name_vi = Name_vi,
                Name_en = Name_en,
                Name_ja = Name_ja,
                Address_vi = Address_vi,
                Address_en = Address_en,
                Address_ja = Address_ja,
                Description_vi = Description_vi,
                Description_en = Description_en,
                Description_ja = Description_ja,
                Detail_vi = Detail_vi,
                Detail_en = Detail_en,
                Detail_ja = Detail_ja,
                // Scripts từ CMS ưu tiên hơn Content cứng
                Content_vi = Scripts?.Vi ?? Content_vi,
                Content_en = Scripts?.En ?? Content_en,
                Content_ja = Scripts?.Ja ?? Content_ja,
                Latitude = Latitude,
                Longitude = Longitude,
                Radius = Radius > 0 ? Radius : 10,
                NearRadius = NearRadius,
                Priority = Priority,
                Rating = Rating,
                ImageUrl = ImageUrl,
            };
        }
    }

    public class FirebaseScripts
    {
        [JsonPropertyName("vi")] public string Vi { get; set; } = "";
        [JsonPropertyName("en")] public string En { get; set; } = "";
        [JsonPropertyName("ja")] public string Ja { get; set; } = "";
    }
}
