using CollectionManager;

namespace AzwConverter
{
    public class CbzItem : CollectionItem
    {
        public bool HdCover { get; set; }
        public bool SdCover { get; set; }

        public int HdImages { get; set; }
        public int SdImages { get; set; }

        public int Pages { get; set; }

        public DateTime? Checked { get; set; }

        public CbzItem? Changed { get; set; }
    }
}
