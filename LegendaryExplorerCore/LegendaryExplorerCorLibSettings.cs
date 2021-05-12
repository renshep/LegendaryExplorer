﻿using System.ComponentModel;

namespace LegendaryExplorerCore
{
    public class LegendaryExplorerCorLibSettings : INotifyPropertyChanged
    {
        // In ME3Explorer (not this lib) the property changed event is listened to for changes and maps them into the .net framework defaults system
        public static LegendaryExplorerCorLibSettings Instance { get; set; }

        public LegendaryExplorerCorLibSettings()
        {
            Instance = this;
        }
        public bool TLKGenderIsMale { get; set; }
        public string TLKDefaultLanguage { get; set; } = "INT"; // maybe should be enum?
        public bool ParseUnknownArrayTypesAsObject { get; set; }
        public string ME1Directory { get; set; }
        public string ME2Directory { get; set; }
        public string ME3Directory { get; set; }

#pragma warning disable
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore
    }
}