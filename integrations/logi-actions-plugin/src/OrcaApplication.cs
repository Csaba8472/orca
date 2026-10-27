namespace Loupedeck.OrcaPlugin
{
    using System;

    public class OrcaApplication : ClientApplication
    {
        protected override String GetProcessName() => "Orca";

        protected override String GetBundleName() => "com.stablyai.orca";

        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
