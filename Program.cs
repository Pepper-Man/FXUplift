namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TEMPORARY HARDCODING
            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.effect";
            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath);
            ReadFromH2.Effect effect = ReadFromH2.GetDataFromXML(h2XmlPath);
            WriteToH3.WriteH3Effect("C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK", "halo_2\\effe_test\\cables_cut_major", effect);
        }
    }
}
