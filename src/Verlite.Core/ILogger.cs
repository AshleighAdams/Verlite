namespace Verlite
{
	public interface ILogger
	{
		void Normal(string output);
		void Verbose(string output);
		void Verbatim(string output);
	}
}
