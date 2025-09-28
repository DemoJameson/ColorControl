﻿using ColorControl.Shared.Common;

namespace ColorControl.Shared.Contracts;

public class CustomStepItem
{
    public string Name { get; private set; }
    public List<string> Items { get; private set; }
    public string SelectedItem { get; set; }

    public CustomStepItem(string name, List<string> items)
    {
        Name = name;
        Items = items;
    }
}

public class PresetStep
{
    public string Raw { get; set; }

    public string UpdatedRaw => $"{(ActionName != null ? $"{ActionName}({ActionValue})" : $"{Step}")}{(Delay > 0 ? $":{Delay}" : "")}{(Delay > 0 && Option != "" ? $":{Option}" : "")}";

    public string Step { get; set; }

    public string ActionName { get; set; }

    public string ActionValue { get; set; }

    public string[] Parameters { get; set; }

    public int Delay { get; set; }

    public string Option { get; set; }

    public override string ToString() => UpdatedRaw;

    public static PresetStep Parse(string step, int defaultDelay = 0, string defaultOption = "")
    {
        var parts = new List<string>();

        var index = step.IndexOf('(');
        var actionName = step;
        string actionValue = null;

        if (index > 0)
        {
            actionName = step[..index];
        }

        var index2 = step.IndexOf(")");

        if (index2 > 0)
        {
            if (index > 0)
            {
                actionValue = step.Substring(index + 1, (index2 - index - 1));
            }

            parts.Add(step.Substring(0, index2 + 1));
            if (index2 < step.Length - 2)
            {
                var extraParts = step.Substring(index2 + 1);
                if (extraParts[0] == ':')
                {
                    extraParts = extraParts.Substring(1);
                }
                parts.AddRange(extraParts.Split(":"));
            }
        }
        else
        {
            parts.AddRange(step.Split(":"));
        }

        var delay = defaultDelay;
        var cmd = defaultOption;
        var key = step;
        if (parts.Count >= 2)
        {
            delay = Utils.ParseInt(parts[1]);
            if (delay > 0)
            {
                key = parts[0];
            }
        }
        if (parts.Count >= 3)
        {
            cmd = parts[2];
        }

        var index3 = key.IndexOf("(");
        string[] parameters = null;
        if (index3 > -1)
        {
            var keyValue = key.Split('(');
            parameters = keyValue[1].Substring(0, keyValue[1].Length - 1).Split(';');
        }

        return new PresetStep
        {
            Raw = step,
            Step = key,
            ActionName = actionName,
            ActionValue = actionValue,
            Parameters = parameters,
            Delay = delay,
            Option = cmd
        };
    }
}
