using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;

using ClientCore;

using Serilog;

namespace AvClientView.Services;

/// <summary>
/// Evaluates arithmetic expressions in INI $X/$Y/$Width/$Height values.
/// Ported from ClientGUI/Parser.cs — matches the same grammar and functions:
///   getX(name), getY(name), getWidth(name), getHeight(name),
///   getBottom(name), getRight(name), horizontalCenterOnParent(name)
/// Supports integers, +, -, *, /, (), UPPERCASE constants, and
/// $Self / $ParentControl parameter aliases.
/// </summary>
public class IniExpressionEvaluator
{
    private const int CHAR_VALUE_ZERO = 48;

    private static IniExpressionEvaluator? _instance;
    public static IniExpressionEvaluator Instance => _instance ?? throw new InvalidOperationException(
        "IniExpressionEvaluator not initialized. Call Initialize() first.");

    private readonly Dictionary<string, int> _constants;
    private string _input = string.Empty;
    private int _tokenPlace;
    private Control? _primaryControl;
    private Control? _parsingControl;

    private IniExpressionEvaluator(Dictionary<string, int> constants)
    {
        _constants = constants;
    }

    /// <summary>
    /// Initializes the evaluator with design resolution and parser constants from INI.
    /// Must be called once before evaluating expressions.
    /// </summary>
    public static void Initialize(int designWidth, int designHeight)
    {
        if (_instance != null)
            return;

        var constants = new Dictionary<string, int>
        {
            ["RESOLUTION_WIDTH"] = designWidth,
            ["RESOLUTION_HEIGHT"] = designHeight
        };

        // Load parser constants from DTACnCNetClient.ini [ParserConstants]
        var parserSection = ClientConfiguration.Instance.GetParserConstants();
        if (parserSection != null)
        {
            foreach (var kvp in parserSection.Keys)
                constants[kvp.Key] = int.TryParse(kvp.Value, out int v) ? v : 0;
        }

        _instance = new IniExpressionEvaluator(constants);
    }

    /// <summary>
    /// Sets the primary (root) control for control name resolution.
    /// </summary>
    public void SetPrimaryControl(Control? primaryControl)
    {
        _primaryControl = primaryControl;
    }

    /// <summary>
    /// Evaluates an expression string and returns the integer result.
    /// The parsingControl is the control being parsed (for $Self/$ParentControl).
    /// </summary>
    public int Evaluate(string expression, Control? parsingControl)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return 0;

        _parsingControl = parsingControl;
        _input = expression;
        _tokenPlace = 0;
        return GetExprValue();
    }

    // ---- Expression grammar ----

    private int GetExprValue()
    {
        int value = 0;

        while (true)
        {
            SkipWhitespace();

            if (IsEndOfInput())
                return value;

            char c = _input[_tokenPlace];

            if (char.IsDigit(c))
            {
                value = GetInt();
            }
            else if (c == '+')
            {
                _tokenPlace++;
                value += GetNumericalValue();
            }
            else if (c == '-')
            {
                _tokenPlace++;
                value -= GetNumericalValue();
            }
            else if (c == '/')
            {
                _tokenPlace++;
                value /= GetExprValue();
            }
            else if (c == '*')
            {
                _tokenPlace++;
                value *= GetExprValue();
            }
            else if (c == '(')
            {
                _tokenPlace++;
                value = GetExprValue();
            }
            else if (c == ')')
            {
                _tokenPlace++;
                return value;
            }
            else if (char.IsUpper(c))
            {
                value = GetConstantValue();
            }
            else if (char.IsLower(c))
            {
                value = GetFunctionValue();
            }
            else
            {
                // Unknown character — skip and try to continue
                Log.Warning($"[IniExpr] Unexpected character '{c}' in expression: {_input}");
                _tokenPlace++;
            }
        }
    }

    private int GetNumericalValue()
    {
        SkipWhitespace();

        if (IsEndOfInput())
            return 0;

        char c = _input[_tokenPlace];

        if (char.IsDigit(c))
            return GetInt();
        else if (char.IsUpper(c))
            return GetConstantValue();
        else if (char.IsLower(c))
            return GetFunctionValue();
        else if (c == '(')
        {
            _tokenPlace++;
            return GetExprValue();
        }

        Log.Warning($"[IniExpr] Unexpected character '{c}' in expression: {_input}");
        _tokenPlace++;
        return 0;
    }

    // ---- Helpers ----

    private void SkipWhitespace()
    {
        while (!IsEndOfInput())
        {
            char c = _input[_tokenPlace];
            if (c == ' ' || c == '\r' || c == '\n')
                _tokenPlace++;
            else
                break;
        }
    }

    private string GetIdentifier()
    {
        string name = "";
        while (!IsEndOfInput())
        {
            char c = _input[_tokenPlace];
            if (char.IsWhiteSpace(c))
                break;
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '$' && c != '.')
                break;
            name += c;
            _tokenPlace++;
        }
        return name;
    }

    private int GetConstantValue()
    {
        string name = GetIdentifier();
        if (_constants.TryGetValue(name, out int value))
            return value;

        Log.Warning($"[IniExpr] Unknown constant '{name}' in expression: {_input}");
        return 0;
    }

    private int GetFunctionValue()
    {
        string functionName = GetIdentifier();
        SkipWhitespace();
        ConsumeChar('(');
        string paramName = GetIdentifier();
        SkipWhitespace();
        ConsumeChar(')');

        // Resolve $Self / $ParentControl aliases
        if (paramName == "$ParentControl")
        {
            if (_parsingControl?.Parent is Control parent && !string.IsNullOrEmpty(parent.Name))
                paramName = parent.Name;
            else
            {
                Log.Warning($"[IniExpr] $ParentControl used but parent is null or unnamed in: {_input}");
                return 0;
            }
        }
        else if (paramName == "$Self")
        {
            if (_parsingControl != null && !string.IsNullOrEmpty(_parsingControl.Name))
                paramName = _parsingControl.Name;
            else
            {
                Log.Warning($"[IniExpr] $Self used but parsing control is null or unnamed in: {_input}");
                return 0;
            }
        }

        Control? target = FindControlByName(_primaryControl, paramName);

        switch (functionName)
        {
            case "getX":
                return target != null ? (int)Canvas.GetLeft(target) : 0;

            case "getY":
                return target != null ? (int)Canvas.GetTop(target) : 0;

            case "getWidth":
                return target != null ? (int)(double.IsNaN(target.Width) ? target.Bounds.Width : target.Width) : 0;

            case "getHeight":
                return target != null ? (int)(double.IsNaN(target.Height) ? target.Bounds.Height : target.Height) : 0;

            case "getBottom":
                if (target != null)
                {
                    double y = Canvas.GetTop(target);
                    double h = double.IsNaN(target.Height) ? target.Bounds.Height : target.Height;
                    return (int)(y + h);
                }
                return 0;

            case "getRight":
                if (target != null)
                {
                    double x = Canvas.GetLeft(target);
                    double w = double.IsNaN(target.Width) ? target.Bounds.Width : target.Width;
                    return (int)(x + w);
                }
                return 0;

            case "horizontalCenterOnParent":
                if (_parsingControl != null && _parsingControl.Parent is Control parentControl)
                {
                    double parentWidth = double.IsNaN(parentControl.Width) ? parentControl.Bounds.Width : parentControl.Width;
                    double myWidth = double.IsNaN(_parsingControl.Width) ? _parsingControl.Bounds.Width : _parsingControl.Width;
                    int centeredX = (int)((parentWidth - myWidth) / 2);
                    Canvas.SetLeft(_parsingControl, centeredX);
                    return centeredX;
                }
                return 0;

            default:
                Log.Warning($"[IniExpr] Unknown function '{functionName}' in expression: {_input}");
                return 0;
        }
    }

    private void ConsumeChar(char token)
    {
        if (IsEndOfInput() || _input[_tokenPlace] != token)
        {
            Log.Warning($"[IniExpr] Expected '{token}' in expression: {_input}");
            return;
        }
        _tokenPlace++;
    }

    private int GetInt()
    {
        int value = 0;
        while (!IsEndOfInput() && char.IsDigit(_input[_tokenPlace]))
        {
            value = (value * 10) + _input[_tokenPlace] - CHAR_VALUE_ZERO;
            _tokenPlace++;
        }
        return value;
    }

    private bool IsEndOfInput() => _tokenPlace >= _input.Length;

    // ---- Avalonia control tree search ----

    /// <summary>
    /// Recursively finds a control by name in the Avalonia visual tree.
    /// </summary>
    private static Control? FindControlByName(Control? root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        if (root.Name == name)
            return root;

        return FindInChildren(root, name);
    }

    private static Control? FindInChildren(Control parent, string name)
    {
        foreach (var child in GetVisualChildren(parent))
        {
            if (child.Name == name)
                return child;

            var found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static IEnumerable<Control> GetVisualChildren(Control parent)
    {
        if (parent is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control c)
                    yield return c;
            }
        }
        else if (parent is ContentControl cc && cc.Content is Control content)
        {
            yield return content;
        }
        else if (parent is Decorator d && d.Child is Control decorChild)
        {
            yield return decorChild;
        }
    }
}
