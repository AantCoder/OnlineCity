using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RimWorldOnlineCity
{
    public class Restricted
    {

        public static string ToStringRestricted(object obj, HashSet<string> excludeTypes = null)
        {
            if (obj == null) return "";
            return Process(obj, 10
                , string.Empty, new Dictionary<object, int>(), excludeTypes ?? new HashSet<string>());
        }

        public static string ToStringRestrictedShort(object obj, HashSet<string> excludeTypes = null)
        {
            if (obj == null) return "";
            return Process(obj, 3
                , string.Empty, new Dictionary<object, int>(), excludeTypes ?? new HashSet<string>());
        }

        static string PrefLineTab = "\t";
        static int LineLength = 50;

        static string Process(object obj, int level, string prefLine, Dictionary<object, int> forParentLink, HashSet<string> excludeTypes)
        {
            try
            {
                if (obj == null) return "";
                Type type = obj.GetType();
                if (excludeTypes.Contains(type.Name)) return "<skip>";
                if (type == typeof(DateTime))
                {
                    return ((DateTime)obj).ToString("g");
                }
                else if (type.IsValueType || type == typeof(string))
                {
                    return obj.ToString();
                }
                else if (type.IsArray || obj is IEnumerable)
                {
                    string elementType = null;
                    Array array = null;

                    if (type.IsArray)
                    {
                        var tt = Type.GetType(type.FullName.Replace("[]", string.Empty));
                        if (tt != null)
                        {
                            elementType = tt.ToString();
                            if (excludeTypes.Contains(tt.Name)) return "<skips>";
                        }
                        //else return "<EEEEEEE" + type.FullName;
                        array = obj as Array;
                    }
                    else
                    {
                        //как лучше узнать кол-во и тип?
                        int cnt = 0;
                        foreach (var o in obj as IEnumerable) cnt++;
                        array = Array.CreateInstance(typeof(object), cnt);
                        int i = 0;
                        foreach (var o in obj as IEnumerable)
                        {
                            if (elementType == null && o != null)
                            {
                                var tt = o.GetType();
                                if (excludeTypes.Contains(tt.Name)) return "<skips>";
                                elementType = tt.ToString();
                            }
                            array.SetValue(o, i++);
                        }
                    }

                    if (elementType == null) elementType = "Object";

                    if (excludeTypes.Contains(elementType)) return "<skips>";

                    var info = "<" + elementType + "[" + array.Length.ToString() + "]" + ">";

                    if (level == 0)
                    {
                        return info + "[...]";
                    }

                    if (array.Length > 0)
                    {
                        var ress = new string[array.Length];
                        var resl = 0;
                        for (int i = 0; i < array.Length; i++)
                        {
                            ress[i] = Process(array.GetValue(i), level - 1, prefLine + PrefLineTab, forParentLink, excludeTypes);
                            resl += ress[i].Length;
                        }

                        if (resl < LineLength)
                        {
                            var res = info + "[" + ress[0];
                            for (int i = 1; i < ress.Length; i++)
                            {
                                res += ", " + ress[i];
                            }
                            return res + "]";
                        }
                        else
                        {
                            var res = info + "["
                                + Environment.NewLine + prefLine + ress[0];
                            for (int i = 1; i < ress.Length; i++)
                            {
                                res += ", "
                                    + Environment.NewLine + prefLine + ress[i];
                            }
                            return res + Environment.NewLine + prefLine + "]";
                        }
                    }
                    return info + "[]";
                }
                else if (obj is Type) return "<Type>" + obj.ToString();
                else if (type.IsClass)
                {
                    var info = "<" + type.Name + ">";

                    if (forParentLink.ContainsKey(obj))
                    {
                        if (forParentLink[obj] >= level)
                            return info + "{duplicate}";
                        else
                            forParentLink[obj] = level;
                    }
                    else
                        forParentLink.Add(obj, level);

                    if (level == 0)
                    {
                        return info + "{...}";
                    }

                    FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                                BindingFlags.NonPublic | BindingFlags.Instance);

                    if (fields.Length > 0)
                    {
                        var ress = new string[fields.Length];
                        var resl = 0;
                        for (int i = 0; i < fields.Length; i++)
                        {
                            object fieldValue = fields[i].GetValue(obj);
                            ress[i] = fieldValue == null
                                ? fields[i].Name + ": null"
                                : fields[i].Name + ": " + Process(fieldValue, level - 1, prefLine + PrefLineTab, forParentLink, excludeTypes);
                            resl += ress[i].Length;
                        }

                        if (resl < LineLength)
                        {
                            var res = info + "{" + ress[0];
                            for (int i = 1; i < ress.Length; i++)
                            {
                                res += ", " + ress[i];
                            }
                            return res + "}";
                        }
                        else
                        {
                            var res = info + "{"
                                + Environment.NewLine + prefLine + ress[0];
                            for (int i = 1; i < ress.Length; i++)
                            {
                                res += ", "
                                    + Environment.NewLine + prefLine + ress[i];
                            }
                            return res + Environment.NewLine + prefLine + "}";
                        }
                    }
                    return info + "{}";
                }
                else
                    throw new ArgumentException("Unknown type");
            }
            catch
            {
                return "<exception>";
            }
        }

    }

}
