using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;

namespace Ocean_ac
{
    /// <summary>
    /// Hosts the embedded dashboard (Resource/ocean_runtime.js) inside a WebView2
    /// control. The runtime bundles the dashboard CSS + JS + a snapshot of the API
    /// data, so the customer panel renders even when the localhost server is down.
    ///
    /// The client app already talks to the website server over HTTP
    /// (ApiBase = "http://localhost:8080"); this class only adds the in-app panel.
    /// Requires the Microsoft.Web.WebView2 NuGet package at build time.
    /// </summary>
    public static class OceanEmbeddedEngine
    {
        public static async Task InitializeRuntime(WebView2 webView)
        {
            await webView.EnsureCoreWebView2Async(null);

            // The renderer owns <body> entirely; give it a minimal shell to boot into.
            string shell =
                "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head>" +
                "<body><div id=\"app-root\"></div></body></html>";
            webView.NavigateToString(shell);

            webView.NavigationCompleted += async (s, e) =>
            {
                string jsRuntimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", "ocean_runtime.js");
                if (!File.Exists(jsRuntimePath)) return;
                try
                {
                    string runtime = File.ReadAllText(jsRuntimePath);
                    // Load the bundle, then render the dashboard into the shell.
                    await webView.CoreWebView2.ExecuteScriptAsync(runtime + ";true;");
                    await webView.CoreWebView2.ExecuteScriptAsync("window.renderOceanPage && window.renderOceanPage(\"index.html\", \"app-root\");");
                }
                catch { /* runtime missing or host not ready */ }
            };
        }

        /// <summary>Navigate the WebView2 to the live dashboard served by the local server.</summary>
        public static void NavigateToLiveDashboard(WebView2 webView, string baseUrl = "https://ocean-ac.onrender.com")
        {
            webView.Source = new Uri(baseUrl + "/dashboard");
        }
    }
}