using Bungie.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using static H2ToH3EffectConverter.ReadFromH2;

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
            public uint Flags { get; set; }
            public TagReferenceData TagRef { get; set; }
            public float[] VelocityBounds { get; set; }
            public float VelocityConeAngle { get; set; }
            public float[] AngularVelocityBounds { get; set; }
            public float[] RadiusModifierBounds { get; set; }
            public uint AScalesVals { get; set; }
            public uint BScalesVals { get; set; }
        }

        public class ParticleSystem
        {
            public TagReferenceData ParticleRef { get; set; }
            public Particle Particle { get; set; }
            public int Location { get; set; }
            public int CoordSystem { get; set; }
            public int Environment { get; set; }
            public int Mode { get; set; }
            public int CameraMode { get; set; }
            public int SortBias { get; set; }
            public uint Flags { get; set; }
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
            public ParticlePhysics ParticlePhys { get; set; }
        }

        public class ParticlePhysics
        {
            public uint Flags { get; set; }
            public Halo2Function Gravity { get; set; }
            public Halo2Function AirFriction { get; set; }
            public Halo2Function RotFriction { get; set; }
            public Halo2Function CollFriction { get; set; }
            public Halo2Function CollElasticity { get; set; }
        }

        public class Halo2Function
        {
            public int InputVar { get; set; }
            public int RangeVar { get; set; }
            public int OutputMod { get; set; }
            public int OutputModInput { get; set; }
            public int[] RawData { get; set; }
            public int FuncType { get; set; }
            public bool UseRange { get; set; }
            public float BaseVal { get; set; }
            public float RangeVal { get; set; }
            public int ColourType { get; set; }
            public GameColor Colour1 { get; set; }
            public GameColor Colour2 { get; set; }
            public GameColor Colour3 { get; set; }
            public GameColor Colour4 { get; set; }
            public LinearCurvePoints CurvePoints { get; set; }
            public SplineCurvePoints SplineCurvePoints { get; set; }
        }

        public class LinearCurvePoints
        {
            public float BaseY0;
            public float? BaseX1;
            public float BaseY1;
            public float? BaseX2;
            public float? BaseY2;
            public float? BaseY3;

            public float RangeY0;
            public float? RangeX1;
            public float RangeY1;
            public float? RangeX2;
            public float? RangeY2;
            public float? RangeY3;

            public bool IsLinearKey => BaseY2.HasValue && BaseY3.HasValue;
        }

        public class SplineCurvePoints
        {
            public float BaseY0;
            public float BaseY1;

            public float BaseHandle1X;
            public float BaseHandle1Y;
            public float BaseHandle2X;
            public float BaseHandle2Y;

            public float RangeY0;
            public float RangeY1;

            public float RangeHandle1X;
            public float RangeHandle1Y;
            public float RangeHandle2X;
            public float RangeHandle2Y;

            public bool IsSpline1;
        }

        public class TagReferenceData
        {
            public string Path { get; set; }
            public string Extension { get; set; }
        }

        public class Particle
        {
            public bool AlreadyExists { get; set; }
            public string[] Flags { get; set; }
            public int Billboard { get; set; }
            public int FirstSeqIndex { get; set; }
            public int SeqCount { get; set; }
            public string ShaderTemplate { get; set; }
            public TagReferenceData Bitmap { get; set; }
            public Halo2Function Colour { get; set; }
            public Halo2Function Alpha { get; set; }
            public Halo2Function Scale { get; set; }
            public Halo2Function Rotation { get; set; }
            public Halo2Function FrameIndex { get; set; }
        }

        public static Effect GetEffectDataFromXML(string xmlPath, string h2ekPath, string h3ekPath)
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
                Console.WriteLine("MARKERS:");
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
                    foreach (XmlNode partElement in eventElement.SelectNodes(".//block[@name='parts']/element"))
                    {
                        Part part = new Part();
                        Console.WriteLine($"\n\tREADING PART {partElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(partElement)}:");

                        part.Environment = int.Parse(partElement.SelectNodes("./field[@name='create in']")[0].InnerText.Trim().Substring(0, 1));
                        part.Mode = int.Parse(partElement.SelectNodes("./field[@name='create in']")[1].InnerText.Trim().Substring(0, 1));
                        part.Location = int.Parse(partElement.SelectSingleNode("./block_index[@type='location']").Attributes["index"].Value);
                        part.Flags = uint.Parse(partElement.SelectSingleNode("./field[@name='flags']").InnerText.Trim());
                        part.TagRef = new TagReferenceData
                        {
                            Path = partElement.SelectSingleNode("./tag_reference[@name='type']").InnerText.Trim(),
                            Extension = partElement.SelectSingleNode("./tag_reference[@name='type']").Attributes["type"].Value
                        };
                        part.VelocityBounds = partElement.SelectSingleNode("./field[@name='velocity bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        part.VelocityConeAngle = float.Parse(partElement.SelectSingleNode("./field[@name='velocity cone angle']").InnerText.Trim());
                        part.AngularVelocityBounds = partElement.SelectSingleNode("./field[@name='angular velocity bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        part.RadiusModifierBounds = partElement.SelectSingleNode("./field[@name='radius modifier bounds']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                        part.AScalesVals = uint.Parse(partElement.SelectSingleNode("./field[@name='A scales values']").InnerText.Trim());
                        part.BScalesVals = uint.Parse(partElement.SelectSingleNode("./field[@name='B scales values']").InnerText.Trim());

                        Utils.PrintObject(part, 1);

                        parts.Add(part);
                    }
                    eventData.Parts = parts.ToArray();

                    // PARTICLE SYSTEMS
                    List<ParticleSystem> particleSystems = new List<ParticleSystem>();
                    foreach (XmlNode particleSysElement in eventElement.SelectNodes(".//block[@name='particle systems']/element"))
                    {
                        ParticleSystem particleSystem = new ParticleSystem();
                        Console.WriteLine($"\n\tREADING PARTICLE SYSTEM {particleSysElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(particleSysElement)}:");

                        particleSystem.ParticleRef = new TagReferenceData
                        {
                            Path = particleSysElement.SelectSingleNode("./tag_reference[@name='particle']").InnerText.Trim(),
                            Extension = particleSysElement.SelectSingleNode("./tag_reference[@name='particle']").Attributes["type"].Value
                        };
                        string particleH2TagPath = Path.ChangeExtension(Path.Combine(h2ekPath + "\\tags\\" + particleSystem.ParticleRef.Path), "particle");
                        string particleH3TagPath = Path.ChangeExtension(Path.Combine(h3ekPath + "\\tags\\halo_2\\" + particleSystem.ParticleRef.Path), "particle");

                        string particleXmlPath = Utils.ExtractTagXML(particleH2TagPath, "particle", h2ekPath);
                        particleSystem.Particle = GetParticleDataFromXML(particleXmlPath, h2ekPath);

                        // Create particle tag from H2 data if it doesn't exist in H3 yet
                        if (File.Exists(particleH3TagPath))
                        {
                            particleSystem.Particle.AlreadyExists = true;
                        }

                        particleSystem.Location = int.Parse(particleSysElement.SelectSingleNode("./block_index[@type='location']").Attributes["index"].Value);
                        particleSystem.CoordSystem = int.Parse(particleSysElement.SelectNodes("./field[@name='coordinate system']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Environment = int.Parse(particleSysElement.SelectNodes("./field[@name='environment']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Mode = int.Parse(particleSysElement.SelectNodes("./field[@name='disposition']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.CameraMode = int.Parse(particleSysElement.SelectNodes("./field[@name='camera mode']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.SortBias = int.Parse(particleSysElement.SelectNodes("./field[@name='sort bias']")[0].InnerText.Trim().Substring(0, 1));
                        particleSystem.Flags = uint.Parse(particleSysElement.SelectSingleNode("./field[@name='flags']").InnerText.Trim());
                        particleSystem.LodIn = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD in distance']").InnerText.Trim());
                        particleSystem.LodFInDelta = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD feather in delta']").InnerText.Trim());
                        particleSystem.LodOut = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD out distance']").InnerText.Trim());
                        particleSystem.LodFOutDelta = float.Parse(particleSysElement.SelectSingleNode("./field[@name='LOD feather out delta']").InnerText.Trim());

                        Utils.PrintObject(particleSystem, 1);

                        // EMITTERS
                        List<Emitter> emitters = new List<Emitter>();
                        foreach (XmlNode emitterElement in particleSysElement.SelectNodes(".//block[@name='emitters']/element"))
                        {
                            Emitter emitter = new Emitter();
                            Console.WriteLine($"\n\t\tREADING EMITTER {emitterElement.ParentNode.ChildNodes.Cast<XmlNode>().ToList().IndexOf(emitterElement)}:");

                            if (emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").InnerText.Trim() != "")
                            {
                                // PARTICLE PHYSICS
                                ParticlePhysics pPhysics = new ParticlePhysics();

                                emitter.PhysicsRef = new TagReferenceData
                                {
                                    Path = emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").InnerText.Trim(),
                                    Extension = emitterElement.SelectSingleNode("./tag_reference[@name='particle physics']").Attributes["type"].Value
                                };

                                string physicsRefFullPath = Path.ChangeExtension(Path.Combine(h2ekPath, "tags", emitter.PhysicsRef.Path), "particle_physics");
                                string particlePhysXml = Utils.ExtractTagXML(physicsRefFullPath, "particle physics", h2ekPath);

                                string pPhysXmlData = Utils.CleanXML(particlePhysXml);
                                using (XmlReader pPhysReader = XmlReader.Create(new StringReader(pPhysXmlData), settings))
                                {
                                    XmlDocument pPhysFile = new XmlDocument();
                                    pPhysFile.Load(pPhysReader);

                                    pPhysics.Flags = uint.Parse(pPhysFile.SelectSingleNode("//field[@name='flags']").InnerText.Trim().Substring(0, 1));

                                    XmlNodeList movementFunctionGroups = pPhysFile.SelectNodes("//block[@name='movements']/element/block[@name='parameters']/element/field[@name='Input Variable']");
                                    List<Halo2Function> movementFunctions = new List<Halo2Function>();
                                    
                                    foreach (XmlNode inputVarField in movementFunctionGroups)
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
                                        func.BaseVal = Utils.ReadHalo2PackedFloat(signed, 6);
                                        func.RangeVal = Utils.ReadHalo2PackedFloat(signed, 10);

                                        // First integer is function type index
                                        func.FuncType = func.RawData[0];

                                        // Second int is use range, 0=off 1=on
                                        func.UseRange = func.RawData[1] != 0;

                                        movementFunctions.Add(func);
                                    }

                                    pPhysics.Gravity = movementFunctions[0];
                                    pPhysics.AirFriction = movementFunctions[1];
                                    pPhysics.RotFriction = movementFunctions[2];

                                    if (movementFunctions.Count >= 5)
                                    {
                                        pPhysics.CollFriction = movementFunctions[3];
                                        pPhysics.CollElasticity = movementFunctions[4];
                                    }
                                }

                                emitter.ParticlePhys = pPhysics;
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
                                func.BaseVal = Utils.ReadHalo2PackedFloat(signed, 6);
                                func.RangeVal = Utils.ReadHalo2PackedFloat(signed, 10);

                                // First integer is function type index
                                func.FuncType = func.RawData[0];

                                // Second int is use range, 0=off 1=on
                                func.UseRange = func.RawData[1] != 0;

                                // Get curve data
                                func.CurvePoints = Utils.HaloCurveParser.ParseCurve(signed);

                                // Function might hold colour rather than float values
                                // Convert raw data to UNsigned bytes
                                // Colour RGB data is in reverse order cos endianness, hence backwards array indices
                                byte[] unsigned = func.RawData.Select(i => (byte)i).ToArray();

                                // H2 stores RGB 0-255 but GameColor.FromRgb() expects linear 0-1 values
                                float[] colour1Linear = Utils.ByteRgbToLinearNormalized(unsigned[6], unsigned[5], unsigned[4]);
                                float[] colour2Linear = Utils.ByteRgbToLinearNormalized(unsigned[18], unsigned[17], unsigned[16]);
                                float[] colour3Linear = Utils.ByteRgbToLinearNormalized(unsigned[10], unsigned[9], unsigned[8]);
                                float[] colour4Linear = Utils.ByteRgbToLinearNormalized(unsigned[14], unsigned[13], unsigned[12]);

                                int funcType = func.RawData[1];

                                if (funcType >= 32) // Check for colour-type function
                                {
                                    if (funcType == 32 || funcType == 33)
                                    {
                                        // 2-Colour
                                        func.ColourType = 2;
                                        func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                                        func.Colour2 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);

                                        if (funcType == 33)
                                        {
                                            func.UseRange = true;
                                        }
                                        else
                                        {
                                            func.UseRange = false;
                                        }
                                    }
                                    else if (funcType == 48 || funcType == 49)
                                    {
                                        // 3-Colour
                                        func.ColourType = 3;
                                        // For some godforsaken reason the colour order changes the more colours there are
                                        // Figured out through trial and error which order to use
                                        func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                                        func.Colour3 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);
                                        func.Colour2 = GameColor.FromRgb(colour3Linear[0], colour3Linear[1], colour3Linear[2]);

                                        if (funcType == 49)
                                        {
                                            func.UseRange = true;
                                        }
                                        else
                                        {
                                            func.UseRange = false;
                                        }
                                    }
                                    else if (funcType == 64 || funcType == 65)
                                    {
                                        // 4-Colour
                                        // For some godforsaken reason the colour order changes the more colours there are
                                        // Figured out through trial and error which order to use
                                        func.ColourType = 4;
                                        func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                                        func.Colour4 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);
                                        func.Colour2 = GameColor.FromRgb(colour3Linear[0], colour3Linear[1], colour3Linear[2]);
                                        func.Colour3 = GameColor.FromRgb(colour4Linear[0], colour4Linear[1], colour4Linear[2]);

                                        if (funcType == 65)
                                        {
                                            func.UseRange = true;
                                        }
                                        else
                                        {
                                            func.UseRange = false;
                                        }
                                    }
                                }
                                
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

                            emitter.EmissionShape = int.Parse(emitterElement.SelectSingleNode("./field[@name='emission shape']")?.InnerText.Trim().Substring(0, 1));
                            emitter.TransOffset = emitterElement.SelectSingleNode("./field[@name='translational offset']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();
                            emitter.RelativeDir = emitterElement.SelectSingleNode("./field[@name='relative direction']").InnerText.Trim().Split(',').Select(s => float.Parse(s)).ToArray();

                            Utils.PrintObject(emitter, 2);

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
    
        public static Particle GetParticleDataFromXML(string xmlPath, string h2ekPath)
        {
            Particle particle = new Particle();

            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                DtdProcessing = DtdProcessing.Ignore
            };

            string xmlData = Utils.CleanXML(xmlPath);
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlData), settings))
            {
                XmlDocument particleFile = new XmlDocument();
                particleFile.Load(reader);

                // Simple data
                particle.Flags = particleFile.SelectSingleNode("//field[@name='flags']").InnerText.Trim().Replace("\t", "").Split('\n');
                particle.Billboard = int.Parse(particleFile.SelectSingleNode("//field[@name='first sequence index']").InnerText.Trim().Substring(0, 1));
                particle.FirstSeqIndex = int.Parse(particleFile.SelectSingleNode("//field[@name='first sequence index']").InnerText.Trim());
                particle.SeqCount = int.Parse(particleFile.SelectSingleNode("//field[@name='sequence count']").InnerText.Trim());

                // Shader stuff
                particle.ShaderTemplate = particleFile.SelectSingleNode("//tag_reference[@name='shader template']").InnerText.Trim();
                XmlNodeList shaderParams = particleFile.SelectNodes("//block[@name='shader parameters']/element");
                foreach (XmlNode parameter in shaderParams)
                {
                    if (parameter.SelectSingleNode("./field[@name='name']").InnerText.Trim() == "alpha_map_texture")
                    {
                        particle.Bitmap = new TagReferenceData
                        {
                            Path = parameter.SelectSingleNode("./tag_reference[@name='bitmap']").InnerText.Trim(),
                            Extension = "bitm"
                        };
                    }
                }

                // Function data
                XmlNodeList particleFunctionGroups = particleFile.SelectNodes("//field[@name='Input Variable']");
                List<Halo2Function> particleFunctions = new List<Halo2Function>();
                foreach (XmlNode inputVarField in particleFunctionGroups)
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
                    func.BaseVal = Utils.ReadHalo2PackedFloat(signed, 6);
                    func.RangeVal = Utils.ReadHalo2PackedFloat(signed, 10);

                    // First integer is function type index
                    func.FuncType = func.RawData[0];

                    // Second int is use range, 0=off 1=on
                    func.UseRange = func.RawData[1] != 0;

                    // Get curve data
                    func.CurvePoints = Utils.HaloCurveParser.ParseCurve(signed);

                    // Function might hold colour rather than float values
                    // Convert raw data to UNsigned bytes
                    // Colour RGB data is in reverse order cos endianness, hence backwards array indices
                    byte[] unsigned = func.RawData.Select(i => (byte)i).ToArray();

                    // H2 stores RGB 0-255 but GameColor.FromRgb() expects linear 0-1 values
                    float[] colour1Linear = Utils.ByteRgbToLinearNormalized(unsigned[6], unsigned[5], unsigned[4]);
                    float[] colour2Linear = Utils.ByteRgbToLinearNormalized(unsigned[18], unsigned[17], unsigned[16]);
                    float[] colour3Linear = Utils.ByteRgbToLinearNormalized(unsigned[10], unsigned[9], unsigned[8]);
                    float[] colour4Linear = Utils.ByteRgbToLinearNormalized(unsigned[14], unsigned[13], unsigned[12]);

                    int funcType = func.RawData[1];

                    if (funcType >= 32) // Check for colour-type function
                    {
                        if (funcType == 32 || funcType == 33)
                        {
                            // 2-Colour
                            func.ColourType = 2;
                            func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                            func.Colour2 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);

                            if (funcType == 33)
                            {
                                func.UseRange = true;
                            }
                            else
                            {
                                func.UseRange = false;
                            }
                        }
                        else if (funcType == 48 || funcType == 49)
                        {
                            // 3-Colour
                            func.ColourType = 3;
                            // For some godforsaken reason the colour order changes the more colours there are
                            // Figured out through trial and error which order to use
                            func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                            func.Colour3 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);
                            func.Colour2 = GameColor.FromRgb(colour3Linear[0], colour3Linear[1], colour3Linear[2]);

                            if (funcType == 49)
                            {
                                func.UseRange = true;
                            }
                            else
                            {
                                func.UseRange = false;
                            }
                        }
                        else if (funcType == 64 || funcType == 65)
                        {
                            // 4-Colour
                            // For some godforsaken reason the colour order changes the more colours there are
                            // Figured out through trial and error which order to use
                            func.ColourType = 4;
                            func.Colour1 = GameColor.FromRgb(colour1Linear[0], colour1Linear[1], colour1Linear[2]);
                            func.Colour4 = GameColor.FromRgb(colour2Linear[0], colour2Linear[1], colour2Linear[2]);
                            func.Colour2 = GameColor.FromRgb(colour3Linear[0], colour3Linear[1], colour3Linear[2]);
                            func.Colour3 = GameColor.FromRgb(colour4Linear[0], colour4Linear[1], colour4Linear[2]);

                            if (funcType == 65)
                            {
                                func.UseRange = true;
                            }
                            else
                            {
                                func.UseRange = false;
                            }
                        }
                    }

                    particleFunctions.Add(func);
                }

                // Same order as defined in the H2 tag
                particle.Colour = particleFunctions[0];
                particle.Alpha = particleFunctions[1];
                particle.Scale = particleFunctions[2];
                particle.Rotation = particleFunctions[3];
                particle.FrameIndex = particleFunctions[4];
            }

            return particle;
        }
    }
}
