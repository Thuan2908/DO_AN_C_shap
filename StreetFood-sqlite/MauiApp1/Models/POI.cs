using System.Globalization;
using SQLite;

namespace MauiApp1.Models
{
    public class POI
    {
        [PrimaryKey]
        public int Id { get; set; }

        // ===== Name =====
        public string Name_vi { get; set; } = "";
        public string Name_en { get; set; } = "";
        public string Name_ja { get; set; } = "";

        // ===== Location =====
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; } = "";
        public double Rating { get; set; }   // ⭐ số sao

        // ===== Address =====
        public string Address_vi { get; set; } = "";
        public string Address_en { get; set; } = "";
        public string Address_ja { get; set; } = "";
        // ===== Description =====
        public string Description_vi { get; set; } = "";
        public string Description_en { get; set; } = "";
        public string Description_ja { get; set; } = "";

        // ===== Detail =====
        public string Detail_vi { get; set; } = "";
        public string Detail_en { get; set; } = "";
        public string Detail_ja { get; set; } = "";

        // ===== Content =====
        public string Content_vi { get; set; } = "";
        public string Content_en { get; set; } = "";
        public string Content_ja { get; set; } = "";

        public double Radius { get; set; } = 10;
        public double NearRadius { get; set; } = 30;
        public int Priority { get; set; } = 1;

        // ===== Runtime =====
        [Ignore]
        public bool IsInside { get; set; }

        [Ignore]
        public DateTime LastTriggered { get; set; } = DateTime.MinValue;

        // ===== Property cho UI =====

        [Ignore]
        public string Name => GetName();

        [Ignore]
        public string Description => GetDescription();

        [Ignore]
        public string Content => GetContent();

        [Ignore]
        public string Detail => GetDetail();

        [Ignore]
        public string Address => GetAddress();

        // ===== Get Language =====

        public string GetName()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return lang switch
            {
                "en" => Name_en,
                "ja" => Name_ja,
                _ => Name_vi
            };
        }

        public string GetDescription()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return lang switch
            {
                "en" => Description_en,
                "ja" => Description_ja,
                _ => Description_vi
            };
        }

        public string GetContent()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return lang switch
            {
                "en" => Content_en,
                "ja" => Content_ja,
                _ => Content_vi
            };
        }

        public string GetDetail()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return lang switch
            {
                "en" => Detail_en,
                "ja" => Detail_ja,
                _ => Detail_vi
            };
        }

        public string GetAddress()
        {
            var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            return lang switch
            {
                "en" => Address_en,
                "ja" => Address_ja,
                _ => Address_vi
            };
        }

        [Ignore]
        public string Language
        {
            get
            {
                var lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                return lang switch
                {
                    "en" => "English",
                    "ja" => "日本語",
                    _ => "Tiếng Việt"
                };
            }
        }

        [Ignore]
        
        public string StarDisplay
        {
            get
            {
                int fullStars = (int)Math.Floor(Rating);
                int emptyStars = 5 - fullStars;

                return new string('⭐', fullStars) + new string('☆', emptyStars);
            }
        }
    }
}