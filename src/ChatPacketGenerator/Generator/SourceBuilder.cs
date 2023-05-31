using System.Text;

namespace ChatPacketGenerator.Generator;

public sealed class SourceBuilder
{
    private const int IndentSize = 4;
    private readonly StringBuilder _sb = new();

    private int _indent;
    private bool _startOfLine = true;

    public SourceBuilder StartBlock()
    {
        AppendIndent();
        _sb.AppendLine("{");
        _startOfLine = true;
        _indent += IndentSize;
        return this;
    }

    public SourceBuilder EndBlock(bool addSemicolon = false)
    {
        _indent -= IndentSize;
        AppendIndent();
        _sb.AppendLine(addSemicolon ? "};" : "}");
        _startOfLine = true;
        return this;
    }

    public SourceBuilder EndAllBlocks()
    {
        while (_indent > 0) EndBlock();
        return this;
    }

    public SourceBuilder Append(string text)
    {
        AppendIndent();

        _sb.Append(text);
        return this;
    }

    public SourceBuilder AppendLine()
    {
        _sb.AppendLine();
        _startOfLine = true;
        return this;
    }

    public SourceBuilder AppendLine(string line)
    {
        AppendIndent();

        _sb.AppendLine(line);
        _startOfLine = true;
        return this;
    }

    private SourceBuilder AppendIndent()
    {
        if (!_startOfLine) return this;
        _sb.Append(' ', _indent);
        _startOfLine = false;
        return this;
    }

    public override string ToString() => _sb.ToString();
}