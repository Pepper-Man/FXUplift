using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace H2ToH3EffectConverter
{
    internal class ReadFromH2
    {
        public class Effect
        {
            public string[] Markers { get; set; }
            public Event[] Events { get; set; }
            public TagReferenceData LoopingSound { get; set; }
            public float AlwaysDistance { get; set; }
            public float NeverDistance { get; set; }
        }

        public class Event
        {
            public int Flags { get; set; }
            public float SkipFraction { get; set; }
            public float[] DelayBounds { get; set; }
            public float[] DurationBounds { get; set; }
            public Part[] Parts { get; set; }
            public ParticleSystem[] ParticleSystems { get; set; }
        }

        public class Part
        {
            public int Environment { get; set; }
            public int Mode { get; set; }
            public int Location { get; set; }
            public int Flags { get; set; }
            public TagReferenceData TagRef { get; set; }
            public float[] VelocityBounds { get; set; }
            public float VelocityConeAngle { get; set; }
            public float[] AngularVelocityBounds { get; set; }
            public float[] RadiusModifierBounds { get; set; }
        }

        public class ParticleSystem
        {
            public string Name { get; set; }
            public TagReferenceData ParticleRef { get; set; }
            public int Location { get; set; }
            public int CoordSystem { get; set; }
            public int Environment { get; set; }
            public int Mode { get; set; }
            public int CameraMode { get; set; }
            public int SortBias { get; set; }
            public int Flags { get; set; }
            public float LodIn { get; set; }
            public float LodFInDelta { get; set; }
            public float LodOut { get; set; }
            public float LodFOutDelta { get; set; }
            public Emitter[] Emitters { get; set; }
        }

        public class Emitter
        {
            public TagReferenceData PhysicsRef { get; set; }
            public Halo2Function EmissionRate { get; set; }
            public Halo2Function Lifespan { get; set; }
            public Halo2Function Velocity { get; set; }
            public Halo2Function AngularVelocity { get; set; }
            public Halo2Function Size { get; set; }
            public Halo2Function Tint { get; set; }
            public Halo2Function Alpha { get; set; }
            public int EmissionShape { get; set; }
            public Halo2Function Radius { get; set; }
            public Halo2Function Angle { get; set; }
            public float[] TransOffset { get; set; }
            public float[] RelativeDir { get; set; }
        }

        public class Halo2Function
        {
            public int InputVar { get; set; }
            public int RangeVar { get; set; }
            public int OutputMod { get; set; }
            public int OutputModInput { get; set; }
            public int[] RawData { get; set; }
            public int FuncType { get; set; }
            public bool useRange { get; set; }
            public float baseVal { get; set; }
            public float rangeVal { get; set; }
        }

        public class TagReferenceData
        {
            public string Path { get; set; }
            public string Extension { get; set; }
        }

        public static Effect GetDataFromXML(string xmlPath)
        {
            Effect effect = new Effect();

            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                DtdProcessing = DtdProcessing.Ignore
            };

            string xmlData = Utils.CleanXML(xmlPath);
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

                    // PARTICLE SYSTEMS
                    List<ParticleSystem> particleSystems = new List<ParticleSystem>();
                    foreach (XmlNode particleSysElement in eventElement.SelectNodes("//block[@name='particle systems']/element"))
                    {
                        ParticleSystem particleSystem = new ParticleSystem();
                        Console.WriteLine($"\n\tREADING PARTICLE SYSTEM {particleSysElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(particleSysElement)}:");

                        particleSystem.Name = particleSysElement.Attributes["name"].Value;
                        particleSystem.ParticleRef = new TagReferenceData
                        {
                            Path = particleSysElement.SelectSingleNode("./tag_reference[@name='particle']").InnerText.Trim(),
                            Extension = particleSysElement.SelectSingleNode("./tag_reference[@name='particle']").Attributes["type"].Value
                        };
                        particleSystem.Location = int.Parse(particleSysElement.SelectSingleNode("./block_index[@type='location']").Attributes["index"].Value);
                        particleSystem.CoordSystem = int.Parse(particleSysElement.SelectNodes("./field[@name='coordinate system']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Environment = int.Parse(particleSysElement.SelectNodes("./field[@name='environment']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Mode = int.Parse(particleSysElement.SelectNodes("./field[@name='disposition']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.CameraMode = int.Parse(particleSysElement.SelectNodes("./field[@name='camera mode']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.SortBias = int.Parse(particleSysElement.SelectNodes("./field[@name='sort bias']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Flags = int.Parse(particleSysElement.SelectSingleNode("./field[@name='flags']").InnerText.Trim());
                        particleSystem.LodIn = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD in distance']").InnerText.Trim());
                        particleSystem.LodFInDelta = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD feather in delta']").InnerText.Trim());
                        particleSystem.LodOut = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD out distance']").InnerText.Trim());
                        particleSystem.LodFOutDelta = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD feather out delta']").InnerText.Trim());

                        Console.WriteLine($"\tName: {particleSystem.Name}\n" +
                            $"\tParticle reference: {particleSystem.ParticleRef.Path}.{particleSystem.ParticleRef.Extension}\n" +
                            $"\tMarker index: {particleSystem.Location}\n" +
                            $"\tCoordinate system: {particleSystem.CoordSystem}\n" +
                            $"\tEnvironment: {particleSystem.Environment}\n" +
                            $"\tDisposition/Mode: {particleSystem.Mode}\n" +
                            $"\tCamera mode: {particleSystem.CameraMode}\n" +
                            $"\tSort bias: {particleSystem.SortBias}\n" +
                            $"\tFlags: {particleSystem.Flags}\n" +
                            $"\tLOD in distance: {particleSystem.LodIn}\n" +
                            $"\tLOD feather in delta: {particleSystem.LodFInDelta}\n" +
                            $"\tLOD out distance: {particleSystem.LodOut}\n" +
                            $"\tLOD feather out delta: {particleSystem.LodFOutDelta}");

                        // EMITTERS
                        List<Emitter> emitters = new List<Emitter>();
                        foreach (XmlNode emitterElement in particleSysElement.SelectNodes(".//block[@name='emitters']/element"))
                        {
                            Emitter emitter = new Emitter();
                            Console.WriteLine($"\n\t\tREADING EMITTER {emitterElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(emitterElement)}:");

                            if (emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").InnerText.Trim() != "")
                            {
                                emitter.PhysicsRef = new TagReferenceData
                                {
                                    Path = emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").InnerText.Trim(),
                                    Extension = emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").Attributes["type"].Value
                                };
                            }

                            XmlNodeList functionFieldGroups = emitterElement.SelectNodes("./field[@name='Input Variable']");
                            List<Halo2Function> functions = new List<Halo2Function>();

                            foreach (XmlNode inputVarField in functionFieldGroups)
                            {
                                Halo2Function func = new Halo2Function();

                                func.InputVar = Utils.funcInputMapping[inputVarField.InnerText.Trim()];
                                func.RangeVar = Utils.funcInputMapping[inputVarField.SelectSingleNode("following-sibling::field[@name='Range Variable']")?.InnerText.Trim()];
                                func.OutputMod = int.Parse(inputVarField.SelectSingleNode("following-sibling::field[@name='Output Modifier']")?.InnerText.Trim().Substring(0, 1));
                                func.OutputModInput = Utils.funcInputMapping[inputVarField.SelectSingleNode("following-sibling::field[@name='Output Modifier Input']")?.InnerText.Trim()];

                                // Read data block
                                XmlNode dataBlock = inputVarField.SelectSingleNode("following-sibling::block[@name='data']");
                                func.RawData = dataBlock.SelectNodes("./element/field[@name='Value']").Cast<XmlNode>().Select(n => int.Parse(n.InnerText.Trim())).ToArray();

                                // Convert raw data to signed bytes
                                sbyte[] signed = func.RawData.Select(i => (sbyte)i).ToArray();
                                // Then read some values
                                func.baseVal = Utils.ReadHalo2PackedFloat(signed, 6);
                                func.rangeVal = Utils.ReadHalo2PackedFloat(signed, 10);

                                // First integer is function type index
                                func.FuncType = func.RawData[0];

                                // Second int is use range, 0=off 1=on
                                func.useRange = func.RawData[1] != 0;

                                functions.Add(func);
                            }

                            // Same order as defined in the H2 tag
                            emitter.EmissionRate = functions[0];
                            emitter.Lifespan = functions[1];
                            emitter.Velocity = functions[2];
                            emitter.AngularVelocity = functions[3];
                            emitter.Size = functions[4];
                            emitter.Tint = functions[5];
                            emitter.Alpha = functions[6];
                            emitter.Radius = functions[7];
                            emitter.Angle = functions[8];

                            Console.WriteLine($"\t\tPhysics reference: {(emitter.PhysicsRef != null ? $"{emitter.PhysicsRef.Path}.{emitter.PhysicsRef.Extension}" : "<none>")}\n" +
                                $"\t\tEmissionRate: FuncType={emitter.EmissionRate.FuncType}, UseRange={emitter.EmissionRate.useRange}, BaseVal={emitter.EmissionRate.baseVal}, RangeVal={emitter.EmissionRate.rangeVal}\n" +
                                $"\t\tLifespan: FuncType={emitter.Lifespan.FuncType}, UseRange={emitter.Lifespan.useRange}, BaseVal={emitter.Lifespan.baseVal}, RangeVal={emitter.Lifespan.rangeVal}\n" +
                                $"\t\tVelocity: FuncType={emitter.Velocity.FuncType}, UseRange={emitter.Velocity.useRange}, BaseVal={emitter.Velocity.baseVal}, RangeVal={emitter.Velocity.rangeVal}\n" +
                                $"\t\tAngularVelocity: FuncType={emitter.AngularVelocity.FuncType}, UseRange={emitter.AngularVelocity.useRange}, BaseVal={emitter.AngularVelocity.baseVal}, RangeVal={emitter.AngularVelocity.rangeVal}\n" +
                                $"\t\tSize: FuncType={emitter.Size.FuncType}, UseRange={emitter.Size.useRange}, BaseVal={emitter.Size.baseVal}, RangeVal={emitter.Size.rangeVal}\n" +
                                $"\t\tTint: FuncType={emitter.Tint.FuncType}, UseRange={emitter.Tint.useRange}, BaseVal={emitter.Tint.baseVal}, RangeVal={emitter.Tint.rangeVal}\n" +
                                $"\t\tAlpha: FuncType={emitter.Alpha.FuncType}, UseRange={emitter.Alpha.useRange}, BaseVal={emitter.Alpha.baseVal}, RangeVal={emitter.Alpha.rangeVal}\n" +
                                $"\t\tRadius: FuncType={emitter.Radius.FuncType}, UseRange={emitter.Radius.useRange}, BaseVal={emitter.Radius.baseVal}, RangeVal={emitter.Radius.rangeVal}\n" +
                                $"\t\tAngle: FuncType={emitter.Angle.FuncType}, UseRange={emitter.Angle.useRange}, BaseVal={emitter.Angle.baseVal}, RangeVal={emitter.Angle.rangeVal}");

                            emitters.Add(emitter);
                        }
                        particleSystem.Emitters = emitters.ToArray();

                        particleSystems.Add(particleSystem);
                    }
                    eventData.ParticleSystems = particleSystems.ToArray();

                    eventsList.Add(eventData);
                }

                effect.Events = eventsList.ToArray();
            }

            return effect;
        }
    }
}
