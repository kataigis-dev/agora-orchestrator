namespace Agora.Cli;

/// <summary>Signals that input ended before a prompt could be satisfied.</summary>
public sealed class PrompterAbortException : Exception;

/// <summary>
/// Console question/answer primitives over an injected reader/writer: a single prompt with a default,
/// a required value, a space/comma-separated list, a bounded subset, a non-negative integer, a choice
/// among options, and yes/no — each re-prompting on invalid input. Extracted from the wizard so this
/// logic (e.g. re-asking on a bad integer) is testable directly, not only end-to-end through the whole
/// wizard. Reads past end-of-input throw <see cref="PrompterAbortException"/>.
/// </summary>
public sealed class Prompter
{
    private readonly TextReader _input;
    private readonly TextWriter _output;

    /// <summary>Creates a prompter over the given input and output streams.</summary>
    public Prompter(TextReader input, TextWriter output)
    {
        _input = input;
        _output = output;
    }

    /// <summary>Writes a blank line to the output (for section spacing).</summary>
    public void WriteLine() => _output.WriteLine();

    /// <summary>Writes a line to the output (for section headers and messages).</summary>
    public void WriteLine(string text) => _output.WriteLine(text);

    /// <summary>Prompts once, returning the entered value or the default on a blank line.</summary>
    public string Ask(string prompt, string @default = "")
    {
        var suffix = @default.Length > 0 ? $" [{@default}]" : "";
        _output.Write($"{prompt}{suffix}: ");
        var line = ReadLine();
        return line.Length == 0 ? @default : line;
    }

    /// <summary>Prompts repeatedly until a non-empty value is entered.</summary>
    public string Required(string prompt)
    {
        while (true)
        {
            var value = Ask(prompt);
            if (value.Length > 0) return value;
            _output.WriteLine("    a value is required.");
        }
    }

    /// <summary>Prompts for a whitespace/comma-separated list of values.</summary>
    public List<string> AskList(string prompt)
        => Ask(prompt)
            .Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    /// <summary>Prompts for a list, re-asking until every value is within <paramref name="allowed"/>.</summary>
    public List<string> AskSubset(string prompt, IReadOnlyList<string> allowed)
    {
        while (true)
        {
            var values = AskList(prompt);
            var invalid = values.Where(v => !allowed.Contains(v)).ToList();
            if (invalid.Count == 0) return values;
            _output.WriteLine($"    not in tools: {string.Join(", ", invalid)}");
        }
    }

    /// <summary>Prompts for a non-negative integer, re-asking on invalid input.</summary>
    public int AskInt(string prompt, int @default)
    {
        while (true)
        {
            var raw = Ask(prompt, @default.ToString());
            if (int.TryParse(raw, out var n) && n >= 0) return n;
            _output.WriteLine("    enter a non-negative integer.");
        }
    }

    /// <summary>Prompts for one of <paramref name="options"/>, re-asking until a valid choice (or a
    /// blank line when <paramref name="allowBlank"/> is set).</summary>
    public string Choice(string prompt, IReadOnlyList<string> options, string @default, bool allowBlank = false)
    {
        var rendered = $"{prompt} [{string.Join("/", options)}]";
        while (true)
        {
            var value = Ask(rendered, @default);
            if (value.Length == 0 && allowBlank) return "";
            if (options.Contains(value)) return value;
            _output.WriteLine($"    choose one of: {string.Join(", ", options)}");
        }
    }

    /// <summary>Prompts for a yes/no answer with the given default.</summary>
    public bool YesNo(string prompt, bool defaultYes)
    {
        var value = Ask($"{prompt} [{(defaultYes ? "Y/n" : "y/N")}]", defaultYes ? "y" : "n");
        return value.StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads and trims a line; throws <see cref="PrompterAbortException"/> at end of input.</summary>
    private string ReadLine()
    {
        var line = _input.ReadLine();
        if (line is null) throw new PrompterAbortException();
        return line.Trim();
    }
}
