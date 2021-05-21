using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CSharpier
{
    public class ConfigurationFileOptions
    {
        public int PrintWidth { get; init; } = 100;
        public int TabWidth { get; init; } = 4;
        public bool UseTabs { get; init; }
        public EndOfLine EndOfLine { get; init; }

        public static ConfigurationFileOptions Create(
            string baseDirectoryPath,
            IFileSystem fileSystem
        ) {
            var directoryInfo = fileSystem.DirectoryInfo.FromDirectoryName(baseDirectoryPath);

            while (directoryInfo is not null)
            {
                var file = directoryInfo.EnumerateFiles(
                        ".csharpierrc*",
                        SearchOption.TopDirectoryOnly
                    )
                    .Where(
                        o =>
                            new[] { ".csharpierrc", ".json", ".yml", ".yaml" }.Contains(
                                o.Extension,
                                StringComparer.OrdinalIgnoreCase
                            )
                    )
                    .OrderBy(o => o.Extension)
                    .FirstOrDefault();

                if (file == null)
                {
                    directoryInfo = directoryInfo.Parent;
                    continue;
                }

                var contents = fileSystem.File.ReadAllText(file.FullName);
                return contents.TrimStart().StartsWith("{")
                    ? ReadJson(contents)
                    : ReadYaml(contents);
            }

            return new ConfigurationFileOptions();
        }

        private static ConfigurationFileOptions ReadJson(string contents)
        {
            return JsonConvert.DeserializeObject<ConfigurationFileOptions>(contents);
        }

        private static ConfigurationFileOptions ReadYaml(string contents)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(
                    CamelCaseNamingConvention.Instance
                )
                .Build();

            return deserializer.Deserialize<ConfigurationFileOptions>(contents);
        }
    }
}
