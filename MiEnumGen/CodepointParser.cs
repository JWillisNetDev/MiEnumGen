using Humanizer;
using System.Text;

namespace MiEnumGen;

public record CodepointToken(string Identifier, string Codepoint);

public class CodepointParser
{
	private readonly StreamReader _reader;

	private string? _lineBuffer;
	private int _index;

	private char Current
	{
		get
		{
			if (_lineBuffer is null || _index >= _lineBuffer.Length)
			{
				return '\0';
			}

			return _lineBuffer[_index];
		}
	}

	private char Next
	{
		get
		{
			if (_lineBuffer is null || _index + 1 >= _lineBuffer.Length)
			{
				return '\0';
			}

			return _lineBuffer[_index + 1];
		}
	}

	private bool MoveNext()
	{
		if (_lineBuffer is null || ++_index >= _lineBuffer.Length)
		{
			return false;
		}

		return true;
	}

	public CodepointParser(Stream inputStream)
	{
		if (!inputStream.CanRead)
		{
			throw new ArgumentException("Input stream must be readable.", nameof(inputStream));
		}

		_reader = new StreamReader(inputStream);
	}

	public async IAsyncEnumerable<CodepointToken> Parse()
	{
		while (!_reader.EndOfStream)
		{
			yield return await ParseLineAsync();
		}
	}

	private async Task<CodepointToken> ParseLineAsync()
	{
		_lineBuffer = await _reader.ReadLineAsync();
		_index = 0;

		string? identifier = ParseCleanIdentifier();
		if (identifier is null)
		{
			throw new InvalidOperationException("Identifier cannot be null.");
		}

		string value = ParseValue();

		return new CodepointToken(identifier, value);
	}

	private string? ParseCleanIdentifier()
	{
		if (_lineBuffer is null || _lineBuffer.Length <= 0)
		{
			return null;
		}

		bool isNextUpper = true;
		var identifierBuffer = new StringBuilder();

		if (char.IsDigit(Current))
		{
			string numberString = ParseHumanNumber();
			identifierBuffer.Append(numberString);
			MoveNext();
		}

		while (Current != '\0')
		{
			if (Current == '_')
			{
				isNextUpper = true;
				MoveNext();
				continue;
			}

			if (Current == ' ')
			{
				MoveNext();
				break;
			}

			char currentChar = isNextUpper ? char.ToUpper(Current) : char.ToLower(Current);
			identifierBuffer.Append(currentChar);
			isNextUpper = false;
			MoveNext();
		}

		return identifierBuffer.ToString();
	}

	private string ParseValue()
	{
		var valueBuffer = new StringBuilder();
		while (Current != '\0')
		{
			valueBuffer.Append(Current);
			MoveNext();
		}

		return valueBuffer.ToString();
	}

	private string ParseHumanNumber()
	{
		var numStringBuffer = new StringBuilder(Current.ToString());
		while (char.IsDigit(Next))
		{
			MoveNext();
			numStringBuffer.Append(Current.ToString());
		}

		string numberString = numStringBuffer.ToString();
		int number = int.Parse(numberString);
		return number.ToWords().Dehumanize();
	}
}
