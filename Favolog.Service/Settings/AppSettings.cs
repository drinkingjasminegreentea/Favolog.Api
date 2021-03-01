namespace Favolog.Service.Settings
{
    public class AppSettings
    {
        public const string Section = "Settings";

        public string OpenGraphGeneratorUrl { get; set; }

        public string AzureBlobConnectionsString { get; set; }
    }
}
