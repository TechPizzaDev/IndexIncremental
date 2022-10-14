using System.Collections.Generic;
using System.Text;

namespace IndexIncremental;

public class StackStringBuilder
{
    public Stack<string> Stack { get; }
    public StringBuilder Builder { get; }

    public StackStringBuilder(Stack<string> stack, StringBuilder builder)
    {
        Stack = stack;
        Builder = builder;
    }

    public StackStringBuilder() : this(new Stack<string>(), new StringBuilder())
    {
    }

    public string ToStringAndClear()
    {
        string str = Builder.ToString();
        Builder.Clear();
        Stack.Clear();
        return str;
    }
}
