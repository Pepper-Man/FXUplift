using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace H2ToH3EffectConverter
{
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

        public static Dictionary<string, int> funcInputMapping = new Dictionary<string, int>
        {
            { "0,particle age", 0 },
            { "1,particle emit time", 10 },
            { "2,particle random 1", 2 },
            { "3,particle random 2", 2 },
            { "4,emitter age", 1 },
            { "5,emitter random 1", 3 },
            { "6,emitter random 2", 3 },
            { "7,system lod", 8 },
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

            byte[] swapped = { b1, b0, b3, b2 };
            Array.Reverse(swapped);
            return BitConverter.ToSingle(swapped, 0);
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
                    var useRange = value.GetType().GetProperty("useRange")?.GetValue(value);
                    var baseVal = value.GetType().GetProperty("baseVal")?.GetValue(value);
                    var rangeVal = value.GetType().GetProperty("rangeVal")?.GetValue(value);

                    Console.WriteLine($"{indent}{prop.Name}: FuncType={funcType}, UseRange={useRange}, BaseVal={baseVal}, RangeVal={rangeVal}");
                }
                else
                {
                    Console.WriteLine($"{indent}{prop.Name}: {value}");
                }
            }
        }
    }
}
