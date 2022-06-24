using System.Globalization;
using System.Text;

namespace Verlite.CLI
{
	internal static class JsonOutput
	{
		public static string GenerateOutput(
			SemVer version,
			Commit? commit,
			TaggedVersion? lastTag,
			int? height)
		{
			var sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendString(1, "commit", commit?.Id);
			sb.AppendString(1, "full", version.ToString());
			sb.AppendInteger(1, "major", version.Major);
			sb.AppendInteger(1, "minor", version.Minor);
			sb.AppendInteger(1, "patch", version.Patch);
			sb.AppendString(1, "prerelease", version.Prerelease);
			sb.AppendString(1, "meta", version.BuildMetadata);
			sb.AppendInteger(1, "height", height);
			if (lastTag is null)
			{
				sb.AppendLine("\t" + @"""lastTag"": null");
			}
			else
			{
				sb.AppendLine("\t" + @"""lastTag"": {");
				sb.AppendString(2, "tag", lastTag.Tag.Name);
				sb.AppendString(2, "commit", lastTag.Tag.PointsTo.Id);
				sb.AppendString(2, "full", lastTag.Version.ToString());
				sb.AppendInteger(2, "major", lastTag.Version.Major);
				sb.AppendInteger(2, "minor", lastTag.Version.Minor);
				sb.AppendInteger(2, "patch", lastTag.Version.Patch);
				sb.AppendString(2, "prerelease", lastTag.Version.Prerelease);
				sb.AppendString(2, "meta", lastTag.Version.BuildMetadata, final: true);
				sb.AppendLine("\t}");
			}
			sb.AppendLine("}");

			return sb.ToString();
		}

		private static void AppendString(this StringBuilder sb, int indentation, string key, string? value, bool final = false)
		{
			value = value is null ? "null" : $@"""{System.Web.HttpUtility.JavaScriptStringEncode(value)}""";
			sb.Append(new string('\t', indentation));
			sb.Append($@"""{key}"": {value}");
			if (final)
				sb.AppendLine("");
			else
				sb.AppendLine(",");
		}
		private static void AppendInteger(this StringBuilder sb, int indentation, string key, int? value, bool final = false)
		{
			var valuestr = value is null ? "null" : value.Value.ToString(CultureInfo.InvariantCulture);
			sb.Append(new string('\t', indentation));
			sb.Append($@"""{key}"": ""{valuestr}""");
			if (final)
				sb.AppendLine("");
			else
				sb.AppendLine(",");
		}
	}
}
