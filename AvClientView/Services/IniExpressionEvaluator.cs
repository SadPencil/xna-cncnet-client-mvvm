using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;

using ClientCore;

using Serilog;

namespace AvClientView.Services;

/// <summary>
/// Evaluates arithmetic expressions in INI $X/$Y/$Width/$Height values.
/// Ported from ClientGUI/Parser.cs — same grammar, same error behavior (throws).
///
/// Supports: integers, +, -, *, /, (), UPPERCASE constants, and functions:
///   getX(name), getY(name), getWidth(name), getHeight(name),
///   getBottom(name), getRight(name), horizontalCenterOnParent(name)
/// Parameter aliases: $Self, $ParentControl
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
        // (EMPTY_SPACE_SIDES, EMPTY_SPACE_TOP, LOBBY_PANEL_SPACING, etc.)
        var parserSection = ClientConfiguration.Instance.GetParserConstants();
        if (parserSection != null)
        {
            foreach (var kvp in parserSection.Keys)
                constants[kvp.Key] = int.TryParse(kvp.Value, out int v) ? v : 0;
        }

        _instance = new IniExpressionEvaluator(constants);
        Serilog.Log.Debug($"[IniExpr] Initialized with {constants.Count} constants: {string.Join(", ", constants.Keys)}");
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
    /// Throws on invalid expressions (matching old Parser.cs behavior).
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

    // ---- Expression grammar (ported from ClientGUI/Parser.cs) ----

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
                throw new FormatException(
                    $"Unexpected character '{c}' when parsing input: {_input}");
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

        throw new FormatException(
            $"Unexpected character '{c}' when parsing input: {_input}");
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

        throw new KeyNotFoundException(
            $"Constant '{name}' not found. " +
            $"Please check [ParserConstants] section in the client settings file.");
    }

    private int GetFunctionValue()
    {
        string functionName = GetIdentifier();
        SkipWhitespace();
        ConsumeChar('(');
        string paramName = GetIdentifier();
        SkipWhitespace();
        ConsumeChar(')');

        // Resolve $Self / $ParentControl aliases.
        // In the old client, game lobby controls are direct children of the
        // game lobby window, so $ParentControl → window. In Avalonia, controls
        // are inside a Canvas (MainCanvas) which is inside the UserControl.
        // $ParentControl should resolve to the primary (UserControl), not the
        // intermediate Canvas, so getWidth/getHeight return the window size.
        if (paramName == "$ParentControl")
        {
            if (_primaryControl != null && !string.IsNullOrEmpty(_primaryControl.Name))
                paramName = _primaryControl.Name;
            else
                throw new FormatException(
                    $"$ParentControl used but no primary control set in expression: {_input}");
        }
        else if (paramName == "$Self")
        {
            if (_parsingControl != null && !string.IsNullOrEmpty(_parsingControl.Name))
                paramName = _parsingControl.Name;
            else
                throw new FormatException(
                    $"$Self used for control that has no name in expression: {_input}");
        }

        Control target = GetControl(paramName);

        switch (functionName)
        {
            case "getX":
                return (int)Canvas.GetLeft(target);

            case "getY":
                return (int)Canvas.GetTop(target);

            case "getWidth":
                return (int)(double.IsNaN(target.Width) ? target.Bounds.Width : target.Width);

            case "getHeight":
                return (int)(double.IsNaN(target.Height) ? target.Bounds.Height : target.Height);

            case "getBottom":
                {
                    double y = Canvas.GetTop(target);
                    double h = double.IsNaN(target.Height) ? target.Bounds.Height : target.Height;
                    return (int)(y + h);
                }

            case "getRight":
                {
                    double x = Canvas.GetLeft(target);
                    double w = double.IsNaN(target.Width) ? target.Bounds.Width : target.Width;
                    return (int)(x + w);
                }

            case "horizontalCenterOnParent":
                if (_parsingControl?.Parent is Control parentControl)
                {
                    double parentWidth = double.IsNaN(parentControl.Width)
                        ? parentControl.Bounds.Width : parentControl.Width;
                    double myWidth = double.IsNaN(_parsingControl.Width)
                        ? _parsingControl.Bounds.Width : _parsingControl.Width;
                    int centeredX = (int)((parentWidth - myWidth) / 2);
                    Canvas.SetLeft(_parsingControl, centeredX);
                    return centeredX;
                }
                throw new FormatException(
                    $"horizontalCenterOnParent used for control that has no parent: {_input}");

            default:
                throw new FormatException(
                    $"Unknown function '{functionName}' in expression: {_input}");
        }
    }

    private void ConsumeChar(char token)
    {
        if (IsEndOfInput() || _input[_tokenPlace] != token)
            throw new FormatException(
                $"Parse error: expected '{token}' in expression {_input}.");
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
    /// Finds a control by name. Throws KeyNotFoundException if not found,
    /// matching the old client's GetControl() behavior.
    /// </summary>
    private Control GetControl(string controlName)
    {
        if (_primaryControl != null && _primaryControl.Name == controlName)
            return _primaryControl;

        var found = FindInChildren(_primaryControl, controlName);
        if (found == null)
            throw new KeyNotFoundException(
                $"Control '{controlName}' not found while parsing input '{_input}'");

        return found;
    }

    private static Control? FindInChildren(Control? parent, string name)
    {
        if (parent == null)
            return null;

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
