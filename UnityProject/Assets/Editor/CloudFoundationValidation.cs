namespace CrowdCheckpoint.Unity.Editor
{
    /// <summary>Preserve the S1-CI public entry point; no cloud requirement inside the Editor.</summary>
    public static class CloudFoundationValidation
    {
        public static void ConfigureAndReport()
        {
            FoundationValidation.ConfigureAndReport();
        }
    }
}
