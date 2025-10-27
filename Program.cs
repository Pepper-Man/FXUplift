using Bungie;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string h3ekPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);

            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.xml";
            GetDataFromXML(h2EffectPath);
        }

        static void GetDataFromXML(string xmlPath)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                DtdProcessing = DtdProcessing.Ignore
            };

            string xmlData = CleanXML(xmlPath);
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData), settings))
            {
                XmlDocument effectFile = new XmlDocument();
                effectFile.Load(reader);

                List<string> effectMarkers = new List<string>();

                Console.WriteLine("Reading markers:");
                foreach (XmlNode markerElement in effectFile.SelectNodes("//block[@name='locations']/element"))
                {
                    XmlNode name = markerElement.SelectSingleNode("./field[@name='marker name']");
                    if (name != null)
                    {
                        string markerName = name.InnerText.Trim();
                        Console.WriteLine(markerName);
                        effectMarkers.Add(markerName);
                    }
                    
                }

                Console.WriteLine("Finished reading effect markers.\n");
            }
        }

        static string CleanXML(string filePath)
        {
            try
            {
                // Read as text
                string xmlContent = File.ReadAllText(filePath);

                // Clean invalid characters
                string cleanedXML = RemoveInvalidXmlCharacters(xmlContent);

                Console.WriteLine("XML file cleaned successfully.", false);

                return cleanedXML;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"XML clean/load error: {ex}", false);
                return null;
            }

            bool IsXmlChar(char ch)
            {
                // Check if the character is a valid XML character
                return ch == 0x9 || ch == 0xA || ch == 0xD ||
                   (ch >= 0x20 && ch <= 0xD7FF) ||
                   (ch >= 0xE000 && ch <= 0xFFFD) ||
                   (ch >= 0x10000 && ch <= 0x10FFFF);
            }

            string RemoveInvalidXmlCharacters(string input)
            {
                // Replace invalid characters with an empty string
                StringBuilder cleanedString = new StringBuilder();
                foreach (char ch in input)
                {
                    if (IsXmlChar(ch))
                    {
                        cleanedString.Append(ch);
                    }
                }

                return cleanedString.ToString();
            }
        }
    }
}
