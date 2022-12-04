namespace AzwConverter
{
    public sealed class CbzState
    {
        public string Name { get; set; }

        public bool HdCover { get; set; }
        public bool SdCover { get; set; }

        public int HdImages { get; set; }
        public int SdImages { get; set; }

        public int Pages { get; set; }

        public DateTime? Checked { get; set; }

        public CbzState? Changed { get; set; }

        public string PageName()
        {
            return $"page-{Pages.ToString().PadLeft(4, '0')}.jpg";
        }
    }
}
