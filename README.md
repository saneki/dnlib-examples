dnlib-examples
==============

Provides more examples for [dnlib], specifically examples using generics.
MIT-licensed.

Examples:
- [GenericExample1] constructs a `List<String>` and uses it.
- [GenericExample2] uses the static method `Array.AsReadOnly<Int32>` to
  create a `ReadOnlyCollection<Int32>` from an `int[]`.
- [GenericExample3] creates a type with generic parameters, as well as a nested
  type with more generic parameters.

[dnlib]:https://github.com/0xd4d/dnlib
[GenericExample1]:src/dnlib.MoreExamples/GenericExample1.cs
[GenericExample2]:src/dnlib.MoreExamples/GenericExample2.cs
[GenericExample3]:src/dnlib.MoreExamples/GenericExample3.cs
