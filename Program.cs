namespace H2ToH3EffectConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // TEMPORARY HARDCODING
            string h2EffectPath = "G:\\Steam\\steamapps\\common\\H2EK\\tags\\effects\\scenarios\\solo\\alphagasgiant\\cables\\cables_cut_major.effect";
            string h3ek = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\H3EK";
            string h2ek = Utils.GetH2EKPath(h2EffectPath);
            string h2XmlPath = Utils.ExtractTagXML(h2EffectPath, "effect", h2ek);
            ReadFromH2.Effect effect = ReadFromH2.GetEffectDataFromXML(h2XmlPath, h2ek, h3ek);
            WriteToH3.WriteH3Effect(h3ek, "halo_2\\effe_test\\cables_cut_major", effect);
        }
    }
}
