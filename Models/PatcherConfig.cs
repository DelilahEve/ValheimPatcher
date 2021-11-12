namespace ValheimPatcher
{
    class PatcherConfig
    {
        public PatcherOption[] manifestOptions;
    }

    class PatcherOption
    {
        public string name;
        public string manifestUrl;

        public PatcherOption() { }
        public PatcherOption(string name, string url)
        {
            this.name = name;
            this.manifestUrl = url;
        }
    }

}
