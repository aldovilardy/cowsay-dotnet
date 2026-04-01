using System.Text.RegularExpressions;

namespace CowSay.Core.Services;

/// <summary>
/// A mini Perl-subset interpreter that processes executable lines in cowfiles
/// before the heredoc template. Supports the small set of Perl operations
/// found in standard cowfiles: chop(), string concatenation (.=),
/// repetition (x operator), string interpolation, and unless conditionals.
/// </summary>
public static partial class CowfilePreprocessor
{
    /// <summary>
    /// Processes any Perl-like code lines that appear before the heredoc in a cowfile template,
    /// returning the potentially modified eyes and tongue values.
    /// </summary>
    /// <param name="template">The full cowfile template text.</param>
    /// <param name="eyes">The current eyes value.</param>
    /// <param name="tongue">The current tongue value.</param>
    /// <returns>A tuple of (eyes, tongue) after processing any pre-heredoc Perl code.</returns>
    public static (string Eyes, string Tongue) Process(string template, string eyes, string tongue)
    {
        var vars = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["eyes"] = eyes,
            ["tongue"] = tongue
        };

        var heredocPattern = HeredocStartRegex();
        var lines = template.Replace("\r\n", "\n").Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Stop processing when we hit the heredoc start
            if (heredocPattern.IsMatch(line))
                break;

            // Skip comments and blank lines
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            // Try to process the line as a Perl statement
            ProcessLine(line, vars);
        }

        return (vars["eyes"], vars["tongue"]);
    }

    /// <summary>
    /// Attempts to interpret a single line of Perl-like code.
    /// </summary>
    private static void ProcessLine(string line, Dictionary<string, string> vars)
    {
        // Remove trailing semicolon
        if (line.EndsWith(';'))
            line = line[..^1].TrimEnd();

        // Pattern: $var = "..." unless ($condition)
        // e.g. $eyes = ".." unless ($eyes)
        var unlessMatch = UnlessRegex().Match(line);
        if (unlessMatch.Success)
        {
            var varName = unlessMatch.Groups[1].Value;
            var value = EvaluateExpression(unlessMatch.Groups[2].Value.Trim(), vars);
            var condVarName = unlessMatch.Groups[3].Value;

            // "unless ($var)" means: execute only if $var is falsy (empty/null)
            if (!vars.TryGetValue(condVarName, out var condVal) || string.IsNullOrEmpty(condVal))
            {
                vars[varName] = value;
            }
            return;
        }

        // Pattern: $var .= expr
        // e.g. $eyes .= ($extra x 2)
        // e.g. $eyes .= " $other_eye"
        var concatMatch = ConcatAssignRegex().Match(line);
        if (concatMatch.Success)
        {
            var varName = concatMatch.Groups[1].Value;
            var expr = concatMatch.Groups[2].Value.Trim();
            var value = EvaluateExpression(expr, vars);
            vars.TryGetValue(varName, out var current);
            vars[varName] = (current ?? "") + value;
            return;
        }

        // Pattern: $var = expr
        // e.g. $extra = chop($eyes)
        var assignMatch = AssignRegex().Match(line);
        if (assignMatch.Success)
        {
            var varName = assignMatch.Groups[1].Value;
            var expr = assignMatch.Groups[2].Value.Trim();
            var value = EvaluateExpression(expr, vars);
            vars[varName] = value;
            return;
        }
    }

    /// <summary>
    /// Evaluates a simple Perl expression and returns its string result.
    /// </summary>
    private static string EvaluateExpression(string expr, Dictionary<string, string> vars)
    {
        // chop($var) - removes and returns the last character of $var
        var chopMatch = ChopRegex().Match(expr);
        if (chopMatch.Success)
        {
            var varName = chopMatch.Groups[1].Value;
            if (vars.TryGetValue(varName, out var val) && val.Length > 0)
            {
                var lastChar = val[^1].ToString();
                vars[varName] = val[..^1];
                return lastChar;
            }
            return "";
        }

        // ($var x N) - repeat string N times
        // e.g. ($extra x 2)
        var repeatMatch = RepeatRegex().Match(expr);
        if (repeatMatch.Success)
        {
            var inner = repeatMatch.Groups[1].Value.Trim();
            var innerValue = EvaluateExpression(inner, vars);
            if (int.TryParse(repeatMatch.Groups[2].Value, out var count))
            {
                return string.Concat(Enumerable.Repeat(innerValue, count));
            }
            return innerValue;
        }

        // "string with $var interpolation"
        if (expr.StartsWith('"') && expr.EndsWith('"'))
        {
            var inner = expr[1..^1];
            return InterpolateString(inner, vars);
        }

        // $var reference
        if (expr.StartsWith('$'))
        {
            var varName = expr[1..];
            return vars.TryGetValue(varName, out var val) ? val : "";
        }

        // Bare string (unquoted)
        return expr;
    }

    /// <summary>
    /// Performs Perl-style string interpolation, replacing $varname references with their values.
    /// </summary>
    private static string InterpolateString(string s, Dictionary<string, string> vars)
    {
        return VarRefRegex().Replace(s, match =>
        {
            var varName = match.Groups[1].Value;
            return vars.TryGetValue(varName, out var val) ? val : "";
        });
    }

    [GeneratedRegex(@"^\$the_cow\s*=\s*<<""?EOC""?")]
    private static partial Regex HeredocStartRegex();

    [GeneratedRegex(@"^\$(\w+)\s*=\s*(.+?)\s+unless\s+\(\$(\w+)\)$")]
    private static partial Regex UnlessRegex();

    [GeneratedRegex(@"^\$(\w+)\s*\.=\s*(.+)$")]
    private static partial Regex ConcatAssignRegex();

    [GeneratedRegex(@"^\$(\w+)\s*=\s*(.+)$")]
    private static partial Regex AssignRegex();

    [GeneratedRegex(@"^chop\(\$(\w+)\)$")]
    private static partial Regex ChopRegex();

    [GeneratedRegex(@"^\((.+?)\s+x\s+(\d+)\)$")]
    private static partial Regex RepeatRegex();

    [GeneratedRegex(@"\$(\w+)")]
    private static partial Regex VarRefRegex();
}
