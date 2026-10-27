namespace Loupedeck.OrcaPlugin
{
    using System;

    public class OrcaPlugin : Plugin
    {
        // Navigation keys send keyboard shortcuts, so Orca must be a linked application.
        public override Boolean UsesApplicationApiOnly => false;

        public override Boolean HasNoApplication => false;

        public OrcaWorktreeStatusPoller WorktreeStatus { get; }

        public OrcaPlugin()
        {
            PluginLog.Init(this.Log);
            this.WorktreeStatus = new OrcaWorktreeStatusPoller(new OrcaCli());
        }

        public override void Load() => this.WorktreeStatus.Start();

        public override void Unload() => this.WorktreeStatus.Dispose();
    }
}
