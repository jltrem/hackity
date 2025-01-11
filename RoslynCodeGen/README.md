# WrappedValdationAttribute

test output:

```
X10.Create(2)
- 20

NonNegative.Create(5) 
- 5

NonNegative.Create(0)
- 0

NonNegative.Create(-5)
- Value cannot be negative.

StrictlyPositive sut = 1;
- 1

StrictlyPositive.TryCreate(0, out var sut);
- false

NonNullException = null;
- Value cannot be null. (Parameter 'value')

NonNullException.Create(new AggregateException("foobar"));
- Promulgate.NonNullException
- System.ApplicationException: foobar

ValidatedPerson.Create(new Person(" James Dean\t", 42));
- Person { FullName = James Dean, Age = 42 }

BasketballScore sut = (76, 41);
- (76, 41)

BasketballScore sut = (76, -1)
- score must be non-negative: (Away = -1)

BasketballScore sut = (-2, -1)
- score must be non-negative: (Home = -2)
- score must be non-negative: (Away = -1)
```
