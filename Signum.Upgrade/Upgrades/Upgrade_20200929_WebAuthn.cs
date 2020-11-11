using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Signum.Upgrade.Upgrades
{
    class Upgrade_20200929_WebAuthn : CodeUpgradeBase
    {
        public override string Description => "Add Support to WebAuthn";

        public override string SouthwindCommitHash => "e7d32021965393f0fb2b46ef4ef58b464b091f93 120693c33e6948ce820cd093fec49b9206fe114f";

        protected override void ExecuteInternal(UpgradeContext uctx)
        {
            uctx.ChangeCodeFile($@"Southwind.Entities\ApplicationConfiguration.cs", file =>
            {
                file.InsertAfterFirstLine(
                    l => l.Contains("public AuthTokenConfigurationEmbedded AuthTokens { get; set; }"),
                    "public WebAuthnConfigurationEmbedded WebAuthn { get; set; }");

            });


            uctx.ChangeCodeFile($@"Southwind.Logic\Starter.cs", file =>
            {
                file.InsertAfterLastLine(
                    l => 
                    l.Contains("SessionLogLogic.Start(sb);") || 
                    l.Contains("UserTicketLogic.Start(sb);") || 
                    l.Contains("AuthLogic.StartAllModules(sb);"),
                    "WebAuthnLogic.Start(sb, ()=> Configuration.Value.WebAuthn);");
            });

            uctx.ChangeCodeFile($@"Southwind.React\App\Layout.tsx", file =>
            {
                file.InsertAfterLastLine(
                    l => l.StartsWith("import"),
                    "import * as WebAuthnClient from '@extensions/Authorization/WebAuthn/WebAuthnClient'");

                file.Replace(
                    "<LoginDropdown />",
                    "<LoginDropdown extraButons={user => <WebAuthnClient.WebAuthnRegisterMenuItem />} />");
            });

            uctx.ChangeCodeFile($@"Southwind.React\App\MainPublic.tsx", file =>
            {
                file.InsertAfterFirstLine(
                    l => l.StartsWith("import * as AuthClient"),
                    "import * as WebAuthnClient from '@extensions/Authorization/WebAuthn/WebAuthnClient'");

                file.InsertAfterFirstLine(
                    l => l.StartsWith("import NotFound from './NotFound'"),
                    "import Login from '@extensions/Authorization/Login/Login'");

                file.InsertBeforeFirstLine(
              l => l.StartsWith("Services.SessionSharing.setAppNameAndRequestSessionStorage"),
              @"Login.customLoginButtons = ctx => <WebAuthnClient.WebAuthnLoginButton ctx={ctx} />;
");

                file.InsertAfterFirstLine(
                    l => l.StartsWith("AuthClient.startPublic"),
                    @"WebAuthnClient.start({ routes, applicationName: ""Southwind"" });");
            });

            uctx.ChangeCodeFile($@"Southwind\Templates\ApplicationConfiguration.tsx", file =>
            {
                file.InsertAfterLastLine(
                    l => l.StartsWith("</Tab>"),
@"<Tab eventKey=""webauthn"" title={ctx.niceName(a => a.webAuthn)}>
  <RenderEntity ctx={ctx.subCtx(a => a.webAuthn)} />
 </Tab>");   
            });

            uctx.ChangeCodeFile($@"Southwind.Terminal\SouthwindMigrations.cs", file =>
            {
                file.InsertBeforeFirstLine(
                    l => l.StartsWith("}, //Auth"),
@"},
WebAuthn = new WebAuthnConfigurationEmbedded
{
    ServerName = ""Southwind""");
            });
        }
    }
}
