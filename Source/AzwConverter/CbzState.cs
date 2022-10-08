﻿namespace AzwConverter
{
    public class CbzState
    {
        public string Name { get; set; }

        public bool HdCover { get; set; }
        public bool SdCover { get; set; }
        
        public int HdImages { get; set; }
        public int SdImages { get; set; }

        public int Pages { get; set; }

        public bool IsEmpty()
        {
            return HdImages == 0 && SdImages == 0;
        }
    }
}