#!/usr/bin/env dotnet-script
// Defines.csx: Generates an enum containing the preprocessor definitions from dwarf.h and libdwarf.h
using System.Globalization;
using System.Text.RegularExpressions;

class EnumDef
{
	public readonly string Name;
	public List<(string name, string value)> Entries;

	public EnumDef(string name)
	{
		Name = name;
		Entries = new List<(string name, string value)>();
	}

	/* Parses a C enum definition */
	public static EnumDef Parse(string def)
	{
		var parts = def.Split('{', 2);

		var d = new EnumDef(parts[0]
			.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
			.Last()
			.Substring("Dwarf_".Length)
			.pascalify());

		IEnumerable<(string name, string value)> entries = parts[1]
			.Split('}', 2)[0]
			.Split(',')
			.Select(s => s.Split('=', 2))
			.Select(s => s.Length == 2
				? (s[0].Trim(), s[1].Trim())
				: (s[0].Trim(), null));

		var pfx = entries.Select(s => s.name).longestPrefix();

		foreach (var e in entries)
			d[e.name.Substring(pfx.Length).pascalify()] = e.value;

		return d;
	}

	public string this[string name]
	{
		set
			=> Entries.Add((name, value));
	}

	/* Prints the enum to the given stream */
	public void PrintTo(TextWriter pr, string lnPrefix)
	{
		pr.WriteLine($"{lnPrefix}{enumModifiers}enum {Name}");
		pr.WriteLine(lnPrefix + "{");

		foreach (var e in Entries)
			pr.WriteLine(lnPrefix + $"\t{e.name}" +
				((e.value is null) ? "," : $" = {e.value},"));

		pr.WriteLine(lnPrefix + "}\n");
	}
}

/* Type modifiers prepended to enum type definitions */
const string enumModifiers = "public ";

/* The file to write to */
const string outfile = "Defines.cs";

/* Definitions with predefined values */
static readonly Dictionary<string,string> manual = new Dictionary<string, string>
{
	{ "DW_DLV_BADADDR", "-1L"},
	{ "DW_DLV_NOCOUNT", "-1L"	},
	{ "DW_DLV_BADOFFSET", "-1L" }
};

/* Every value with these prefixes is packed into their own enum type */
static readonly (string prefix, string enumName)[] enumPrefixes = {
	("DW_TAG_", "Tag"),
	("DW_FORM_", "Form"),
	("DW_AT_", "AttributeNumber"),
	("DW_OP_", "Operation"),
	("DW_ATE_", "BaseType"),
	("DW_UT_", "UnitType"),
	("DW_LLE_", "LocationListEntry"),
	("DW_DS_", "DecimalSign"),
	("DW_END_", "Endianness"),
	("DW_ACCESS_", "Accessibility"),
	("DW_VIS_", "Visibility"),
	("DW_VIRTUALITY_", "Virtuality"),
	("DW_LANG_", "SourceLanguage"),
	("DW_ID_", "IdentifierCase"),
	("DW_CC_", "CallingConvention"),
	("DW_INL_", "Inline"),
	("DW_ORD_", "ArrayOrdering"),
	("DW_DSC_", "DiscriminantDescriptor"),
	("DW_IDX_", "NameIndex"),
	("DW_DEFAULTED_", "DefaultedMember"),
	("DW_LNS_", "LineNumberStandardOpcode"),
	("DW_LNE_", "LineNumberExtendedOpcode"),
	("DW_LNCT_", "LineNumberHeaderEntryFormat"),
	("DW_MACRO_", "MacroOpcode"),
	("DW_CFA_", "CallFrameInstruction"),
	("DW_RLE_", "RangeListEntry")
};

static readonly HashSet<string> omitList = new HashSet<string>
{
	"DW_LANG_Upc"
};

/* Finds the longest prefix that matches all given strings */
static string longestPrefix(this IEnumerable<string> s)
{
	StringBuilder sb = new StringBuilder();
	string f = s.First();

	while(s.All(x => x[sb.Length] == f[sb.Length]))
		sb.Append(f[sb.Length]);

	return sb.ToString();
}

/** Removes any comments in the string */
static string removeComment(this string s)
{
	var ls = s.Split("//", 2);

	if(ls.Length == 2 && !ls[0].Contains("/*"))
		return ls[0].removeComment();

	var bs = s.Split("/*", 2);

	if(bs.Length == 2)
		return bs[0] + bs[1].Split("*/", 2)[1].removeComment();

	return s;
}

/** extracts enums from the file.
	Does NOT scan for typedefs */
static IEnumerable<EnumDef> extractEnums(string file)
	=> new Regex(@"enum\s+\S+\s*{.*?}", RegexOptions.Singleline)
		.Matches(file)
		.Select(m => EnumDef.Parse(m.Value));

delegate bool MaybeFunc<A, B>(A input, out B output);

static IEnumerable<B> SelectWhere<A,B>(this IEnumerable<A> ls, MaybeFunc<A,B> f)
{
	foreach (var a in ls)
	{
		if(f(a, out var b))
			yield return b;
	}
}

/** extracts preprocessor definitions from the file.
	Does NOT run a full preprocessor (i.e. doesn't check for #ifdef) */
static IEnumerable<(string name, string val)> extractDefines(string file)
	=> new Regex(@"^#define\s+(\S+)[ 	]+(.*?)$", RegexOptions.Multiline)
		.Matches(file)
		.Select(m => (m.Groups[1].Value, m.Groups[2].Value.Trim()))
		.Distinct()
		.SelectWhere(((string name, string val) d, out (string name, string val) v) => {
			if(manual.TryGetValue(d.name, out v.val))
			{
				v.name = d.name;
				return true;
			}
			if (omitList.Contains(d.name))
			{
				v = default;
				return false;
			}

			v = (d.name, translateNumberFromC(d.val));

			if(v.val is null)
			{
				Console.Error.WriteLine($"Omitting '#define {d.name} {d.val}'");
				return false;
			}

			return true;
		});

private static B foldr<A,B>(this IEnumerable<A> ls, Func<B,A,B> f, B zero)
{
	B cur = zero;

	foreach (var a in ls)
		cur = f(cur, a);

	return cur;
}

// null-safe concat
private static string cc(this string l, string r)
	=> (l is null || r is null) ? null : l + r;

/** Parses a C integer literal and translates it to a C# integer literal.
	Does NOT support manual type casting or bitwise operations */
private static string translateNumberFromC(this string val)
{
	if(val.StartsWith("(") && val.EndsWith(")"))
		return translateNumberFromC(val.Substring(1, val.Length - 2));

	// accept references
	if(new Regex(@"^\w+$").IsMatch(val))
		return val;
	if(val.Contains('+'))
	{
		var s = val.Split('+', 2, StringSplitOptions.TrimEntries);
		return translateNumberFromC(s[0]).cc(" + ").cc(translateNumberFromC(s[1]));
	}
	if(val.EndsWith("LL"))
		return translateNumberFromC(val.Substring(0, val.Length - 2)).cc("L");
	if(val.EndsWith("L"))
		return translateNumberFromC(val.Substring(0, val.Length - 1)).cc("L");
	if(val.StartsWith("-"))
		return "-".cc(translateNumberFromC(val.Substring(1)));

	if(val.StartsWith("0x") || (val[0] >= '1' && val[0] <= '9'))
		return val;
	if(val.StartsWith("0") &&  val.All(c => c >= '0' && c <= '7'))
		return (val.foldr((x,c) => (x << 3) | (ulong)(uint)(c - '0'), 0UL) >> 3).ToString();

	return null;
}

public static string StrJoin(this IEnumerable<string> s, string joiner="")
	=> string.Join(joiner, s);

static string literalType(this string val)
	=> new Regex(@"\dL$").IsMatch(val) ? "long" : "int";

// Transforms a snake_case string into a PascalCase string
static string pascalify(this string s)
	=> s.Split('_').Select(ss => Char.ToUpper(ss[0]) + ss.Substring(1).ToLower()).StrJoin();

string input = Directory.GetFiles("/usr/include/libdwarf/")
	.Select(File.ReadAllText)
	.Select(removeComment)
	.StrJoin("\n");

static bool TryFirst<T>(this IEnumerable<T> ls, Func<T, bool> p, out T val)
{
	foreach (var i in ls)
	{
		if(p(i))
		{
			val = i;
			return true;
		}
	}

	val = default;
	return false;
}

void fill(TextWriter o)
{
	o.WriteLine(
		$"// {outfile}: Generated by Defines.csx, contains libdwarf's enums\n" +
		"using System;\n" +
		"\n" +
		"namespace Dwarf\n" +
		"{\n" +
		"	public static class Defines\n" +
		"	{\n");

	(string prefix, EnumDef e)[] enums
		= enumPrefixes.Select(e => (e.prefix, new EnumDef(e.enumName))).ToArray();

	foreach (var d in extractDefines(input))
	{
		if(enums.TryFirst(e => d.name.StartsWith(e.prefix), out var e))
			e.e[d.name.Substring(e.prefix.Length).pascalify()] = d.val;
		else
			o.WriteLine($"\t\tpublic const {d.val.literalType()} {d.name} = {d.val};");
	}

	o.WriteLine("	}\n");

	foreach (var e in enums.Select(ex => ex.e).Concat(extractEnums(input)))
	{
		if(!e.Entries.Any())
			Console.WriteLine($"Warning: Empty enum '{e.Name}'");

		e.PrintTo(o, "\t");
	}

	o.WriteLine("}");
}

using(var f = File.CreateText(outfile))
	fill(f);