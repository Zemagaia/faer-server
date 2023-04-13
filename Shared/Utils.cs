using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using common.resources;
using Dynamitey;
using Ionic.Zlib;
using Newtonsoft.Json;

namespace common
{
    public static class Utils
    {
        public static JsonSerializerSettings SerializerSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
            };
        }
        
        public static byte[] ToBytes<T>(this T[] array, bool server)
        {
            if (array == null) return new byte[0];
            using (var ms = new MemoryStream())
            using (var wtr = new NWriter(ms))
            {
                var len = array.Length;
                wtr.Write((short)len);
                for (var i = 0; i < len; i++)
                {
                    byte[] bytes;
                    if (typeof(T[]) == typeof(ItemData[])) 
                        bytes = (array[i] as ItemData).Export(server);
                    else if (typeof(T[]) == typeof(PetData[]))
                        bytes = (array[i] as PetData).Export();
                    else if (typeof(T[]) == typeof(QuestData[]))
                        bytes = (array[i] as QuestData).Export(server);
                    else if (typeof(T[]) == typeof(AcceptedQuestData[]))
                        bytes = (array[i] as AcceptedQuestData).Export(server);
                    else throw new NotSupportedException();
                    wtr.Write((short)bytes.Length);
                    wtr.Write(bytes);
                }

                return ms.ToArray();
            }
        }
        
        public static object FromBytes(this byte[] array, Type type)
        {
            using (var ms = new MemoryStream(array))
            using (var rdr = new NReader(ms))
            {
                var len = rdr.ReadInt16();
                object[] data;
                if (type == typeof(ItemData[]))
                {
                    data = new ItemData[len];
                    for (var i = 0; i < len; i++)
                        data[i] = new ItemData(rdr.ReadBytes(rdr.ReadInt16()));
                }
                else if (type == typeof(PetData[]))
                {
                    data = new PetData[len];
                    for (var i = 0; i < len; i++)
                        data[i] = new PetData(rdr.ReadBytes(rdr.ReadInt16()));
                }
                else if (type == typeof(QuestData[]))
                {
                    data = new QuestData[len];
                    for (var i = 0; i < len; i++)
                        data[i] = new QuestData(rdr.ReadBytes(rdr.ReadInt16()));
                }
                else if (type == typeof(AcceptedQuestData[]))
                {
                    data = new AcceptedQuestData[len];
                    for (var i = 0; i < len; i++)
                        data[i] = new AcceptedQuestData(rdr.ReadBytes(rdr.ReadInt16()));
                }
                else throw new NotSupportedException();
                return data;
            }
        }

        public static object ReadObject(this NReader rdr, Type type)
        {
            var len = rdr.ReadInt16();
            object[] data;
            if (type == typeof(ItemData[]))
            {
                data = new ItemData[len];
                for (var i = 0; i < len; i++)
                    data[i] = new ItemData(rdr.ReadBytes(rdr.ReadInt16()));
            }
            else throw new NotSupportedException();

            return data;
        }

        public static byte[] ToUtf8Bytes(this string val)
        {
            return Encoding.UTF8.GetBytes(val);
        }

        public static XElement AddAttribute(this XElement elem, XName name, object value)
        {
            elem.SetAttributeValue(name, value);
            return elem;
        }

        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static bool IsInt(this string str)
        {
            int dummy;
            return Int32.TryParse(str, out dummy);
        }

        public static int ToInt32(this string str)
        {
            return FromString(str);
        }

        public static T[] ResizeArray<T>(T[] array, int newSize)
        {
            var inventory = new T[newSize];
            for (int i = 0; i < (array.Length > inventory.Length ? inventory.Length : array.Length); i++)
                inventory[i] = array[i];

            return inventory;
        }

        public static string GetBasePath(string folder)
        {
            return Path.Combine(GetAssemblyDirectory(), folder);
        }

        public static string GetAssemblyDirectory()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static byte[] Deflate(string src)
        {
            var bytes = Encoding.UTF8.GetBytes(src);
            return Deflate(bytes);
        }

        public static byte[] Deflate(byte[] src)
        {
            byte[] zipBytes;

            using (var dst = new MemoryStream())
            {
                using (var df = new DeflateStream(dst, CompressionMode.Compress))
                    df.Write(src, 0, src.Length);

                zipBytes = dst.ToArray();
            }

            return zipBytes;
        }

        public static int FromString(string x, int def = 0)
        {
            var val = def;
            try
            {
                val = x.StartsWith("0x") ? int.Parse(x.Substring(2), NumberStyles.HexNumber) : int.Parse(x);
            }
            catch
            {
            }

            return val;
        }

        public static string To4Hex(this ushort x)
        {
            return "0x" + x.ToString("x4");
        }

        public static string ToCommaSepString<T>(this T[] arr)
        {
            StringBuilder ret = new StringBuilder();
            for (var i = 0; i < arr.Length; i++)
            {
                if (i != 0) ret.Append(", ");
                ret.Append(arr[i].ToString());
            }

            return ret.ToString();
        }

        public static T[] CommaToArray<T>(this string x)
        {
            if (typeof(T) == typeof(ushort))
                return x.Split(',').Select(_ => (T)(object)(ushort)FromString(_.Trim())).ToArray();
            if (typeof(T) == typeof(string))
                return x.Split(',').Select(_ => (T)(object)_.Trim()).ToArray();
            else //assume int
                return x.Split(',').Select(_ => (T)(object)FromString(_.Trim())).ToArray();
        }

        public static byte[] SHA1(string val)
        {
            SHA1Managed sha1 = new SHA1Managed();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(val));
        }

        public static int ToUnixTimestamp(this DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static T Exec<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                var box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool IsValidEmail(string strIn)
        {
            var invalid = false;
            if (String.IsNullOrEmpty(strIn))
                return false;

            MatchEvaluator domainMapper = match =>
            {
                // IdnMapping class with default property values.
                var idn = new IdnMapping();

                var domainName = match.Groups[2].Value;
                try
                {
                    domainName = idn.GetAscii(domainName);
                }
                catch (ArgumentException)
                {
                    invalid = true;
                }

                return match.Groups[1].Value + domainName;
            };

            // Use IdnMapping class to convert Unicode domain names. 
            strIn = Regex.Replace(strIn, @"(@)(.+)$", domainMapper);
            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format. 
            return Regex.IsMatch(strIn,
                @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                RegexOptions.IgnoreCase);
        }

        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                        Attribute.GetCustomAttribute(field,
                            typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return attr?.Description;
                }
            }

            return null;
        }

        public static bool HasDescription(this FieldInfo field)
        {
            return (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description != null;
        }

        public static int FieldGetShiftId(this FieldInfo field)
        {
            int i;
            var desc = (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute)
                ?.Description;
            if (desc != null)
            {
                var arr = desc.Split(' ');
                if (arr.Length == 2 && int.TryParse(arr[1], out i)) return i;
                else if (arr.Length == 1 && int.TryParse(arr[0], out i)) return i;
            }
            return -1;
        }

        public static bool ServerOnly(this FieldInfo field)
        {
            var desc = (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute)
                ?.Description;
            if (desc != null)
            {
                var arr = desc.Split(' ');
                if (arr.Length == 1) return false;
                if (arr.Length == 2 && arr[0] == "Server") return true;
            }
            return false;
        }

        public static void ByteArrayImport(object target, uint key, NReader rdr, int shiftOffset)
        {
            short len;
            int j;
            foreach (var field in target.GetType().GetFields().OrderBy(x => x.FieldGetShiftId()))
            {
                // field shiftId = bitshift shenanigans
                var name = field.Name;
                var shiftId = field.FieldGetShiftId() - shiftOffset;
                // object type
                if (shiftId == -2)
                {
                    Dynamic.InvokeSet(target, name, rdr.ReadUInt16());
                    continue;
                }
                
                if (shiftId > 31 || (key & (uint)1 << shiftId) == 0) continue;
                var type = field.FieldType;
                if (type == typeof(string))
                    Dynamic.InvokeSet(target, name, rdr.ReadUTF());
                else if (type == typeof(int))
                    Dynamic.InvokeSet(target, name, rdr.ReadInt32());
                else if (type == typeof(byte))
                    Dynamic.InvokeSet(target, name, rdr.ReadByte());
                else if (type == typeof(ushort))
                    Dynamic.InvokeSet(target, name, rdr.ReadUInt16());
                else if (type == typeof(bool))
                    Dynamic.InvokeSet(target, name, rdr.ReadBoolean());
                else if (type == typeof(float))
                    Dynamic.InvokeSet(target, name, rdr.ReadSingle());
                else if (type == typeof(double))
                    Dynamic.InvokeSet(target, name, rdr.ReadDouble());
                else if (type == typeof(byte[]))
                {
                    len = rdr.ReadInt16(); 
                    var arr = new byte[len];
                    for (j = 0; j < len; j++)
                        arr[j] = rdr.ReadByte();
                    Dynamic.InvokeSet(target, name, arr);
                }
                else if (type == typeof(bool[]))
                {
                    len = rdr.ReadInt16(); 
                    var arr = new bool[len];
                    for (j = 0; j < len; j++)
                        arr[j] = rdr.ReadBoolean();
                    Dynamic.InvokeSet(target, name, arr);
                }
                else if (type == typeof(ushort[]))
                {
                    len = rdr.ReadInt16();
                    var arr = new ushort[len];
                    for (j = 0; j < len; j++)
                        arr[j] = rdr.ReadUInt16();
                    Dynamic.InvokeSet(target, name, arr);
                }
                else if (type == typeof(string[]))
                {
                    len = rdr.ReadInt16(); 
                    var arr = new string[len];
                    for (j = 0; j < len; j++)
                        arr[j] = rdr.ReadUTF();
                    Dynamic.InvokeSet(target, name, arr);
                }
                else if (type == typeof(KeyValuePair<byte, short>[]))
                {
                    len = rdr.ReadInt16(); 
                    var arr = new KeyValuePair<byte, short>[len];
                    for (j = 0; j < len; j++)
                        arr[j] = new KeyValuePair<byte, short>(rdr.ReadByte(), rdr.ReadInt16());
                    Dynamic.InvokeSet(target, name, arr);
                }
                else if (type == typeof(ItemData[]))
                    Dynamic.InvokeSet(target, name, (ItemData[])rdr.ReadObject(typeof(ItemData[])));
            }
        }
        
        public static byte[] ByteArrayExport(object target, bool server)
        {
            using (var ms = new MemoryStream())
            using (var wtr = new NWriter(ms))
            {
                int i;
                uint key = 0;
                uint key2 = 0;
                wtr.Write(key);
                wtr.Write(key2);
                foreach (var field in target.GetType().GetFields().OrderBy(x => x.FieldGetShiftId()))
                {
                    // field shiftId = bitshift shenanigans
                    var name = field.Name;
                    var shiftId = field.FieldGetShiftId();
                    // object type
                    if (shiftId == -2)
                    {
                        wtr.Write((ushort)Dynamic.InvokeGet(target, name));
                        continue;
                    }

                    // all others
                    if ((field.ServerOnly() && !server) || shiftId == -1) continue;
                    var val = Dynamic.InvokeGet(target, name);
                    var type = field.FieldType;
                    if (type == typeof(string) && val != null)
                        wtr.WriteUTF((string)val);
                    else if (type == typeof(byte) && (byte)val != default)
                        wtr.Write((byte)val);
                    else if (type == typeof(int) && (int)val != default)
                        wtr.Write((int)val);
                    else if (type == typeof(ushort) && (ushort)val != default)
                        wtr.Write((ushort)val);
                    else if (type == typeof(bool) && (bool)val != default)
                        wtr.Write((bool)val);
                    else if (type == typeof(float) && (float)val != default)
                        wtr.Write((float)val);
                    else if (type == typeof(double) && (double)val != default)
                        wtr.Write((double)val);
                    else if (type == typeof(byte[]) && val != null)
                    {
                        var arr = (byte[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                            wtr.Write(arr[i]);
                    }
                    else if (type == typeof(bool[]) && val != null)
                    {
                        var arr = (bool[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                            wtr.Write(arr[i]);
                    }
                    else if (type == typeof(int[]) && val != null)
                    {
                        var arr = (int[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                            wtr.Write(arr[i]);
                    }
                    else if (type == typeof(ushort[]) && val != null)
                    {
                        var arr = (ushort[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                            wtr.Write(arr[i]);
                    }
                    else if (type == typeof(string[]) && val != null)
                    {
                        var arr = (string[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                            wtr.WriteUTF(arr[i]);
                    }
                    else if (type == typeof(KeyValuePair<byte, short>[]) && val != null)
                    {
                        var arr = (KeyValuePair<byte, short>[])val;
                        wtr.Write((short)arr.Length);
                        for (i = 0; i < arr.Length; i++)
                        {
                            wtr.Write(arr[i].Key);
                            wtr.Write(arr[i].Value);
                        }
                    }
                    else if (type == typeof(ItemData[]) && val != null)
                        wtr.Write(((ItemData[])val).ToBytes(server));
                    else continue;
                    
                    if (shiftId > 31)
                        key2 |= (uint)1 << (shiftId - 31);
                    else
                        key |= (uint)1 << shiftId;
                }

                wtr.BaseStream.Position = 0;
                wtr.Write(key);
                wtr.Write(key2);
                return ms.ToArray();
            }
        }

        public static T FromJson<T>(string json) where T : class
        {
            if (String.IsNullOrWhiteSpace(json)) return null;
            var jsonSerializer = new JsonSerializer();
            using (var strRdr = new StringReader(json))
            using (var jsRdr = new JsonTextReader(strRdr))
                return jsonSerializer.Deserialize<T>(jsRdr);
        }

        public static DateTime FromUnixTimestamp(int time)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(time).ToLocalTime();
            return dateTime;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", "");
        }

        // https://www.codeproject.com/Articles/770323/How-to-Convert-a-Date-Time-to-X-minutes-ago-in-Csh
        public static string TimeAgo(DateTime dt)
        {
            TimeSpan span = DateTime.Now - dt;
            if (span.Days > 365)
            {
                int years = (span.Days / 365);
                if (span.Days % 365 != 0)
                    years += 1;
                return $"{years} {(years == 1 ? "year" : "years")} ago";
            }

            if (span.Days > 30)
            {
                int months = (span.Days / 30);
                if (span.Days % 31 != 0)
                    months += 1;
                return $"{months} {(months == 1 ? "month" : "months")} ago";
            }

            if (span.Days > 0)
                return $"{span.Days} {(span.Days == 1 ? "day" : "days")} ago";
            if (span.Hours > 0)
                return $"{span.Hours} {(span.Hours == 1 ? "hour" : "hours")} ago";
            if (span.Minutes > 0)
                return $"{span.Minutes} {(span.Minutes == 1 ? "minute" : "minutes")} ago";
            if (span.Seconds > 5)
                return $"{span.Seconds} seconds ago";
            if (span.Seconds <= 5)
                return "just now";
            return string.Empty;
        }

        public static bool HasElement(this XElement e, string name)
        {
            return e.Element(name) != null;
        }

        public static bool HasAttribute(this XElement e, string name)
        {
            return e.Attribute(name) != null;
        }

        public static int GetInt(string x)
        {
            return x.Contains("x") ? Convert.ToInt32(x, 16) : int.Parse(x);
        }

        public static ConditionEffectIndex GetEffect(string val)
        {
            ConditionEffectIndex ret =
                (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), val.Replace(" ", ""));
            return ret;
        }

        public static T GetValue<T>(this XElement e, string n, T def = default(T))
        {
            if (e.Element(n) == null)
                return def;

            string val = e.Element(n).Value;
            var t = typeof(T);
            if (t == typeof(string))
                return (T)Convert.ChangeType(val, t);
            else if (t == typeof(ushort))
                return (T)Convert.ChangeType(Convert.ToUInt16(val, 16), t);
            else if (t == typeof(int))
                return (T)Convert.ChangeType(GetInt(val), t);
            else if (t == typeof(uint))
                return (T)Convert.ChangeType(Convert.ToUInt32(val, 16), t);
            else if (t == typeof(double))
                return (T)Convert.ChangeType(double.Parse(val, CultureInfo.InvariantCulture), t);
            else if (t == typeof(float))
                return (T)Convert.ChangeType(float.Parse(val, CultureInfo.InvariantCulture), t);
            else if (t == typeof(bool))
                return (T)Convert.ChangeType(string.IsNullOrWhiteSpace(val) || bool.Parse(val), t);

            return def;
        }

        public static T GetAttribute<T>(this XElement e, string n, T def = default(T))
        {
            if (e.Attribute(n) == null)
                return def;

            string val = e.Attribute(n).Value;
            var t = typeof(T);
            if (t == typeof(string))
                return (T)Convert.ChangeType(val, t);
            else if (t == typeof(ushort))
                return (T)Convert.ChangeType(Convert.ToUInt16(val, 16), t);
            else if (t == typeof(int))
                return (T)Convert.ChangeType(GetInt(val), t);
            else if (t == typeof(uint))
                return (T)Convert.ChangeType(Convert.ToUInt32(val, 16), t);
            else if (t == typeof(double))
                return (T)Convert.ChangeType(double.Parse(val, CultureInfo.InvariantCulture), t);
            else if (t == typeof(float))
                return (T)Convert.ChangeType(float.Parse(val, CultureInfo.InvariantCulture), t);
            else if (t == typeof(bool))
                return (T)Convert.ChangeType(string.IsNullOrWhiteSpace(val) || bool.Parse(val), t);

            return def;
        }

        public static IEnumerable<string[]> ReadFilesAndNames(string basePath, string pattern, SearchOption option = SearchOption.AllDirectories)
        {
            var xmls = Directory.EnumerateFiles(basePath, pattern, option).ToArray();
            for (var i = 0; i < xmls.Length; i++)
            {
                yield return new[]
                {
                    File.ReadAllText(xmls[i]),
                    $"{xmls[i].Replace("./resources/worlds/", "").Replace(".jm", "")}"
                };
            }
        }
        
        public static string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }

        public static void WriteFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
        
        public static string GetBuildConfig()
        {
#if DEBUG
            return "debug";
#else
            return "release";
#endif
        }
    }

    public static class StringUtils
    {
        public static bool ContainsIgnoreCase(this string self, string val)
        {
            return self.IndexOf(val, StringComparison.InvariantCultureIgnoreCase) != -1;
        }

        public static bool EqualsIgnoreCase(this string self, string val)
        {
            return self.Equals(val, StringComparison.InvariantCultureIgnoreCase);
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }
    }
    
    public static class ParseUtils
    {
        public static string ParseString(this XElement element, string name, string undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return value;
        }

        public static int ParseInt(this XElement element, string name, int undefined = 0)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return Utils.FromString(value, undefined);
        }
        
        private static int? NFromString(string x, int? undefined = null)
        {
            var val = undefined;
            try
            {
                val = x.StartsWith("0x") ? int.Parse(x.Substring(2), NumberStyles.HexNumber) : int.Parse(x);
            }
            catch { }
            
            return val;
        }
        
        public static int? ParseNInt(this XElement element, string name, int? undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return NFromString(value, undefined);
        }

        public static long ParseLong(this XElement element, string name, long undefined = 0)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return long.Parse(value);
        }

        public static uint ParseUInt(this XElement element, string name, bool isHex = true, uint undefined = 0)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return Convert.ToUInt32(value, isHex ? 16 : 10);
        }

        public static float ParseFloat(this XElement element, string name, float undefined = 0)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public static float? ParseNFloat(this XElement element, string name, float? undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public static bool ParseBool(this XElement element, string name, bool undefined = false)
        {
            var isAttr = name[0].Equals('@');
            var id = name[0].Equals('@') ? name.Remove(0, 1) : name;
            var value = isAttr ? element.Attribute(id)?.Value : element.Element(id)?.Value;
            if (string.IsNullOrWhiteSpace(value)) 
            {
                if (isAttr && element.Attribute(id) != null || !isAttr && element.Element(id) != null)
                    return true;
                return undefined; 
            }
            return bool.Parse(value);
        }

        public static ushort ParseUshort(this XElement element, string name, ushort undefined = 0)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return (ushort)(value.StartsWith("0x") ? int.Parse(value.Substring(2), NumberStyles.HexNumber) : int.Parse(value));
        }

        public static DamageTypes ParseDamageType(this XElement element, string name, DamageTypes undefined = DamageTypes.Physical)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return undefined;
            return (DamageTypes)Enum.Parse(typeof(DamageTypes), value.Replace(" ", ""));
        }

        public static ConditionEffectIndex ParseConditionEffect(this XElement element, string name, ConditionEffectIndex undefined = ConditionEffectIndex.Dead)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return undefined;
            return (ConditionEffectIndex)Enum.Parse(typeof(ConditionEffectIndex), value.Replace(" ", ""));
        }
        
        public static string[] ParseStringArray(this XElement element, string name, char seperator, string[] undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            return value.Split(seperator);
        }

        public static int[] ParseIntArray(this XElement element, string name, char seperator, int[] undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator).Select(k => int.Parse(k)).ToArray();
        }

        public static ushort[] ParseUshortArray(this XElement element, string name, char seperator, ushort[] undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator).Select(k => (ushort)(k.StartsWith("0x") ? Int32.Parse(k.Substring(2), NumberStyles.HexNumber) : Int32.Parse(k))).ToArray();
        }

        public static float[] ParseFloatArray(this XElement element, string name, char seperator, float[] undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator).Select(k => float.Parse(k)).ToArray();
        }

        public static double[] ParseDoubleArray(this XElement element, string name, char seperator, double[] undefined = null)
        {
            var value = name[0].Equals('@') ? element.Attribute(name.Remove(0, 1))?.Value : element.Element(name)?.Value;
            if (string.IsNullOrWhiteSpace(value)) return undefined;
            value = Regex.Replace(value, @"\s+", "");
            return ParseStringArray(element, name, seperator).Select(k => double.Parse(k)).ToArray();
        }
    }
}