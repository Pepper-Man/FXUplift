using Bungie;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace H2ToH3EffectConverter
{
    internal class Program
    {
        private class Effect
        {
            public string[] Markers { get; set; }
            public Event[] Events { get; set; }
            public TagReferenceData LoopingSound {  get; set; }
            public float AlwaysDistance { get; set; }
            public float NeverDistance { get; set; }
        }

        private class Event
        {
            public int Flags { get; set; }
            public float SkipFraction { get; set; }
            public float[] DelayBounds { get; set; }
            public float[] DurationBounds { get; set; }
            public Part[] Parts { get; set; }
            public ParticleSystem[] ParticleSystems { get; set; }
        }

        private class Part
        {
            public int Environment {  get; set; }
            public int Mode { get; set; }
            public int Location { get; set; }
            public int Flags { get; set; }
            public TagReferenceData TagRef { get; set; }
            public float[] VelocityBounds { get; set; }
            public float VelocityConeAngle { get; set; }
            public float[] AngularVelocityBounds { get; set; }
            public float[] RadiusModifierBounds { get; set; }
        }

        private class ParticleSystem
        {

        }

        private class TagReferenceData
        {
            public string Path {  get; set; }
            public string Extension { get; set; }
        }

        static void Main(string[] args)
        {
            string h3ekPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            ManagedBlamSystem.InitializeProject(InitializationType.TagsOnly, h3ekPath);

            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.xml";
            GetDataFromXML(h2EffectPath);
        }

        static Effect GetDataFromXML(string xmlPath)
        {
            Effect effect = new Effect();

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

                // MARKERS
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
                effect.Markers = effectMarkers.ToArray();
                Console.WriteLine("Finished reading effect markers.\n");

                // EVENTS
                List<Event> eventsList = new List<Event>();
                foreach (XmlNode eventElement in effectFile.SelectNodes("//block[@name='events']/element"))
                {
                    Event eventData = new Event();
                    Console.WriteLine($"\nREADING EVENT {eventElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(eventElement)}:");

                    eventData.SkipFraction = float.Parse(eventElement.SelectSingleNode("./field[@name='skip fraction']").InnerText.Trim());
                    eventData.DelayBounds = eventElement.SelectSingleNode("./field[@name='delay bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                    eventData.DurationBounds = eventElement.SelectSingleNode("./field[@name='duration bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                    Console.WriteLine($"Skip fraction: {eventData.SkipFraction}\nDelay bounds: {string.Join(", ", eventData.DelayBounds)}\nDuration bounds: {string.Join(", ", eventData.DurationBounds)}");

                    // PARTS
                    List<Part> parts = new List<Part>();
                    foreach (XmlNode partElement in eventElement.SelectNodes("//block[@name='parts']/element"))
                    {
                        Part part = new Part();
                        Console.WriteLine($"\n\tREADING PART {partElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(partElement)}:");

                        part.Environment = int.Parse(partElement.SelectNodes("./field[@name='create in']")[0].InnerText.Trim().Substring(0, 1));
                        part.Mode = int.Parse(partElement.SelectNodes("./field[@name='create in']")[1].InnerText.Trim().Substring(0, 1));
                        part.Location = int.Parse(partElement.SelectSingleNode("./block_index[@type='location']").Attributes["index"].Value);
                        part.Flags = int.Parse(partElement.SelectSingleNode("./field[@name='flags']").InnerText.Trim());
                        part.TagRef = new TagReferenceData
                        {
                            Path = partElement.SelectSingleNode("./tag_reference[@name='type']").InnerText.Trim(),
                            Extension = partElement.SelectSingleNode("./tag_reference[@name='type']").Attributes["type"].Value
                        };
                        part.VelocityBounds = partElement.SelectSingleNode("./field[@name='velocity bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        part.VelocityConeAngle = float.Parse(partElement.SelectSingleNode("./field[@name='velocity cone angle']").InnerText.Trim());
                        part.AngularVelocityBounds = partElement.SelectSingleNode("./field[@name='angular velocity bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        part.RadiusModifierBounds = partElement.SelectSingleNode("./field[@name='radius modifier bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        Console.WriteLine($"\tEnvironment: {part.Environment}\n" +
                            $"\tMode: {part.Mode}\n" +
                            $"\tMarker index: {part.Location}\n" +
                            $"\tFlags: {part.Flags}\n" +
                            $"\tTag reference: {part.TagRef.Path}.{part.TagRef.Extension}\n" +
                            $"\tVelocity bounds: {string.Join(", ", part.VelocityBounds)}\n" +
                            $"\tVelocity cone angle: {part.VelocityConeAngle}\n" +
                            $"\tAngular velocity bounds: {string.Join(", ", part.AngularVelocityBounds)}\n" +
                            $"\tRadius modifier bounds: {string.Join(", ", part.RadiusModifierBounds)}");

                        parts.Add(part);
                    }

                    eventData.Parts = parts.ToArray();
                    eventsList.Add(eventData);
                }

                effect.Events = eventsList.ToArray();
            }

            return effect;
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
