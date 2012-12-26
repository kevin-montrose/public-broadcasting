using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PublicBroadcasting.Impl
{
    internal class ToStringThunk
    {
        private static string EscapeString(string str)
        {
            return str.Replace("\"", @"\""");
        }

        private static string Indent(string str, int by)
        {
            var byStr = "";
            for (var i = 0; i < by; i++)
            {
                byStr += " ";
            }

            return Regex.Replace(str, @"^", @byStr, RegexOptions.Multiline);
        }

        private static List<dynamic> OrderDynamic(dynamic dyn)
        {
            var ret = new List<dynamic>();

            foreach (var kv in dyn)
            {
                ret.Add(new { Key = (string)kv.Key, Value = kv.Value });
            }

            ret = (List<dynamic>)ret.OrderBy(o => o.Key).ToList();

            return ret;
        }

        public static string Call(dynamic val)
        {
            if (val == null)
            {
                return "null";
            }

            if (val is string)
            {
                return "\"" + EscapeString((string)val) + "\"";
            }

            if (Nullable.GetUnderlyingType(val.GetType()) != null)
            {
                val = val.Value;
            }

            var valType = (Type)val.GetType();

            if (valType == typeof(byte) || valType == typeof(sbyte) || valType == typeof(short) || valType == typeof(ushort) || valType == typeof(int) || valType == typeof(uint) ||
                valType == typeof(long) || valType == typeof(ulong) || valType == typeof(float) || valType == typeof(double) || valType == typeof(decimal) || valType == typeof(bool) ||
                valType == typeof(char))
            {
                return val.ToString();
            }

            if (val is Guid)
            {
                return val.ToString("D");
            }

            if (val is Uri)
            {
                if (val.IsAbsoluteUri)
                {
                    return val.AbsoluteUri;
                }
                else
                {
                    return val.ToString();
                }
            }

            if (val is TimeSpan)
            {
                return val.ToString("c");
            }

            if (val is DateTime)
            {
                return val.ToString("u");
            }

            if (valType.IsList())
            {
                var parts = new List<string>();

                foreach (var part in val)
                {
                    parts.Add(Call(part));
                }

                var containsType = valType.GetListInterface().GetGenericArguments()[0];

                if (containsType.IsSimple())
                {
                    return "[" + string.Join(", ", parts) + "]";
                }
                else
                {
                    return "[" + Environment.NewLine + Indent(string.Join("," + Environment.NewLine, parts), 1) + Environment.NewLine + "]";
                }
            }

            if (valType.IsDictionary())
            {
                var parts = new List<string>();

                foreach (var kv in val)
                {
                    var dKey = (string)Call(kv.Key);
                    var dVal = (string)Call(kv.Value);

                    if (dKey.Contains(Environment.NewLine) || dVal.Contains(Environment.NewLine))
                    {
                        parts.Add("{" + Environment.NewLine + Indent(dKey, 2) + "" + Environment.NewLine + "   ->" + Environment.NewLine + Indent(dVal, 2) + Environment.NewLine + " }");
                    }
                    else
                    {
                        parts.Add("{" + dKey + " -> " + dVal + "}");
                    }
                }

                parts = parts.OrderBy(o => o).ToList();

                return "{" + Environment.NewLine + " " + string.Join("," + Environment.NewLine + " ", parts) + Environment.NewLine + "}";
            }

            var ret = new StringBuilder();
            var first = true;

            ret.AppendLine("{");
            var inOrder = OrderDynamic(val);
            foreach (var kv in inOrder)
            {
                var propName = (string)kv.Key;
                var propVal = kv.Value;

                if (!first)
                {
                    ret.AppendLine(",");
                }

                first = false;

                ret.Append(" ");
                ret.Append(propName);
                ret.Append(": ");

                var propValStr = Call(propVal);

                if (propValStr.Contains(Environment.NewLine))
                {
                    ret.AppendLine();
                    ret.Append(Indent(propValStr, 2));
                }
                else
                {
                    ret.Append(propValStr);
                }
            }

            ret.AppendLine();
            ret.Append("}");

            return ret.ToString();
        }
    }
}
