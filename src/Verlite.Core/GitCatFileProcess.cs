using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Verlite
{
	internal sealed class GitCatFileProcess : IDisposable
	{
		internal Process? CatFileProcess { get; set; }
		internal Process? ShadowCatFileProcess { get; set; }
		private UTF8Encoding Encoding { get; } = new(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: false);

		private char[] WhitespaceChars { get; } = new[]
		{
			'\t',
			'\v',
			'\f',
			'\r',
			'\n',
			' ',
			'\u0085', // NEXT LINE
			'\u00A0', // NBSP
			'\u1680', // OGHAM SPACE MARK
			'\u2000', // EN QUAD
			'\u2001', // EM QUAD
			'\u2002', // EN SPACE
			'\u2003', // EM SPACE
			'\u2004', // THREE-PER-EM SPACE
			'\u2005', // FOUR-PER-EM SPACE
			'\u2006', // SIX-PER-EM SPACE
			'\u2007', // FIGURE SPACE
			'\u2008', // PUNCTUATION SPACE
			'\u2009', // THIN SPACE
			'\u200A', // HAIR SPACE
			'\u2028', // LINE SEPARATOR
			'\u2029', // PARAGRAPH SEPARATOR
			'\u202F', // NARROW NBSP
			'\u205F', // MEDIUM MATHEMATICAL SPACE
			'\u3000', // IDEOGRAPHIC SPACE
			'\u180E', // MONGOLIAN VOWEL SEPARATOR
			'\u200B', // ZERO WIDTH SPACE
			'\u200C', // ZERO WIDTH NON-JOINER
			'\u200D', // ZERO WIDTH JOINER
			'\u2060', // WORD JOINER
			'\uFEFF', // ZERO WIDTH NON-BREAKING SPACE
		};

		private ILogger? Log { get; }
		private string Root { get; }
		private string Name { get; }
		public GitCatFileProcess(ILogger? log, string root, string name)
		{
			Root = root;
			Name = name;
		}

		private readonly SemaphoreSlim catFileSemaphore = new(initialCount: 1, maxCount: 1);
		public async Task<string?> ReadObject(string type, string id)
		{
			await catFileSemaphore.WaitAsync();
			try
			{

				bool isFirst = false;
				if (CatFileProcess is null)
				{
					isFirst = true;

					Log?.Verbatim($"{Root} $ git cat-file --batch");
					ProcessStartInfo info = new()
					{
						FileName = "git",
						Arguments = "cat-file --batch",
						WorkingDirectory = Root,
						RedirectStandardError = false,
						RedirectStandardOutput = true,
						StandardOutputEncoding = Encoding,
						RedirectStandardInput = true,
						UseShellExecute = false,
					};
					CatFileProcess = Process.Start(info);
				}

				var (cin, cout) = (CatFileProcess.StandardInput, CatFileProcess.StandardOutput.BaseStream);

				// if this git call is forwarded onto another shell script,
				// then it's possible to query git before it's ready, but once
				// it does respond, it's ready to be used.
				if (isFirst)
				{
					Log?.Verbatim($"First run: awaiting cat-file startup ({Name})");
					await cin.WriteLineAsync(" ");

					using var cts = new CancellationTokenSource();

					var timeout = Task.Delay(5000, cts.Token);
					var gotBack = ReadLineAsync(cout);

					var completedTask = await Task.WhenAny(timeout, gotBack);

					if (completedTask != timeout)
						cts.Cancel();
					else
						throw new UnknownGitException($"The git cat-file process timed out ({Name})");

					var result = await gotBack;
					if (result.Trim(WhitespaceChars) != "missing")
						throw new UnknownGitException($"The git cat-file process returned unexpected output: {result} ({Name})");
				}

				using var cts2 = new CancellationTokenSource();
				var timeout2 = Task.Delay(30_000, cts2.Token);

				Log?.Verbatim($"git cat-file < {id} ({Name})");
				if (await Task.WhenAny(cin.WriteLineAsync(id), timeout2) == timeout2)
					throw new UnknownGitException($"The git cat-file process write timed out ({Name})");

				var readLineTask = ReadLineAsync(cout);
				if (await Task.WhenAny(readLineTask, timeout2) == timeout2)
					throw new UnknownGitException($"The git cat-file process read timed out ({Name})");

				string line = await readLineTask;
				Log?.Verbatim($"git cat-file > {line} ({Name})");
				string[] response = line.Trim(WhitespaceChars).Split(' ');


				if (response[0] != id)
					throw new UnknownGitException($"The returned blob hash did not match ({Name})");
				else if (response[1] == "missing")
					return null;
				else if (response[1] != type)
					throw new UnknownGitException($"Blob for {id} expected {type} but was {response[1]} ({Name})");

				var length = int.Parse(response[2], CultureInfo.InvariantCulture);
				var buffer = new byte[length];

				if (await Task.WhenAny(cout.ReadAsync(buffer, 0, length), timeout2) == timeout2)
					throw new UnknownGitException($"The git cat-file process read block timed out ({Name})");
				if (await Task.WhenAny(ReadLineAsync(cout), timeout2) == timeout2)
					throw new UnknownGitException($"The git cat-file process read block line timed out ({Name})");

				cts2.Cancel();
				return Encoding.GetString(buffer);
			}
			catch (Exception ex)
			{
				throw new UnknownGitException($"Failed to communicate with the git cat-file process: {ex.Message} ({Name})");
			}
			finally
			{
				catFileSemaphore.Release();
			}
		}

		private async Task<string> ReadLineAsync(Stream stream, int maxLength = 128)
		{
			var buffer = new byte[maxLength];

			int length = 0;
			for (; length < maxLength; length++)
			{
				if (await stream.ReadAsync(buffer, length, 1) == -1)
					break;
				else if (buffer[length] == '\n')
					break;
			}

			return Encoding.GetString(buffer, 0, length);
		}

		public void Dispose()
		{
			try
			{
				CatFileProcess?.StandardInput.Close();
				CatFileProcess?.Kill();
				CatFileProcess?.Close();
				catFileSemaphore.Dispose();
			}
			catch (IOException) { } // process may already be terminated
			catch (System.ComponentModel.Win32Exception) { }
			finally { }
		}

		internal Process? GetProcess()
		{
			return CatFileProcess;
		}
	}
}
