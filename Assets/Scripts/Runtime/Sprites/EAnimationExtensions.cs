using System;
using System.Collections.Generic;
using System.Linq;

public static class EAnimationExtensions
{
    private static readonly Dictionary<EAnimation, string> _codeCache;

    static EAnimationExtensions()
    {
        _codeCache = new Dictionary<EAnimation, string>();

        foreach (var value in Enum.GetValues(typeof(EAnimation)).Cast<EAnimation>())
        {
            var memInfo = typeof(EAnimation).GetMember(value.ToString());
            var attribute = memInfo[0]
                .GetCustomAttributes(typeof(CodeAttribute), false)
                .FirstOrDefault() as CodeAttribute;

            _codeCache[value] = attribute?.Code ?? value.ToString().ToLower();
        }
    }

    public static string GetCode(this EAnimation anim) => _codeCache[anim];
}
