namespace AnimeSD2HD
{
    internal record NumberOption(string Label, int Value);

    internal class WaifuModel
    {
        public WaifuModel(string label, string identifier, params NumberOption[] scales)
        {
            ID = identifier;
            Label = label;
            ScalingOptions = scales;
            DenoiseOptions = new[]
            {
                new NumberOption("0: None", 0),
                new NumberOption("1: Low", 1),
                new NumberOption("2: Medium", 2),
                new NumberOption("3: High", 3)
            };
        }
        public string ID { get; private set; }
        public string Label { get; set; }
        public NumberOption[] ScalingOptions { set; get; }
        public NumberOption[] DenoiseOptions { set; get; }
    }

    internal static class WaifuModels
    {
        private static WaifuModel AnimeStyleArtRGB { get; } = new WaifuModel("Anime Style Art RGB", "models-upconv_7_anime_style_art_rgb", new NumberOption("2×", 2));
        private static WaifuModel CUNet { get; } = new WaifuModel("CUnet", "models-cunet", new NumberOption("1×", 1), new NumberOption("2×", 2));
        private static WaifuModel Photo { get; } = new WaifuModel("Photo", "models-upconv_7_photo", new NumberOption("2×", 2));

        public static WaifuModel[] AvailableModels { get; } = new[] { AnimeStyleArtRGB, CUNet, Photo };
    }
}