using Microsoft.CodeAnalysis;


namespace Verlite
{
	[Generator]
	public class VersionEmbedGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context) { }

		public void Execute(GeneratorExecutionContext context)
		{
			var assembly = context.Compilation.SourceModule.Name;

			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteEmbedVersion", out var embedVersion);

			if (!string.IsNullOrEmpty(embedVersion) && embedVersion!.ToUpperInvariant() != "TRUE")
				return;

			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteVersion", out var version);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteMajor", out var major);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteMinor", out var minor);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerlitePatch", out var patch);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerlitePrerelease", out var prerelease);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteBuildMetadata", out var meta);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteCommit", out var commit);
			context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.VerliteHeight", out var height);

			version ??= string.Empty;
			major ??= string.Empty;
			minor ??= string.Empty;
			patch ??= string.Empty;
			prerelease ??= string.Empty;
			meta ??= string.Empty;
			commit ??= string.Empty;
			height ??= string.Empty;

			var coreVersion = string.IsNullOrEmpty(version) ? string.Empty : $"{major}.{minor}.{patch}";

			string source = $@"
#pragma warning disable

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""{assembly}"")]

namespace Verlite
{{
[global::System.Runtime.CompilerServices.CompilerGenerated]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class Version
	{{
		public const string Full = ""{version}"";
		public const string Core = ""{coreVersion}"";
		public const string Major = ""{major}"";
		public const string Minor = ""{minor}"";
		public const string Patch = ""{patch}"";
		public const string Prerelease = ""{prerelease}"";
		public const string BuildMetadata = ""{meta}"";
		public const string Commit = ""{commit}"";
		public const string Height = ""{height}"";
	}}
}}
";
			context.AddSource($"Verlite.g.cs", source);
		}
	}
}
