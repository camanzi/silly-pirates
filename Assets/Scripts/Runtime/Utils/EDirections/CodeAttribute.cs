using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Field)]
public class CodeAttribute : Attribute
{
    public string Code { get; }
    public CodeAttribute(string code) => Code = code;
}