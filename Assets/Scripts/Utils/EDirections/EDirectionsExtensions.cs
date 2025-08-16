using System;
using System.Collections.Generic;
using System.Linq;

public static class EDirectionsExtensions
{
    private static readonly Dictionary<EDirection, string> _codeCache;

    static EDirectionsExtensions()
    {
        _codeCache = new Dictionary<EDirection, string>();

        foreach (var value in Enum.GetValues(typeof(EDirection)).Cast<EDirection>())
        {
            var memInfo = typeof(EDirection).GetMember(value.ToString());
            var attribute = memInfo[0]
                .GetCustomAttributes(typeof(CodeAttribute), false)
                .FirstOrDefault() as CodeAttribute;

            _codeCache[value] = attribute?.Code ?? value.ToString();
        }
    }

    public static string GetCode(this EDirection dir) => _codeCache[dir];
}