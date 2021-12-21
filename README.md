# Dwarf_net
A .NET wrapper for [libdwarf](https://sourceforge.net/projects/libdwarf/)

## Building
Before building with `dotnet build`, you must run `./Defines.csx`

This requires [dotnet-script](https://github.com/filipw/dotnet-script).

Libdwarf must be installed at run- and buildtime.

## Installing
To add the package to your project, use `dotnet add package Dwarf.NET`

## Using
To start, open a `Debug` instance via one of the class's constructors.
This class represents an opened DWARF binary and is used as a starting point
for every other class in the library.

Referring to the DWARF documentation ([found here](https://www.dwarfstd.org/doc/DWARF5.pdf))
may be advisable or necessary at some points,
as the documentation of Dwarf_net (and libdwarf) is lacking in some places.

## Progress
The wrapper is not fully complete at present, but should cover enough of libdwarf for most use cases.

The current priority is to get (relatively) complete and well documented DWARF5 support
and move on to legacy interfaces later.
