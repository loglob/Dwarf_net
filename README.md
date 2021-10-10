# Dwarf_net
A .NET wrapper for [libdwarf](https://sourceforge.net/projects/libdwarf/)

## Building
Before building with `dotnet build`, you must run `./Defines.csx`

This requires [dotnet-script](https://github.com/filipw/dotnet-script).

Libdwarf must be installed at runtime (but not build time).

## Using
To start, open a `Debug` instance via one of the class's constructors.
This class represents an opened DWARF binary and is used as a starting point
for every other Class in the library.

Referring to the DWARF documentation ([found here](https://www.dwarfstd.org/doc/DWARF5.pdf))
may be advisable or necessary at some points,
as the documentation of Dwarf_net (and libdwarf) is lacking in some places.

## Progress
The wrapper is not complete at present.

The current priority is to get (relatively) complete and well documented DWARF5 support
and move on to legacy interfaces later.