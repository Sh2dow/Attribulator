using FramePFX.Themes;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
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

    public class SearchSettings
    {
        [XmlAttribute("nodeText")]
        public string NodeText { get; set; }

        [XmlAttribute("nodeEnabled")]
        public bool NodeEnabled { get; set; }

        [XmlAttribute("fieldText")]
        public string FieldText { get; set; }

        [XmlAttribute("fieldEnabled")]
        public bool FieldEnabled { get; set; }

        [XmlAttribute("valueText")]
        public string ValueText { get; set; }

        [XmlAttribute("valueEnabled")]
        public bool ValueEnabled { get; set; }
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

        [XmlElement("search")]
        public SearchSettings Search { get; set; }
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
            if(this.Root.Search == null)
            {
                this.Root.Search = new SearchSettings();
            }
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(RootSettings));
            using var stream = new StreamWriter(PATH);
            serializer.Serialize(stream, this.Root);
        }
    }
}
