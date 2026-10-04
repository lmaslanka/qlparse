using System.Text;
using Xunit.Sdk;

namespace QlParse.ArchitectureTests.Support;

internal static class RuleAssert
{
    public static void Check(Rule rule, IEnumerable<string> violations)
    {
        var items = violations.Distinct(StringComparer.Ordinal).OrderBy(v => v, StringComparer.Ordinal).ToList();
        if (items.Count == 0)
        {
            return;
        }

        var message = new StringBuilder()
            .AppendLine($"[{rule.Id}] {rule.Title}")
            .AppendLine()
            .AppendLine("Violations:");

        foreach (var item in items)
        {
            message.AppendLine($"  - {item}");
        }

        message
            .AppendLine()
            .AppendLine($"How to fix: {rule.Guidance}")
            .Append($"See ARCHITECTURE.md § {rule.Id}");

        throw new XunitException(message.ToString());
    }
}
