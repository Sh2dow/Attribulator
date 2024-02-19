using FramePFX.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Attribulator.UI
{
    public class GameSettings
    {
        [XmlAttribute("header")]
        public string Header { get; set; }

        [XmlAttribute("profile")]
        public string Profile { get; set; }

        [XmlElement("exePath")]
        public string ExePath { get; set; }

        [XmlElement("selected")]
        public bool Selected { get; set; }
    }

    [XmlRoot("settings")]
    public class RootSettings
    {
        [XmlArray("games")]
        [XmlArrayItem("game")]
        public List<GameSettings> Games { get; set; }

        [XmlIgnore]
        public GameSettings SelectedGame => Games.FirstOrDefault(x => x.Selected);

        [XmlElement("openCollectionByDoubleClick")]
        public bool OpenCollectionByDoubleClick { get; set; }

        [XmlElement("showWelcomeTab")]
        public bool ShowWelcomeTab { get; set; }

        [XmlElement("script")]
        public string Srcipt { get; set; }

        [XmlElement("theme")]
        public ThemeType Theme { get; set; }
    }

    public class Settings
    {
        private static string PATH = @"settings.xml";

        public RootSettings Root { get; private set; }

        public Settings()
        {
            var serializer = new XmlSerializer(typeof(RootSettings));
            using var stream = new StreamReader(PATH);
            this.Root = serializer.Deserialize(stream) as RootSettings;
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(RootSettings));
            using var stream = new StreamWriter(PATH);
            serializer.Serialize(stream, this.Root);
        }
    }
}
