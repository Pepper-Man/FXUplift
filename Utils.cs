using Bungie;
using Bungie.Game;
using Bungie.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace H2ToH3EffectConverter
{
    using CurvePoints = ReadFromH2.CurvePoints;

    internal class Utils
    {
        public static string CleanXML(string filePath)
        {
            try
            {
                // Read as text
                string xmlContent = File.ReadAllText(filePath);

                // Clean invalid characters
                string cleanedXML = RemoveInvalidXmlCharacters(xmlContent);

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

        public static Dictionary<string, int> funcInputMapping = new Dictionary<string, int>
        {
            { "0,particle age", 0 },
            { "1,particle emit time", 10 },
            { "2,particle random 1", 2 },
            { "3,particle random 2", 2 },
            { "4,emitter age", 1 },
            { "5,emitter random 1", 3 },
            { "6,emitter random 2", 3 },
            { "7,system lod", 11 },
            { "8,game time", 12 },
            { "9,effect a scale", 13 },
            { "10,effect b scale", 14 },
            { "11,particle rotation", 15 },
            { "12,explosion animation", 18 },
            { "13,explosion rotation", 19 },
            { "14,particle random 3", 2 },
            { "15,particle random 4", 2 },
            { "16,location random", 16 }
        };

        public static float ReadHalo2PackedFloat(sbyte[] data, int i)
        {
            byte b0 = (byte)data[i];
            byte b1 = (byte)data[i + 1];
            byte b2 = (byte)data[i + 2];
            byte b3 = (byte)data[i + 3];

            byte[] swappedOrder = { b1, b0, b3, b2 };
            Array.Reverse(swappedOrder);
            return BitConverter.ToSingle(swappedOrder, 0);
        }

        public static void PrintObject(object obj, int indents)
        {
            string indent = "";
            if (indents == 1)
            {
                indent = "\t";
            }
            else if (indents == 2)
            {
                indent = "\t\t";
            }

            Type type = obj.GetType();
            foreach (var prop in type.GetProperties())
            {
                var value = prop.GetValue(obj);
                if (value == null)
                {
                    Console.WriteLine($"{indent}{prop.Name}: <null>");
                    continue;
                }

                // Arrays
                if (value.GetType().IsArray)
                {
                    var array = (IEnumerable)value;
                    var elements = new List<string>();
                    foreach (var item in array)
                        elements.Add(item?.ToString() ?? "<null>");
                    Console.WriteLine($"{indent}{prop.Name}: {string.Join(", ", elements)}");
                }

                // TagReferenceData
                else if (value is ReadFromH2.TagReferenceData tagRef)
                {
                    Console.WriteLine($"{indent}{prop.Name}: {tagRef.Path}.{tagRef.Extension}");
                }

                // Halo2Functions
                else if (value.GetType().GetProperty("FuncType") != null)
                {
                    var funcType = value.GetType().GetProperty("FuncType")?.GetValue(value);
                    var useRange = value.GetType().GetProperty("UseRange")?.GetValue(value);
                    var baseVal = value.GetType().GetProperty("BaseVal")?.GetValue(value);
                    var rangeVal = value.GetType().GetProperty("RangeVal")?.GetValue(value);
                    var colour1 = value.GetType().GetProperty("Colour1")?.GetValue(value);
                    var colour2 = value.GetType().GetProperty("Colour2")?.GetValue(value);
                    var colour3 = value.GetType().GetProperty("Colour3")?.GetValue(value);
                    var colour4 = value.GetType().GetProperty("Colour4")?.GetValue(value);

                    if (prop.Name != "Tint")
                    {
                        Console.WriteLine($"{indent}{prop.Name}: FuncType={Utils.funcTypeMapping[(int)funcType]}, UseRange={useRange}, BaseVal={baseVal}, RangeVal={rangeVal}");
                    }
                    else
                    {
                        string toColourString(object c) => c is GameColor g ? $"{g.Red},{g.Green},{g.Blue}" : "null";

                        string colour1String = toColourString(colour1);
                        string colour2String = toColourString(colour2);
                        string colour3String = toColourString(colour3);
                        string colour4String = toColourString(colour4);

                        Console.WriteLine($"{indent}{prop.Name}: FuncType={Utils.funcTypeMapping[(int)funcType]}, UseRange={useRange}, Colour1={colour1String}, Colour2={colour2String}, Colour3={colour3String}, Colour4={colour4String}");
                    }
                }
                else
                {
                    Console.WriteLine($"{indent}{prop.Name}: {value}");
                }
            }
        }

        public static string ExtractTagXML(string h2TagPath, string type, string h2ek)
        {
            string toolExePath = Path.Combine(h2ek, "tool.exe");
            if (!File.Exists(toolExePath)) throw new FileNotFoundException($"Could not find tool.exe at \"{toolExePath}\".", toolExePath);

            string xmlOutPath = GetXmlOutPath(h2TagPath, type);

            // Reuse an export made earlier if the tag hasn't changed since
            if (HasFreshXml(h2TagPath, xmlOutPath)) return xmlOutPath;

            // Delete any stale or invalid export so a failed or old export is never mistaken for a fresh one
            if (File.Exists(xmlOutPath)) File.Delete(xmlOutPath);

            // Build arguments
            string[] argumentsArray = new string[]
            {
                "export-tag-to-xml",
                "\"" + h2TagPath + "\"",
                "\"" + xmlOutPath + "\""
            };

            string arguments = string.Join(" ", argumentsArray);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = toolExePath,
                Arguments = arguments,
                WorkingDirectory = h2ek,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"[Warning] tool.exe exited with code {process.ExitCode} for \"{h2TagPath}\".");
                }
            }

            if (!File.Exists(xmlOutPath)) throw new FileNotFoundException($"tool.exe did not produce an XML file for \"{h2TagPath}\".", xmlOutPath);

            return xmlOutPath;
        }

        public static void ExtractTagsBatch(IEnumerable<string> h2TagPaths, string type, string h2ek, int chunkSize = 250)
        {
            string toolExePath = Path.Combine(h2ek, "tool.exe");
            if (!File.Exists(toolExePath)) throw new FileNotFoundException($"Could not find tool.exe at \"{toolExePath}\".", toolExePath);

            string tagsRoot = Path.Combine(h2ek, "tags") + Path.DirectorySeparatorChar;
            char[] listingBreakers = { ',', '\t', '\r', '\n' }; // Would corrupt the delicate batch txt "<tag>,<outputpath>" format

            // Collect the tags that actually need exporting
            List<KeyValuePair<string, string>> toExport = new List<KeyValuePair<string, string>>();
            int leftForIndividual = 0;
            foreach (string tagPath in h2TagPaths)
            {
                string xmlOutPath = GetXmlOutPath(tagPath, type);
                if (HasFreshXml(tagPath, xmlOutPath)) continue;

                if (!tagPath.StartsWith(tagsRoot, StringComparison.OrdinalIgnoreCase) ||
                    tagPath.IndexOfAny(listingBreakers) >= 0 || xmlOutPath.IndexOfAny(listingBreakers) >= 0)
                {
                    leftForIndividual++; // Will corrupt the batch txt data, so extract this individually
                    continue;
                }

                if (File.Exists(xmlOutPath)) File.Delete(xmlOutPath); // Stale or bad data
                toExport.Add(new KeyValuePair<string, string>(tagPath, xmlOutPath));
            }

            Console.WriteLine($"XML batch export ({type}): {toExport.Count} to batch export, {leftForIndividual} left for individual export.");
            if (toExport.Count == 0) return;

            string listPath = Path.Combine(Path.GetTempPath(), "h2_export_list.txt");
            int chunkCount = (toExport.Count + chunkSize - 1) / chunkSize;

            for (int c = 0; c < chunkCount; c++)
            {
                List<KeyValuePair<string, string>> chunk = toExport.Skip(c * chunkSize).Take(chunkSize).ToList();

                // Format must be "<tags\relative\tag.extension>,<full\output\filepath.xml>"
                // and the final line MUST end with a line break
                File.WriteAllText(listPath, string.Concat(chunk.Select(kv => kv.Key.Substring(tagsRoot.Length) + "," + kv.Value + "\r\n")), Encoding.Default);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = toolExePath,
                    Arguments = $"export-tags-to-xml \"{listPath}\"",
                    WorkingDirectory = h2ek,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    // Per-tag progress lines are discarded; report per chunk instead
                    process.OutputDataReceived += (sender, e) => { };
                    process.ErrorDataReceived += (sender, e) => { };
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"[Warning] tool.exe exited with code {process.ExitCode} during batch {c + 1}/{chunkCount}.");
                    }
                }

                Console.WriteLine($"  batch {c + 1}/{chunkCount} done");
            }

            int missing = toExport.Count(kv => !HasFreshXml(kv.Key, kv.Value));
            if (missing > 0)
            {
                Console.WriteLine($"[Warning] {missing} tags weren't exported by the batch and will be exported individually.");
            }
        }

        private const string H2TagsRootToken = "H2EK\\tags";

        // Index of "H2EK\tags" in a path, or -1 if not found
        public static int IndexOfTagsRoot(string fullPath)
        {
            return fullPath.IndexOf(H2TagsRootToken, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetH2EKPath(string fullEffectPath)
        {
            int index = IndexOfTagsRoot(fullEffectPath);
            if (index < 0) throw new ArgumentException($"Path is not inside \"H2EK\\tags\": {fullEffectPath}");

            return fullEffectPath.Substring(0, index + "H2EK".Length);
        }

        // Parse floats the same way on every machine regardless of decimal separator
        public static float ParseFloat(string s)
        {
            return float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        // Tag XML enums look like "3 some name" (or just "3"). Returns the leading integer.
        public static int ParseEnumIndex(string enumText)
        {
            if (enumText == null) throw new FormatException("Enum field is missing from the tag XML!");

            Match m = Regex.Match(enumText, @"^\s*(-?\d+)");
            if (!m.Success) throw new FormatException($"Could not read an enum index from \"{enumText.Trim()}\".");

            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        // Convert 0..255 byte -> normalized sRGB 0..1
        public static float Norm(byte b) => b / 255f;

        // byte[] -> plain 0..1 floats, no gamma conversion
        // GameColor.FromRgb() takes the value shown in H3 Guerilla's Xenon column
        public static float[] ByteRgbToNormalized(byte r, byte g, byte b)
        {
            return new float[] { Norm(r), Norm(g), Norm(b) };
        }

        public static Dictionary<int, FunctionEditorMasterType> funcTypeMapping = new Dictionary<int, FunctionEditorMasterType>
        {
            { 0, FunctionEditorMasterType.Transition },
            { 1, FunctionEditorMasterType.Basic },
            { 2, FunctionEditorMasterType.Transition },
            { 3, FunctionEditorMasterType.Periodic },
            { 4, FunctionEditorMasterType.Curve },
            { 5, FunctionEditorMasterType.Curve },
            { 6, FunctionEditorMasterType.Curve },
            { 7, FunctionEditorMasterType.Curve },
            { 8, FunctionEditorMasterType.Curve },
            { 9, FunctionEditorMasterType.Exponent },
            { 10, FunctionEditorMasterType.Curve }
        };

        public static Dictionary<int, int> billboardTypeMapping = new Dictionary<int, int>
        {
            { 0, 0 },
            { 1, 2 },
            { 2, 3 },
            { 3, 4 },
            { 4, 5 },
        };

        public static Dictionary<string, uint> particleMainFlagValues = new Dictionary<string, uint>
        {
            { "dies at rest", 1 },
            { "dies in air", 8 },
            { "dies in media", 4 },
            { "dies on structure collision", 2 },
            { "has sweetener", 16 },
        };

        public static Dictionary<string, uint> particleAppearFlagValues = new Dictionary<string, uint>
        {
            { "bitmap authored vertically", 32 },
            { "disable frame blending", 0 },
            { "random u mirror", 1 },
            { "random v mirror", 2 },
            { "receive lightmap lighting", 8 },
            { "select random sequence", 0 },
            { "spins", 0 },
            { "tint from diffuse texture", 16 },
        };

        public static Dictionary<string, uint> particleAnimFlagValues = new Dictionary<string, uint>
        {
            { "can animate backwards", 2 },
            { "frame animation one shot", 1 },
        };

        // Dictionary for converting H2's particle shader templates to their respective alpha mode
        // Plasma doesn't exist in H3, dunno what to do with that
        // Alpha test is a special case since its a different category, hence 99
        // "Normal" additive mode for H3 particles doesnt let them fade properly, need to use add*srcalpha instead
        // 0 - opaque
        // 1 - additive
        // 2 - multiply
        // 3 - alpha blend
        // 4 - double mult
        // 5 - max
        // 6 - mult add
        // 7 - add times dstalpha
        // 8 - add times srcalpha
        // 9 - inv alpha blend
        // 10 - pre mult alpha
        // 50 - plasma
        // 99 - alpha test
        public static Dictionary<string, int> shaderTemplateBlendMode = new Dictionary<string, int>
        {
            { "particle_alpha_blend_distortion", 3 },
            { "particle_alpha_test", 99 },
            { "particle_plasma", 50 },
            { "particle_alpha_multiply_add_tint", 6 },
            { "particle_alpha_test_no_lighting", 99 },
            { "particle_alpha_test_tint", 99 },
            { "particle_alpha_test_tint_no_lighting", 99 },
            { "particle_additive", 8 },
            { "particle_additive_tint", 8 },
            { "particle_alpha_blend", 3 },
            { "particle_alpha_blend_tint", 3 },
            { "particle_alpha_multiply_add", 6 },
            { "particle_alpha_test_no_lighting_fixed_for_glass", 99 },
        };

        public static class HaloCurveParser
        {
            // Spline blocks also store the evaluated cubic f(x) = a x^3 + b x^2 + c x + y0.
            // Handles are only the editor's view of it, and can be corrupt.
            private static void RepairSplineHandles(float y0, float y1, float a, float b, float c, ref float h1x, ref float h1y, ref float h2x, ref float h2y)
            {
                float startSlope = c;
                float endSlope = 3 * a + 2 * b + c;

                bool Close(float x, float y)
                {
                    return Math.Abs(x - y) <= 1e-3f * Math.Max(1f, Math.Abs(y));
                }

                bool h1Ok = h1x > 0f && h1x < 1f && Close((h1y - y0) / h1x, startSlope);
                if (!h1Ok)
                {
                    h1x = 1f / 3f;
                    h1y = y0 + startSlope * h1x;
                }

                bool h2Ok = h2x > h1x && h2x < 0.999f && Close((y1 - h2y) / (1f - h2x), endSlope);
                if (!h2Ok)
                {
                    h2x = h1x < 0.5f ? 2f / 3f : (1f + h1x) / 2f;
                    h2y = y1 - endSlope * (1f - h2x);
                }
            }

            public static CurvePoints ParseCurve(sbyte[] data)
            {
                // Safely warn and return 0 instead of throwing if reading past end of data
                bool warnedTruncated = false;

                // Helper to decode float from 4 bytes starting at an index
                float ReadFloat(int byteIndex)
                {
                    if (byteIndex < 0 || byteIndex + 4 > data.Length)
                    {
                        if (!warnedTruncated)
                        {
                            Console.WriteLine($"[Warning] Curve data (type {data[0]}) is only {data.Length} bytes long, values beyond that are read as 0.");
                            warnedTruncated = true;
                        }

                        return 0.0f;
                    }

                    byte[] raw = new byte[4];
                    for (int i = 0; i < 4; i++)
                        raw[i] = (byte)data[byteIndex + i];

                    return BitConverter.ToSingle(raw, 0);
                }

                // First value determines graph type: 4 = linear, 5 = linear key
                int graphType = data[0];

                if (graphType == 4) // Linear
                {
                    // y0 and y1 always exist?
                    float baseY0 = ReadFloat(24);
                    float baseY1 = ReadFloat(32);
                    float rangeY0 = ReadFloat(48);
                    float rangeY1 = ReadFloat(56);

                    return new CurvePoints
                    {
                        BaseY0 = baseY0,
                        BaseY1 = baseY1,
                        BaseY2 = null,
                        BaseY3 = null,
                        RangeY0 = rangeY0,
                        RangeY1 = rangeY1,
                        RangeY2 = null,
                        RangeY3 = null,
                    };
                }
                else if (graphType == 5) // Linear key
                {
                    float baseY0 = ReadFloat(24);
                    float baseX1 = ReadFloat(28);
                    float baseY1 = ReadFloat(32);
                    float baseX2 = ReadFloat(64);
                    float baseY2 = ReadFloat(40);
                    float baseY3 = ReadFloat(48);

                    float rangeY0 = ReadFloat(104);
                    float rangeX1 = ReadFloat(108);
                    float rangeY1 = ReadFloat(112);
                    float rangeX2 = ReadFloat(116);
                    float rangeY2 = ReadFloat(120);
                    float rangeY3 = ReadFloat(128);


                    return new CurvePoints
                    {
                        BaseY0 = baseY0,
                        BaseX1 = baseX1,
                        BaseY1 = baseY1,
                        BaseX2 = baseX2,
                        BaseY2 = baseY2,
                        BaseY3 = baseY3,
                        RangeY0 = rangeY0,
                        RangeX1 = rangeX1,
                        RangeY1 = rangeY1,
                        RangeX2 = rangeX2,
                        RangeY2 = rangeY2,
                        RangeY3 = rangeY3,
                    };
                }
                else if (graphType == 7 || graphType == 10) // Spline 
                {
                    float baseY0 = ReadFloat(24);
                    float baseY1 = ReadFloat(48);
                    float baseHandle1X = ReadFloat(28);
                    float baseHandle1Y = ReadFloat(32);
                    float baseHandle2X = ReadFloat(36);
                    float baseHandle2Y = ReadFloat(40);

                    float rangeY0 = ReadFloat(72);
                    float rangeY1 = ReadFloat(96);
                    float rangeHandle1X = ReadFloat(76);
                    float rangeHandle1Y = ReadFloat(80);
                    float rangeHandle2X = ReadFloat(84);
                    float rangeHandle2Y = ReadFloat(88);

                    // Base block
                    RepairSplineHandles(baseY0, baseY1, ReadFloat(52), ReadFloat(56), ReadFloat(60), ref baseHandle1X, ref baseHandle1Y, ref baseHandle2X, ref baseHandle2Y);

                    // Range block, same layout, 48 bytes further on
                    RepairSplineHandles(rangeY0, rangeY1, ReadFloat(100), ReadFloat(104), ReadFloat(108), ref rangeHandle1X, ref rangeHandle1Y, ref rangeHandle2X, ref rangeHandle2Y);

                    bool spline1 = false;
                    bool spline2 = false;
                    if (graphType == 7)
                    {
                        spline1 = true;
                    }
                    else
                    {
                        spline2 = true;
                    }

                    return new CurvePoints
                    {
                        IsSpline1 = spline1,
                        IsSpline2 = spline2,
                        BaseY0 = baseY0,
                        BaseY1 = baseY1,
                        BaseHandle1X = baseHandle1X,
                        BaseHandle1Y = baseHandle1Y,
                        BaseHandle2X = baseHandle2X,
                        BaseHandle2Y = baseHandle2Y,
                        RangeY0 = rangeY0,
                        RangeY1 = rangeY1,
                        RangeHandle1X = rangeHandle1X,
                        RangeHandle1Y = rangeHandle1Y,
                        RangeHandle2X = rangeHandle2X,
                        RangeHandle2Y = rangeHandle2Y
                    };
                }
                else if (graphType == 9) // Exponent
                {
                    float baseMin = ReadFloat(20);
                    float baseMax = ReadFloat(24);
                    float baseExponent = ReadFloat(28);

                    float rangeMin = ReadFloat(32);
                    float rangeMax = ReadFloat(36);
                    float rangeExponent = ReadFloat(40);

                    return new CurvePoints
                    {
                        BaseMin = baseMin,
                        BaseMax = baseMax,
                        BaseExponent = baseExponent,
                        RangeMin = rangeMin,
                        RangeMax = rangeMax,
                        RangeExponent = rangeExponent
                    };
                }
                else if (graphType == 3) // Periodic
                {
                    float baseMin = ReadFloat(28);
                    float baseMax = ReadFloat(32);
                    float baseFrequency = ReadFloat(20);
                    float basePhase = ReadFloat(24);
                    int basePeriodFunc = data[2];

                    float rangeMin = ReadFloat(44);
                    float rangeMax = ReadFloat(48);
                    float rangeFrequency = ReadFloat(36);
                    float rangePhase = ReadFloat(40);
                    int rangePeriodFunc = data[3];


                    return new CurvePoints
                    {
                        BaseMin = baseMin,
                        BaseMax = baseMax,
                        BaseFreq = baseFrequency,
                        BasePhase = basePhase,
                        RangeMin = rangeMin,
                        RangeMax = rangeMax,
                        RangeFreq = rangeFrequency,
                        RangePhase = rangePhase,
                        BaseGraphFunc = basePeriodFunc,
                        RangeGraphFunc = rangePeriodFunc
                    };
                }
                else if (graphType == 2) // Transition
                {
                    float baseMin = ReadFloat(20);
                    float baseMax = ReadFloat(24);
                    int baseTransFunc = data[2];

                    float rangeMin = ReadFloat(28);
                    float rangeMax = ReadFloat(32);
                    int rangeTransFunc = data[3];

                    return new CurvePoints
                    {
                        BaseMin = baseMin,
                        BaseMax = baseMax,
                        BaseGraphFunc = baseTransFunc,
                        RangeMin = rangeMin,
                        RangeMax = rangeMax,
                        RangeGraphFunc = rangeTransFunc
                    };
                }
                else
                {
                    return new CurvePoints { BaseY0 = 0, BaseY1 = 0, BaseY2 = null, BaseY3 = null };
                }    
            }
        }
    
        public static string H2ToH3Path(string h2EffectPath)
        {
            int tagsIndex = IndexOfTagsRoot(h2EffectPath);
            if (tagsIndex < 0) throw new ArgumentException($"Path is not inside \"H2EK\\tags\": {h2EffectPath}");

            string relativeH2Path = h2EffectPath.Substring(tagsIndex + H2TagsRootToken.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine("halo_2\\", Path.GetDirectoryName(relativeH2Path), Path.GetFileNameWithoutExtension(relativeH2Path));
        }

        public static string GetEditingKitFolder(string ek)
        {
            string editingKit;

            while (true)
            {
                Console.WriteLine($"Enter path to your \"{ek}\" folder:");
                editingKit = Console.ReadLine().Replace("\"", "");
                if (Directory.Exists(editingKit))
                {
                    if (string.Equals(new DirectoryInfo(editingKit.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)).Name, ek, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"This doesn't appear to be the \"{ek}\" folder path. Please try again.\n");
                    }
                }
                else
                {
                    Console.WriteLine("Folder does not exist! Please try again.\n");
                }
            }

            return editingKit;
        }

        // Get folder the exported XMLs for a tag type live in
        private static string GetXmlDir(string type)
        {
            string subFolder = "";
            if (type == "effect") subFolder = "xml\\effects";
            else if (type == "particle physics") subFolder = "xml\\particle_phys";
            else if (type == "particle") subFolder = "xml\\particles";

            string xmlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, subFolder);
            Directory.CreateDirectory(xmlDir);
            return xmlDir;
        }

        // Short hash of the full tag path, so same-named tags in different folders get different XML files
        private static string ShortPathHash(string path)
        {
            using (var sha1 = System.Security.Cryptography.SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
                return BitConverter.ToString(hash, 0, 4).Replace("-", "");
            }
        }

        public static string GetXmlOutPath(string h2TagPath, string type)
        {
            string tagName = Path.GetFileNameWithoutExtension(h2TagPath);
            return Path.Combine(GetXmlDir(type), $"{tagName}_{ShortPathHash(Path.GetFullPath(h2TagPath))}.xml");
        }

        // An export should be reused if it exists, isn't empty, and is at least as new as the tag
        private static bool HasFreshXml(string h2TagPath, string xmlOutPath)
        {
            FileInfo xml = new FileInfo(xmlOutPath);
            return xml.Exists && xml.Length > 0 && xml.LastWriteTimeUtc >= File.GetLastWriteTimeUtc(h2TagPath);
        }
    }
}
