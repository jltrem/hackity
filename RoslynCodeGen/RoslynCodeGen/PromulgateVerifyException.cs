using System;

namespace RoslynCodeGen;

public class PromulgateVerifyException(string className, string propertyName, object value)
    : ArgumentException($"Promulgate verify failed for {className}.{propertyName} with value `{value}`");
