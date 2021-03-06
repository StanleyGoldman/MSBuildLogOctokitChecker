﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

namespace BCC.Web
{
    public class Program
    {
        [ExcludeFromCodeCoverage]
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        [ExcludeFromCodeCoverage]
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(new Program().BuildCrossCheckConfigure)
                .UseStartup<Startup>()
                .Build();

        public void BuildCrossCheckConfigure(WebHostBuilderContext context, IConfigurationBuilder config)
        {
            config.AddEnvironmentVariables("BCC_");

            var builtConfig = config.Build();

            var azureKeyVault = builtConfig["Azure:KeyVault"];

            if (!string.IsNullOrEmpty(azureKeyVault))
            {
                var keyVaultConfigBuilder = GetConfigurationBuilder();

                var keyVaultClient = GetKeyVaultClient();

                keyVaultConfigBuilder.AddAzureKeyVault($"https://{azureKeyVault}.vault.azure.net/", keyVaultClient, new StringUnescapingSecretsManager());
                
                var keyVaultConfig = keyVaultConfigBuilder.Build();

                config.AddConfiguration(keyVaultConfig);
            }
        }

        [ExcludeFromCodeCoverage]
        protected virtual KeyVaultClient GetKeyVaultClient()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            return new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
        }

        [ExcludeFromCodeCoverage]
        protected virtual IConfigurationBuilder GetConfigurationBuilder()
        {
            return new ConfigurationBuilder();
        }

        private class StringUnescapingSecretsManager : DefaultKeyVaultSecretManager
        {
            public override string GetKey(SecretBundle secret)
            {
                if (secret.ContentType == "text/plain")
                {
                    secret.Value = System.Text.RegularExpressions.Regex.Unescape(secret.Value);
                }
                return base.GetKey(secret);
            }
        }
    }
}
