using System;
using System.Collections.Generic;

namespace EventSourcing.Core.Tests.MockAggregates;

public enum MockEnum
{
  A = 0,
  B = 1,
  C = 2,
}

[Flags]
public enum MockFlagEnum : byte
{
  A = 1 << 0,
  B = 1 << 1,
  C = 1 << 2,
  D = 1 << 3,
  E = 1 << 4
}

public class MockNestedClass
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
}

public record MockEvent : Event
{
  public bool MockBoolean { get; init; }
  public string MockString { get; init; }
  public decimal MockDecimal { get; init; }
  public double MockDouble { get; init; }
    
  public MockEnum MockEnum { get; init; }
  public MockFlagEnum MockFlagEnum { get; init; }
    
  public MockNestedClass MockNestedClass { get; init; }
    
  public List<MockNestedClass> MockNestedClassList { get; init; }

  public List<float> MockFloatList { get; init; }
  public HashSet<string> MockStringSet { get; init; }
}